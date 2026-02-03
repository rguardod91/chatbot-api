using ChatBot.Domain.ValueObjects;

namespace ChatBot.Domain.Entities
{
    public class TranxaUser
    {
        public Guid Id { get; set; }
        public PhoneNumber WhatsAppNumber { get; set; } = null!;
        public string? DetectedName { get; set; }
        public DateTime FirstContactAt { get; set; }
        public DateTime? LastInteractionAt { get; set; }
        public bool IsActive { get; set; }

        public ICollection<TranxaSession> Sessions { get; set; } = new List<TranxaSession>();
    }
}