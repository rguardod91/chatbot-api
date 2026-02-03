using ChatBot.Domain.Enums;

namespace ChatBot.Application.DTOs.Tranza.Models
{
    public class SessionContext
    {
        public ConversationStep Step { get; set; } = ConversationStep.Start;

        public string? DocumentType { get; set; }
        public string? DocumentNumber { get; set; }

        public List<CardDto> Cards { get; set; } = new();

        public string? SelectedTokenId { get; set; }

        // 🕒 Control de expiración por inactividad
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;
    }
}
