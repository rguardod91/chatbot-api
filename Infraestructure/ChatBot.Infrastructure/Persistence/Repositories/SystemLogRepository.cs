using ChatBot.Application.Interfaces.Persistence;
using ChatBot.Domain.Entities;
using ChatBot.Infrastructure.Persistence.Context;

namespace ChatBot.Infrastructure.Persistence.Repositories;

public class SystemLogRepository : ISystemLogRepository
{
    private readonly TranxaDbContext _context;

    public SystemLogRepository(TranxaDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(TranxaSystemLog log)
    {
        await _context.SystemLogs.AddAsync(log);
    }
}