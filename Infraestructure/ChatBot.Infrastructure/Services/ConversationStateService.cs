using ChatBot.Application.Interfaces.Persistence;
using ChatBot.Application.Interfaces.Services;
using ChatBot.Domain.Entities;
using ChatBot.Domain.Enums;
using System.Text.Json;

namespace ChatBot.Infrastructure.Services
{
    public class ConversationStateService : IConversationStateService
    {
        private readonly IRepository<TranxaSessionState> _stateRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ConversationStateService(
            IRepository<TranxaSessionState> stateRepository,
            IUnitOfWork unitOfWork)
        {
            _stateRepository = stateRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<ConversationStep> GetCurrentStepAsync(Guid sessionId)
        {
            var state = (await _stateRepository.FindAsync(s => s.SessionId == sessionId))
                .OrderByDescending(s => s.UpdatedAt)
                .FirstOrDefault();

            return state?.CurrentStep ?? ConversationStep.Start;
        }

        public async Task SetStepAsync(Guid sessionId, ConversationStep step, object? tempData = null)
        {
            var entity = new TranxaSessionState
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                CurrentStep = step,
                TempData = tempData != null ? JsonSerializer.Serialize(tempData) : null,
                UpdatedAt = DateTime.UtcNow
            };

            await _stateRepository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<T?> GetTempDataAsync<T>(Guid sessionId)
        {
            var state = (await _stateRepository.FindAsync(s => s.SessionId == sessionId))
                .OrderByDescending(s => s.UpdatedAt)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(state?.TempData))
                return default;

            return JsonSerializer.Deserialize<T>(state.TempData);
        }
    }
}
