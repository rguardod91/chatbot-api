namespace ChatBot.Domain.Entities
{
    public class TranxaExternalServiceLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ServiceName { get; set; } = string.Empty;
        public Guid SessionId { get; set; }
        public string Endpoint { get; set; } = string.Empty;
        public string HttpMethod { get; set; } = string.Empty;
        public string? RequestSummary { get; set; }
        public string? ResponseSummary { get; set; }
        public string? Details { get; set; }
        public DateTime CreatedAt { get; set; }
        public TranxaSession? Session { get; set; }
    }
}