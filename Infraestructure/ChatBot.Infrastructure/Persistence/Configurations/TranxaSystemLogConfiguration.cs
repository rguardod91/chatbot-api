using ChatBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatBot.Infrastructure.Persistence.Configurations
{
    public class TranxaSystemLogConfiguration : IEntityTypeConfiguration<TranxaSystemLog>
    {
        public void Configure(EntityTypeBuilder<TranxaSystemLog> builder)
        {
            builder.ToTable("TranxaSystemLogs");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.LogLevel)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.Source)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.Message)
                .HasMaxLength(1000)
                .IsRequired();

            builder.Property(x => x.TraceId)
                .HasMaxLength(100);

            builder.HasIndex(x => x.LogLevel);
            builder.HasIndex(x => x.CreatedAt);
        }
    }
}
