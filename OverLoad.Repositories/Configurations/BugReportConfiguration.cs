using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OverLoad.Domain.Entities;

namespace OverLoad.Repositories.Configurations;

/// <summary>Mirrors the production BugReports table exactly (names, lengths, delete rules).</summary>
public class BugReportConfiguration : IEntityTypeConfiguration<BugReport>
{
    public void Configure(EntityTypeBuilder<BugReport> builder)
    {
        builder.ToTable("BugReports");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Title).IsRequired().HasMaxLength(250);
        builder.Property(b => b.Description).IsRequired().HasMaxLength(2000);

        builder.HasOne(b => b.Course)
               .WithMany()
               .HasForeignKey(b => b.CourseId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.Lesson)
               .WithMany()
               .HasForeignKey(b => b.LessonId)
               .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(b => b.User)
               .WithMany()
               .HasForeignKey(b => b.UserId)
               .OnDelete(DeleteBehavior.NoAction);
    }
}
