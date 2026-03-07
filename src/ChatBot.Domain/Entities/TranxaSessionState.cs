using ChatBot.Domain.Enums;

namespace ChatBot.Domain.Entities
{
    public class TranxaSessionState
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid SessionId { get; set; }
        public ConversationStep CurrentStep { get; set; }
        public string? Data { get; set; }
        public string? TempData { get; set; }
        public DateTime UpdatedAt { get; set; }
        public TranxaSession? Session { get; set; }
    }
}