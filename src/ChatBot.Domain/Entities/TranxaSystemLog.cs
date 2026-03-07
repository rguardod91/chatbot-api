namespace ChatBot.Domain.Entities
{
    public class TranxaSystemLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Source { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string LogLevel { get; set; } = string.Empty;
        public string? TraceId { get; set; }
        public string? StackTrace { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}