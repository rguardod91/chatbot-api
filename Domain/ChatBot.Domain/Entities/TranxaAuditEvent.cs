namespace ChatBot.Domain.Entities
{
    public class TranxaAuditEvent
    {
        public Guid Id { get; set; }
        public Guid SessionId { get; set; }
        public Guid UserId { get; set; }
        public string EventType { get; set; } = null!;
        public string? ExternalReference { get; set; }
        public string Result { get; set; } = null!;
        public string? Details { get; set; }
        public DateTime CreatedAt { get; set; }

        public TranxaSession Session { get; set; } = null!;
        public TranxaUser User { get; set; } = null!;
    }
}
