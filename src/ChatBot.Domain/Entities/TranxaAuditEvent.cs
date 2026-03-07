namespace ChatBot.Domain.Entities
{
    public class TranxaAuditEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string EventType { get; set; } = string.Empty;
        public Guid SessionId { get; set; }
        public Guid UserId { get; set; }
        public string? ExternalReference { get; set; }
        public string? Result { get; set; }
        public string? Details { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? Data { get; set; }
        public DateTime CreatedAt { get; set; }
        public TranxaSession? Session { get; set; }
        public TranxaUser? User { get; set; }
    }
}