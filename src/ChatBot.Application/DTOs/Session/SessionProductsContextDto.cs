namespace ChatBot.Application.DTOs.Session
{
    public class SessionProductsContextDto
    {
        public List<CardContextDto> Cards { get; set; } = new();
        public string? SelectedTokenId { get; set; }
    }

    public class CardContextDto
    {
        public string TokenId { get; set; } = default!;
        public string MaskedPan { get; set; } = default!;
    }
}
