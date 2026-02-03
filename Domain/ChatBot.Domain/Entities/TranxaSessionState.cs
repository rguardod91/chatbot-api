using ChatBot.Domain.Enums;

namespace ChatBot.Domain.Entities
{
    public class TranxaSessionState
    {
        public Guid Id { get; set; }
        public Guid SessionId { get; set; }
        public ConversationStep CurrentStep { get; set; }
        public string? TempData { get; set; }
        public DateTime UpdatedAt { get; set; }

        public TranxaSession Session { get; set; } = null!;
    }
}
