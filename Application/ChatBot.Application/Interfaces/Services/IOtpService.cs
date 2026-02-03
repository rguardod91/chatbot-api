namespace ChatBot.Application.Interfaces.Services
{
    public interface IOtpService
    {
        void GenerateOtp(string userKey);
        bool ValidateOtp(string userKey, string code);
    }
}
