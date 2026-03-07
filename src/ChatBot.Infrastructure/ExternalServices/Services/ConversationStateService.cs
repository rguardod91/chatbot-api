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
            Console.WriteLine("===============================================");
            Console.WriteLine("[CONVERSACION] Consultando estado actual");
            Console.WriteLine($"[CONVERSACION] SessionId: {sessionId}");

            try
            {
                var state = (await _sessionStateRepository.FindAsync(x => x.SessionId == sessionId))
                    .OrderByDescending(x => x.UpdatedAt)
                    .FirstOrDefault();

                var step = state?.CurrentStep ?? ConversationStep.Start;

                Console.WriteLine($"[CONVERSACION] Paso actual: {step}");

                return step;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[CONVERSACION] ERROR consultando estado");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task SetStepAsync(Guid sessionId, ConversationStep step, object? tempData = null)
        {
            Console.WriteLine("===============================================");
            Console.WriteLine("[CONVERSACION] Guardando nuevo estado");
            Console.WriteLine($"[CONVERSACION] SessionId: {sessionId}");
            Console.WriteLine($"[CONVERSACION] Nuevo paso: {step}");

            try
            {
                var serializedTempData = tempData != null
                    ? JsonSerializer.Serialize(tempData)
                    : null;

                if (serializedTempData != null)
                    Console.WriteLine($"[CONVERSACION] TempData: {serializedTempData}");

                var state = new TranxaSessionState
                {
                    Id = Guid.NewGuid(),
                    SessionId = sessionId,
                    CurrentStep = step,
                    TempData = serializedTempData,
                    UpdatedAt = DateTime.UtcNow
                };

                await _sessionStateRepository.AddAsync(state);
                await _unitOfWork.SaveChangesAsync();

                Console.WriteLine("[CONVERSACION] Estado guardado correctamente");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[CONVERSACION] ERROR guardando estado");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task<T?> GetTempDataAsync<T>(Guid sessionId)
        {
            Console.WriteLine("===============================================");
            Console.WriteLine("[CONVERSACION] Recuperando TempData");
            Console.WriteLine($"[CONVERSACION] SessionId: {sessionId}");

            try
            {
                var state = (await _sessionStateRepository.FindAsync(x => x.SessionId == sessionId))
                    .OrderByDescending(x => x.UpdatedAt)
                    .FirstOrDefault();

                if (string.IsNullOrWhiteSpace(state?.TempData))
                {
                    Console.WriteLine("[CONVERSACION] No hay TempData almacenado");
                    return default;
                }

                Console.WriteLine($"[CONVERSACION] TempData encontrado: {state.TempData}");

                return JsonSerializer.Deserialize<T>(state.TempData);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[CONVERSACION] ERROR recuperando TempData");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }
    }
}