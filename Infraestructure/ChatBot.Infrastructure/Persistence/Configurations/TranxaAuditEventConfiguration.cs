using ChatBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatBot.Infrastructure.Persistence.Configurations
{
    public class TranxaAuditEventConfiguration : IEntityTypeConfiguration<TranxaAuditEvent>
    {
        public void Configure(EntityTypeBuilder<TranxaAuditEvent> builder)
        {
            builder.ToTable("TranxaAuditEvents");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.EventType)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.ExternalReference)
                .HasMaxLength(150);

            builder.Property(x => x.Result)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.Details)
                .HasMaxLength(500);

            builder.HasIndex(x => x.SessionId);
            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.EventType);

            builder.HasOne(x => x.Session)
                .WithMany()
                .HasForeignKey(x => x.SessionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
