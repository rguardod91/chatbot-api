using ChatBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatBot.Infrastructure.Persistence.Configurations
{
    public class TranxaAuditLogConfig : IEntityTypeConfiguration<TranxaAuditLog>
    {
        public void Configure(EntityTypeBuilder<TranxaAuditLog> builder)
        {
            builder.ToTable("TranxaAuditLog");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.UserChannelId).HasMaxLength(50).IsRequired();
            builder.Property(x => x.Action).HasMaxLength(50).IsRequired();
            builder.Property(x => x.TokenId).HasMaxLength(100).IsRequired();
            builder.Property(x => x.Result).HasMaxLength(50).IsRequired();
            builder.Property(x => x.ApprovalCode).HasMaxLength(20);
            builder.Property(x => x.DeclineReason).HasMaxLength(200);
            builder.Property(x => x.CreatedAt).IsRequired();
        }
    }
}
