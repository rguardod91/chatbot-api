namespace ChatBot.Application.DTOs.WhatsApp
{
    public class IncomingWhatsAppMessageDto
    {
        public string From { get; set; } = default!;
        public string Text { get; set; } = default!;
    }
}
