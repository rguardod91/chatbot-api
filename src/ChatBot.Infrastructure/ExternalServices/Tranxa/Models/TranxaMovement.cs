namespace ChatBot.Infrastructure.ExternalServices.Tranxa.Models
{
    public class TranxaMovement
    {
        public string MvDate { get; set; } = default!;
        public string Reference { get; set; } = default!;
        public string Currency { get; set; } = default!;
        public string MvDet { get; set; } = default!;
        public string MvAmt { get; set; } = default!;
        public string MvSign { get; set; } = default!;
    }
}
