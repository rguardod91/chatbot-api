namespace ChatBot.Application.Configurations
{
    public class AwsSecretsConfig
    {
        public ConnectionStringsConfig ConnectionStrings { get; set; } = new();
        public TranxaConfig Tranxa { get; set; } = new();
        public WhatsAppConfig WhatsApp { get; set; } = new();
    }

    public class ConnectionStringsConfig
    {
        public string DefaultConnection { get; set; } = string.Empty;
    }

    public class TranxaConfig
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string BaseUrlUltraRed { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
    }

    public class WhatsAppConfig
    {
        public string AccessToken { get; set; } = string.Empty;
        public string PhoneNumberId { get; set; } = string.Empty;
        public string VerifyToken { get; set; } = string.Empty;
    }
}
