using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using ChatBot.Application.Configuration;
using ChatBot.Application.Interfaces.External;
using Microsoft.Extensions.Options;

namespace ChatBot.Infrastructure.Services
{
    public class SecretsManagerService : ISecretsManagerService
    {
        private readonly AwsSettings _awsSettings;

        public SecretsManagerService(IOptions<AwsSettings> awsOptions)
        {
            _awsSettings = awsOptions.Value;
        }

        public async Task<string> GetSecretAsync()
        {
            var client = new AmazonSecretsManagerClient(
                RegionEndpoint.GetBySystemName(_awsSettings.Region));

            var request = new GetSecretValueRequest
            {
                SecretId = _awsSettings.SecretName,
                VersionStage = "AWSCURRENT"
            };

            var response = await client.GetSecretValueAsync(request);

            return response.SecretString ?? throw new Exception("Secret vacío.");
        }
    }
}
