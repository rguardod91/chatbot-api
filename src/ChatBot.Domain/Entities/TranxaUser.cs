using ChatBot.Domain.ValueObjects;

namespace ChatBot.Domain.Entities
{
    public class TranxaUser
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public PhoneNumber WhatsAppNumber { get; set; } = null!;
        public string? DetectedName { get; set; }
        public DateTime FirstContactAt { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public ICollection<TranxaSession>? Sessions { get; set; }
    }
}