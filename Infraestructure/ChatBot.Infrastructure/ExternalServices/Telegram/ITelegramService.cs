namespace ChatBot.Infrastructure.ExternalServices.Telegram
{
    public interface ITelegramService
    {
        Task SendMessageAsync(long chatId, string message);
    }

}
