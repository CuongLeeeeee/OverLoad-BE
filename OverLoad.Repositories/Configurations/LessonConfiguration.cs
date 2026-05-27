using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OverLoad.Domain.Entities;

namespace OverLoad.Repositories.Configurations;

public class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.ToTable("Lessons");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Title).IsRequired().HasMaxLength(300);
        builder.Property(l => l.Description).HasMaxLength(1000);
        builder.Property(l => l.Content).HasColumnType("nvarchar(max)");

        builder.HasMany(l => l.UserProgresses)
               .WithOne(p => p.Lesson)
               .HasForeignKey(p => p.LessonId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
