using ChatBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatBot.Infrastructure.Persistence.Configurations
{
    public class TranxaSessionConfiguration : IEntityTypeConfiguration<TranxaSession>
    {
        public void Configure(EntityTypeBuilder<TranxaSession> builder)
        {
            builder.ToTable("TranxaSessions");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();


            builder.Property(x => x.CurrentFlow)
                .HasMaxLength(100);

            builder.Property(x => x.FailedAttempts)
                .IsRequired();

            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.Status);

            builder.HasOne(x => x.User)
                .WithMany(u => u.Sessions)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
