using ChatBot.Domain.Enums;

namespace ChatBot.Application.DTOs.Tranza.Models
{
    public class SessionContext
    {
        //--------------------------------------
        // FLOW STATE
        //--------------------------------------

        public ConversationStep Step { get; set; } = ConversationStep.Start;

        //--------------------------------------
        // USER DATA
        //--------------------------------------

        public string? DocumentType { get; set; }
        public string? DocumentNumber { get; set; }

        public List<CardDto> Cards { get; set; } = new();

        public string? SelectedTokenId { get; set; }
        public string? UserEmail { get; set; }

        public bool IsAuthenticated { get; set; }

        //--------------------------------------
        // OTP CONTROL
        //--------------------------------------

        public int OtpAttempts { get; set; } = 0;

        public DateTime OtpGeneratedAt { get; set; } = DateTime.UtcNow;

        //--------------------------------------
        // STEP ATTEMPTS CONTROL
        //--------------------------------------

        // Controla intentos por etapa (documento, producto, menu, etc)
        public int StepAttempts { get; set; } = 0;

        //--------------------------------------
        // LOOP DETECTION
        //--------------------------------------

        // Último mensaje recibido
        public string? LastMessage { get; set; }

        // Número de repeticiones del mismo mensaje
        public int LoopCount { get; set; } = 0;

        //--------------------------------------
        // SESSION TIMEOUT
        //--------------------------------------

        // Control de expiración por inactividad
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;

        //--------------------------------------
        // RESET METHODS
        //--------------------------------------

        public void ResetOtp()
        {
            OtpAttempts = 0;
            OtpGeneratedAt = DateTime.UtcNow;
        }

        public void ResetStepAttempts()
        {
            StepAttempts = 0;
        }

        public void ResetLoopDetection()
        {
            LoopCount = 0;
            LastMessage = null;
        }

        public void ResetSession()
        {
            Step = ConversationStep.Start;
            DocumentType = null;
            DocumentNumber = null;
            Cards.Clear();
            SelectedTokenId = null;
            UserEmail = null;
            IsAuthenticated = false;

            ResetOtp();
            ResetStepAttempts();
            ResetLoopDetection();

            LastActivity = DateTime.UtcNow;
        }
    }
}