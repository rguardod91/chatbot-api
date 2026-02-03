using ChatBot.Application.Interfaces.Persistence;
using ChatBot.Application.Interfaces.Services;
using ChatBot.Domain.Entities;
using ChatBot.Domain.Enums;
using System.Text.Json;

namespace ChatBot.Infrastructure.ExternalServices.Services
{
    public class ConversationStateService : IConversationStateService
    {
        private readonly IRepository<TranxaSessionState> _sessionStateRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ConversationStateService(
            IRepository<TranxaSessionState> sessionStateRepository,
            IUnitOfWork unitOfWork)
        {
            _sessionStateRepository = sessionStateRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<ConversationStep> GetCurrentStepAsync(Guid sessionId)
        {
            var state = (await _sessionStateRepository.FindAsync(x => x.SessionId == sessionId))
                .OrderByDescending(x => x.UpdatedAt)
                .FirstOrDefault();

            return state?.CurrentStep ?? ConversationStep.Start;
        }

        public async Task SetStepAsync(Guid sessionId, ConversationStep step, object? tempData = null)
        {
            var state = new TranxaSessionState
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                CurrentStep = step,
                TempData = tempData != null ? JsonSerializer.Serialize(tempData) : null,
                UpdatedAt = DateTime.UtcNow
            };

            await _sessionStateRepository.AddAsync(state);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<T?> GetTempDataAsync<T>(Guid sessionId)
        {
            var state = (await _sessionStateRepository.FindAsync(x => x.SessionId == sessionId))
                .OrderByDescending(x => x.UpdatedAt)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(state?.TempData))
                return default;

            return JsonSerializer.Deserialize<T>(state.TempData);
        }
    }
}
