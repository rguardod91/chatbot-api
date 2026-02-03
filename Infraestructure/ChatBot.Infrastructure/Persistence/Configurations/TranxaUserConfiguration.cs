using ChatBot.Domain.Entities;
using ChatBot.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatBot.Infrastructure.Persistence.Configurations
{
    public class TranxaUserConfiguration : IEntityTypeConfiguration<TranxaUser>
    {
        public void Configure(EntityTypeBuilder<TranxaUser> builder)
        {
            builder.ToTable("TranxaUsers");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.WhatsAppNumber)
                .HasConversion(
                    v => v.Value,
                    v => new PhoneNumber(v))
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.DetectedName)
                .HasMaxLength(150);

            builder.Property(x => x.FirstContactAt)
                .IsRequired();

            builder.Property(x => x.IsActive)
                .IsRequired();

            builder.HasIndex(x => x.WhatsAppNumber)
                .IsUnique();
        }
    }
}
