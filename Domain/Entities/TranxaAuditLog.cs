using System;

namespace ChatBot.Domain.Entities
{
    public class TranxaAuditLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Action { get; set; } = string.Empty;
        public string? Data { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}