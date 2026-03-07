namespace ChatBot.Infrastructure.ExternalServices.Base
{
    public interface IHttpService
    {
        Task<string> GetAsync(string url);
        Task<string> PostAsync(string url, object body);
    }
}
