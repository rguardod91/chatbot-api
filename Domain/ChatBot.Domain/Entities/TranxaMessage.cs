using ChatBot.Domain.Enums;

namespace ChatBot.Domain.Entities
{
    public class TranxaMessage
    {
        public Guid Id { get; set; }
        public Guid SessionId { get; set; }
        public MessageDirection Direction { get; set; }
        public string MessageType { get; set; } = null!;
        public string? Content { get; set; }
        public DateTime CreatedAt { get; set; }

        public TranxaSession Session { get; set; } = null!;
    }
}
