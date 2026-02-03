namespace ChatBot.Application.Interfaces.Services
{
    public interface IBotConversationEngine
    {
        Task<string> ProcessMessageAsync(string phoneNumber, string messageText);
    }
}
