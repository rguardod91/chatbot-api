namespace ChatBot.Domain.Entities
{
    public class TranxaAuditLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string UserChannelId { get; set; } = string.Empty; // Telegram o WhatsApp
        public string Action { get; set; } = string.Empty;
        public string TokenId { get; set; } = string.Empty;

        public string Result { get; set; } = string.Empty;
        public string? ApprovalCode { get; set; }
        public string? DeclineReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
