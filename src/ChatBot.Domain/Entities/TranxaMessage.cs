using ChatBot.Domain.Enums;

namespace ChatBot.Domain.Entities
{
    public class TranxaMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Content { get; set; } = string.Empty;
        public string Sender { get; set; } = string.Empty;
        public MessageDirection Direction { get; set; }
        public string MessageType { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public Guid SessionId { get; set; }
        public TranxaSession? Session { get; set; }
    }
}