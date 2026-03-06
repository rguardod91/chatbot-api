namespace ChatBot.Application.Interfaces.Services
{
    public interface IBotConversationEngine
    {
        Task<List<string>> ProcessMessageAsync(string userId, string message);
    }
}
