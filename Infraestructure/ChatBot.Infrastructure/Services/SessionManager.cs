using ChatBot.Application.DTOs.Tranza.Models;
using ChatBot.Application.Interfaces.Services;
using Microsoft.Extensions.Caching.Memory;

public class SessionManager : ISessionManager
{
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan SessionTimeout = TimeSpan.FromMinutes(5);

    public SessionManager(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task<SessionContext?> GetSessionAsync(string userId)
    {
        _cache.TryGetValue(userId, out SessionContext? context);
        return Task.FromResult(context);
    }

    public Task SaveSessionAsync(string userId, SessionContext context)
    {
        var options = new MemoryCacheEntryOptions
        {
            SlidingExpiration = SessionTimeout
        };

        _cache.Set(userId, context, options);
        return Task.CompletedTask;
    }
}