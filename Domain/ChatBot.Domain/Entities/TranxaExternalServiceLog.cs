namespace ChatBot.Domain.Entities
{
    public class TranxaExternalServiceLog
    {
        public Guid Id { get; set; }
        public Guid? SessionId { get; set; }
        public string ServiceName { get; set; } = null!;
        public string Endpoint { get; set; } = null!;
        public string HttpMethod { get; set; } = null!;
        public int? ResponseCode { get; set; }
        public bool IsSuccess { get; set; }
        public int? DurationMs { get; set; }
        public string? RequestSummary { get; set; }
        public string? ResponseSummary { get; set; }
        public DateTime CreatedAt { get; set; }

        public TranxaSession? Session { get; set; }
    }
}
