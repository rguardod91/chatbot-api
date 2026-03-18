namespace ChatBot.Application.DTOs.Tranza
{
    public class CardDto
    {
        public string TokenId { get; set; } = default!;
        public string Pan { get; set; } = default!;
        public string Currency { get; set; } = default!;
        public string CurrBalance { get; set; } = default!;
        public int CardStatus { get; set; }
        public List<MovementDto> Movements { get; set; } = new();
    }
}
