using CryptiqChat.Models;

public class MessageStatus
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }

    public ICollection<ChatMessage> Messages { get; set; }
}
