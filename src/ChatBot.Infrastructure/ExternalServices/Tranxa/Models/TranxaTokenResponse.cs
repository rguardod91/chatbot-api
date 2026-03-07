namespace ChatBot.Infrastructure.ExternalServices.Tranxa.Models
{
    public class TranxaTokenResponse
    {
        public string access_token { get; set; } = null!;
        public int expires_in { get; set; }
        public string token_type { get; set; } = null!;
    }
}
