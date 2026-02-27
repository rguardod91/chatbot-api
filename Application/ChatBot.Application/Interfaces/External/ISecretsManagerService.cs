namespace ChatBot.Application.Interfaces.External
{
    public interface ISecretsManagerService
    {
        Task<string> GetSecretAsync();
    }
}
