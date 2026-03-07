namespace ChatBot.Infrastructure.ExternalServices.Tranxa.Models
{
    public class TranxaCard
    {
        public string CurrBalance { get; set; } = default!;
        public string AvailBalance { get; set; } = default!;
        public string FinBalance { get; set; } = default!;
        public string Currency { get; set; } = default!;
        public string Pan { get; set; } = default!;
        public string TokenId { get; set; } = default!;
        public string CardType { get; set; } = default!;
        public string ContractName { get; set; } = default!;
        public int CardStatus { get; set; }
        public bool IsPinSet { get; set; }

        public List<TranxaMovement> Movements { get; set; } = new();
    }
}
