using CryptiqChat.Services;
using Microsoft.AspNetCore.SignalR;

namespace CryptiqChat.Hubs
{
    public class ChatHub : Hub
    {
        private static readonly Dictionary<string, string> _connectedUsers = new();
        private readonly ChatService _chatService;

        public ChatHub(ChatService chatService)
        {
            _chatService = chatService;
        }

        // ── Al conectarse ───────────────────────────────────────────
        public override async Task OnConnectedAsync()
        {
            // El cliente debe pasar su userId como query string: ?userId=<guid>
            var userId = Context.GetHttpContext()?.Request.Query["userId"].ToString();

            if (!string.IsNullOrEmpty(userId))
            {
                _connectedUsers[userId] = Context.ConnectionId;
                Console.WriteLine($"✅ User logged in: {userId}");

                var userGuid = Guid.Parse(userId);

                // Buscar mensajes pendientes
                var pendingMessages = await _chatService.GetMessagesByStatusAsync(userGuid, 4);

                foreach (var msg in pendingMessages)
                {
                    var message = new
                    {
                        Id         = msg.Id,
                        SenderId   = msg.SenderId.ToString(),
                        ReceiverId = msg.ReceiverId?.ToString(),
                        Payload    = msg.EncryptedPayload,
                        QrData     = msg.QrData,
                        CreatedAt  = msg.CreatedAt,
                        StatusId   = msg.StatusId
                    };

                    await Clients.Caller.SendAsync("ReceivePrivateMessage", message);

                    // Actualizar estado a entregado
                    await _chatService.UpdateMessageStatusAsync(msg.Id, 1);
                }
            }

            await base.OnConnectedAsync();
        }

        // ── Al desconectarse ────────────────────────────────────────
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var user = _connectedUsers.FirstOrDefault(x => x.Value == Context.ConnectionId);
            if (user.Key != null)
            {
                _connectedUsers.Remove(user.Key);
                Console.WriteLine($"❌ Offline: {user.Key}");
            }
            await base.OnDisconnectedAsync(exception);
        }

        // ── Mensaje privado + guardado en BD ──────────────────────
        public async Task SendPrivateMessage(string receiverId, string encryptedPayload, string qrData)
        {
            try
            {
                var senderIdStr = _connectedUsers.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;
                if (string.IsNullOrEmpty(senderIdStr))
                {
                    Console.WriteLine("⚠️ Sender not registered");
                    return;
                }

                var senderId = Guid.Parse(senderIdStr);
                var receiverGuid = Guid.Parse(receiverId);
                var statusId = _connectedUsers.ContainsKey(receiverId) ? 1 : 4;
                var saved = await _chatService.SavePrivateMessageAsync(senderId, receiverGuid, encryptedPayload, qrData, statusId);

                var message = new
                {
                    Id = saved.Id,
                    SenderId = senderIdStr,
                    ReceiverId = receiverId,
                    Payload = encryptedPayload,
                    QrData = qrData,
                    CreatedAt = saved.CreatedAt,
                    StatusId = saved.StatusId
                };

                if (_connectedUsers.TryGetValue(receiverId, out var receiverConnectionId))
                {
                    await Clients.Client(receiverConnectionId).SendAsync("ReceivePrivateMessage", message);
                    await _chatService.UpdateMessageStatusAsync(saved.Id, 1);
                }

                await Clients.Caller.SendAsync("MessageSent", message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en SendPrivateMessage: {ex}");
                throw; // deja que SignalR lo propague
            }
        }


        // ── Mensaje grupal + guardado en BD ───────────────────────
        public async Task SendGroupMessage(string groupId, string encryptedPayload, string qrData)
        {
            var senderIdStr = _connectedUsers.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;
            if (string.IsNullOrEmpty(senderIdStr)) return;

            var senderId  = Guid.Parse(senderIdStr);
            var groupGuid = Guid.Parse(groupId);

            // 1. Guardar en BD
            var saved = await _chatService.SaveGroupMessageAsync(senderId, groupGuid, encryptedPayload, qrData);

            var message = new
            {
                Id        = saved.Id,
                SenderId  = senderIdStr,
                GroupId   = groupId,
                Payload   = encryptedPayload,
                QrData    = qrData,
                CreatedAt = saved.CreatedAt
            };

            // 2. Entregar a todos en el grupo
            await Clients.Group(groupId).SendAsync("ReceiveGroupMessage", message);

            Console.WriteLine($"💾 Group message saved in database: {saved.Id}");
        }

        public async Task JoinGroup(string groupId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupId);
            Console.WriteLine($"➕ {Context.ConnectionId} joined the group {groupId}");
        }

        public async Task LeaveGroup(string groupId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupId);
        }

        public async Task MarkMessageAsRead(Guid messageId)
        {
            await _chatService.UpdateMessageStatusAsync(messageId, 2); // Read
            var message = await _chatService.GetMessageByIdAsync(messageId);

            if (message != null)
            {
                if (_connectedUsers.TryGetValue(message.SenderId.ToString(), out var senderConnectionId))
                {
                    await Clients.Client(senderConnectionId).SendAsync("MessageReadConfirmation", new
                    {
                        Id = message.Id,
                        StatusId = message.StatusId,
                        ReadAt = DateTime.UtcNow
                    });
                }
            }

            Console.WriteLine($"👁️ Message read: {messageId}");
        }

    }
}
