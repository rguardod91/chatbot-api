using ChatBot.Domain.Enums;

namespace ChatBot.Application.Interfaces.Services
{
    public interface IConversationStateService
    {
        Task<ConversationStep> GetCurrentStepAsync(Guid sessionId);
        Task SetStepAsync(Guid sessionId, ConversationStep step, object? tempData = null);
        Task<T?> GetTempDataAsync<T>(Guid sessionId);
    }
}
