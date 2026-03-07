using ChatBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatBot.Infrastructure.Persistence.Configurations
{
    public class TranxaExternalServiceLogConfiguration : IEntityTypeConfiguration<TranxaExternalServiceLog>
    {
        public void Configure(EntityTypeBuilder<TranxaExternalServiceLog> builder)
        {
            builder.ToTable("TranxaExternalServiceLogs");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.ServiceName)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.Endpoint)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.HttpMethod)
                .HasMaxLength(10)
                .IsRequired();

            builder.Property(x => x.RequestSummary)
                .HasMaxLength(500);

            builder.Property(x => x.ResponseSummary)
                .HasMaxLength(500);

            builder.HasIndex(x => x.SessionId);
            builder.HasIndex(x => x.ServiceName);
            builder.HasIndex(x => x.CreatedAt);

            builder.HasOne(x => x.Session)
                .WithMany()
                .HasForeignKey(x => x.SessionId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
