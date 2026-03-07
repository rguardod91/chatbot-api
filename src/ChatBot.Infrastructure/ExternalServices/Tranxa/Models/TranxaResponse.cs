namespace ChatBot.Infrastructure.ExternalServices.Tranxa.Models
{
    public class TranxaResponse
    {
        public string OperDay { get; set; } = default!;
        public TranxaPerson Person { get; set; } = default!;
        public string Id { get; set; } = default!;
        public List<TranxaCard> Card { get; set; } = new();
        public string ApprovalCode { get; set; } = default!;
        public string Result { get; set; } = default!;
    }
}
