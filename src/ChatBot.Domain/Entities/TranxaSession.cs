using ChatBot.Domain.Enums;

namespace ChatBot.Domain.Entities
{
    public class TranxaSession
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public SessionStatus Status { get; set; }
        public string? CurrentFlow { get; set; }
        public int FailedAttempts { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime LastActivityAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public TranxaUser? User { get; set; }
        public ICollection<TranxaSessionState>? States { get; set; }
        public ICollection<TranxaMessage>? Messages { get; set; }
    }
}