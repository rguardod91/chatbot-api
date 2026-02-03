using ChatBot.Application.DTOs.Tranza.Models;
using ChatBot.Application.Interfaces.Services;
using Microsoft.Extensions.Caching.Memory;

namespace ChatBot.Infrastructure.Services;

public class SessionManager : ISessionManager
{
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan SessionTimeout = TimeSpan.FromMinutes(5);

    public SessionManager(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task<SessionContext> GetSessionAsync(string userId)
    {
        if (!_cache.TryGetValue(userId, out SessionContext context))
        {
            context = new SessionContext();
            _cache.Set(userId, context, SessionTimeout);
        }

        return Task.FromResult(context);
    }

    public Task SaveSessionAsync(string userId, SessionContext context)
    {
        _cache.Set(userId, context, SessionTimeout);
        return Task.CompletedTask;
    }
}
