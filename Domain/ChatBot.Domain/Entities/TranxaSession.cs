using ChatBot.Domain.Enums;

namespace ChatBot.Domain.Entities
{
    public class TranxaSession
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime LastActivityAt { get; set; }
        public SessionStatus Status { get; set; }
        public string? CurrentFlow { get; set; }
        public int FailedAttempts { get; set; }

        public TranxaUser User { get; set; } = null!;

        // 🔹 RELACIONES
        public ICollection<TranxaSessionState> States { get; set; } = new List<TranxaSessionState>();
        public ICollection<TranxaMessage> Messages { get; set; } = new List<TranxaMessage>();
    }
}
