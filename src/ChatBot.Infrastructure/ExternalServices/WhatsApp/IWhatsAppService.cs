namespace ChatBot.Infrastructure.ExternalServices.WhatsApp
{
    public interface IWhatsAppService
    {
        Task SendTextMessageAsync(string phoneNumberId, string to, string message);
    }

}
