namespace ChatBot.Application.DTOs
{
    public class IncomingMessageDto
    {
        public string PhoneNumber { get; set; } = null!;
        public string MessageText { get; set; } = null!;
    }
}
