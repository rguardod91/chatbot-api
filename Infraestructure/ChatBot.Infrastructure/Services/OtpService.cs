using ChatBot.Application.Interfaces.Services;

namespace ChatBot.Infrastructure.Services
{
    public class OtpService : IOtpService
    {
        private static readonly Dictionary<string, string> _store = new();

        public void GenerateOtp(string userKey)
        {
            var rnd = new Random();
            var code = rnd.Next(1000, 9999).ToString();
            _store[userKey] = code;
            Console.WriteLine($"[OTP SIMULADO] Código para {userKey}: {code}");
        }

        public bool ValidateOtp(string userKey, string code)
        {
            return _store.ContainsKey(userKey) && _store[userKey] == code;
        }
    }
}
