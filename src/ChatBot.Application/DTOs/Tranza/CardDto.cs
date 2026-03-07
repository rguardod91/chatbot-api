namespace ChatBot.Application.DTOs.Tranza
{
    public class CardDto
    {
        public string TokenId { get; set; } = default!;
        public string Pan { get; set; } = default!;
        public string Currency { get; set; } = default!;
        public string CurrBalance { get; set; } = default!;

        public List<MovementDto> Movements { get; set; } = new();
    }
}
