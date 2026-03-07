namespace ChatBot.Application.DTOs.Tranza
{
    public class OtpValidationResponseDto
    {
        public string? Message { get; set; }
        public int? Status { get; set; }
        public string Result { get; set; } = string.Empty;
        public string? DeclineReason { get; set; }
    }
}
