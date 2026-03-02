using CryptiqChat.Data;
using CryptiqChat.Models;
using Microsoft.EntityFrameworkCore;

namespace CryptiqChat.Services
{
    public class ChatService
    {
        private readonly CryptiqDbContext _db;

        public ChatService(CryptiqDbContext db)
        {
            _db = db;
            Console.WriteLine($"Conectado a: {_db.Database.GetConnectionString()}");
        }

        // ── Guardar mensaje 1-a-1 con estado ──────────────────────────
        public async Task<ChatMessage> SavePrivateMessageAsync(
            Guid senderId, Guid receiverId,
            string encryptedPayload, string qrData,
            int statusId = 4)
        {
            var message = new ChatMessage
            {
                SenderId         = senderId,
                ReceiverId       = receiverId,
                EncryptedPayload = encryptedPayload,
                QrData           = qrData,
                CreatedAt        = DateTime.UtcNow,
                StatusId         = statusId,
                IsDeleted        = false
            };

            _db.ChatMessages.Add(message);
            await _db.SaveChangesAsync();

            Console.WriteLine($"Insertado en BD: {message.Id}, Status={message.StatusId}");
            return message;
        }

        public async Task<ChatMessage?> GetMessageByIdAsync(Guid messageId)
        {
            return await _db.ChatMessages
                .Include(m => m.Status) 
                .FirstOrDefaultAsync(m => m.Id == messageId); 
        }

        // ── Guardar mensaje grupal ─────────────────────────────────
        public async Task<ChatMessage> SaveGroupMessageAsync(
            Guid senderId, Guid groupId,
            string encryptedPayload, string qrData)
        {
            var message = new ChatMessage
            {
                SenderId         = senderId,
                GroupId          = groupId,
                EncryptedPayload = encryptedPayload,
                QrData           = qrData,
                CreatedAt        = DateTime.UtcNow
            };

            _db.ChatMessages.Add(message);
            await _db.SaveChangesAsync();
            return message;
        }

        // ── Obtener historial de mensajes 1-a-1 ───────────────────
        public async Task<List<ChatMessage>> GetPrivateHistoryAsync(
            Guid userId, Guid contactId, int limit = 50)
        {
            return await _db.ChatMessages
                .Where(m =>
                    m.IsDeleted == false &&
                    m.GroupId == null &&
                    ((m.SenderId == userId   && m.ReceiverId == contactId) ||
                     (m.SenderId == contactId && m.ReceiverId == userId)))
                .OrderByDescending(m => m.CreatedAt)
                .Take(limit)
                .Include(m => m.Sender)
                .ToListAsync();
        }

        // ── Obtener historial de mensajes grupales ─────────────────
        public async Task<List<ChatMessage>> GetGroupHistoryAsync(
            Guid groupId, int limit = 50)
        {
            return await _db.ChatMessages
                .Where(m => m.GroupId == groupId && m.IsDeleted == false)
                .OrderByDescending(m => m.CreatedAt)
                .Take(limit)
                .Include(m => m.Sender)
                .ToListAsync();
        }

        // ── Obtener mensajes por estado ───────────────────────────────
        public async Task<List<ChatMessage>> GetMessagesByStatusAsync(Guid receiverId, int statusId)
        {
            return await _db.ChatMessages
                .Where(m => m.ReceiverId == receiverId && m.StatusId == statusId && m.IsDeleted == false)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }

        // ── Actualizar estado de un mensaje ───────────────────────────
        public async Task UpdateMessageStatusAsync(Guid messageId, int newStatusId)
        {
            var message = await _db.ChatMessages.FindAsync(messageId);
            if (message != null)
            {
                message.StatusId = newStatusId;
                await _db.SaveChangesAsync();
            }
        }
    }
}
