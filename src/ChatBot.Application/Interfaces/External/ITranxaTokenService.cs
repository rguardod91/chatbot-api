namespace ChatBot.Application.Interfaces.External
{
    public interface ITranxaTokenService
    {
        Task<string> GetTokenAsync();
    }
}
