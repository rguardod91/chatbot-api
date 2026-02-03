namespace ChatBot.Domain.Entities
{
    public class TranxaSystemLog
    {
        public Guid Id { get; set; }
        public string LogLevel { get; set; } = null!;
        public string Source { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string? Exception { get; set; }
        public string? TraceId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
