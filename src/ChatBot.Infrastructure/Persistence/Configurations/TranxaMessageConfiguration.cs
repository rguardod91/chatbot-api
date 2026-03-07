using ChatBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatBot.Infrastructure.Persistence.Configurations
{
    public class TranxaMessageConfiguration : IEntityTypeConfiguration<TranxaMessage>
    {
        public void Configure(EntityTypeBuilder<TranxaMessage> builder)
        {
            builder.ToTable("TranxaMessages");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Direction)
                .HasConversion<string>()
                .HasMaxLength(10)
                .IsRequired();


            builder.Property(x => x.MessageType)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.Content)
                .HasMaxLength(1000);

            builder.HasIndex(x => x.SessionId);
            builder.HasIndex(x => x.CreatedAt);

            builder.HasOne(x => x.Session)
                .WithMany(s => s.Messages)
                .HasForeignKey(x => x.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
