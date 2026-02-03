using ChatBot.Domain.Entities;
using ChatBot.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace ChatBot.Infrastructure.Persistence.Context
{
    public class TranxaDbContext : DbContext
    {
        public TranxaDbContext(DbContextOptions<TranxaDbContext> options) : base(options) { }

        public DbSet<TranxaUser> Users => Set<TranxaUser>();
        public DbSet<TranxaSession> Sessions => Set<TranxaSession>();
        public DbSet<TranxaMessage> Messages => Set<TranxaMessage>();
        public DbSet<TranxaSessionState> SessionStates => Set<TranxaSessionState>();
        public DbSet<TranxaAuditEvent> AuditEvents => Set<TranxaAuditEvent>();
        public DbSet<TranxaSystemLog> SystemLogs => Set<TranxaSystemLog>();
        public DbSet<TranxaExternalServiceLog> ExternalServiceLogs => Set<TranxaExternalServiceLog>();
        public DbSet<TranxaAuditLog> TranxaAuditLogs => Set<TranxaAuditLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("Tranxa");
            modelBuilder.ApplyConfiguration(new TranxaAuditLogConfig());
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(TranxaDbContext).Assembly);
        }
    }
}
