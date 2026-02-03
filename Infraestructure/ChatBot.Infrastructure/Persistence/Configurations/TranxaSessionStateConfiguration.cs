using ChatBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatBot.Infrastructure.Persistence.Configurations
{
    public class TranxaSessionStateConfiguration : IEntityTypeConfiguration<TranxaSessionState>
    {
        public void Configure(EntityTypeBuilder<TranxaSessionState> builder)
        {
            builder.ToTable("TranxaSessionStates");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.CurrentStep)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();


            builder.Property(x => x.TempData)
                .HasColumnType("NVARCHAR(MAX)");

            builder.HasIndex(x => x.SessionId);

            builder.HasOne(x => x.Session)
                .WithMany(s => s.States)
                .HasForeignKey(x => x.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
