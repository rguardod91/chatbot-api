namespace ChatBot.Application.Configuration
{
    public class AwsSettings
    {
        public string Region { get; set; } = string.Empty;
        public string SecretName { get; set; } = string.Empty;
        public bool UseSecretsManager { get; set; }
    }
}
