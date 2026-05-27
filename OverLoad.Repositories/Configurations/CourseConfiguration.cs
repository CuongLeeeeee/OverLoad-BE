using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OverLoad.Domain.Entities;

namespace OverLoad.Repositories.Configurations;

public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ToTable("Courses");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Title).IsRequired().HasMaxLength(300);
        builder.Property(c => c.Slug).IsRequired().HasMaxLength(300);
        builder.HasIndex(c => c.Slug).IsUnique();
        builder.Property(c => c.Description).HasMaxLength(2000);
        builder.Property(c => c.ThumbnailUrl).HasMaxLength(500);
        builder.Property(c => c.Category).HasMaxLength(100);
        builder.Property(c => c.Level).HasConversion<string>();

        builder.HasMany(c => c.Lessons)
               .WithOne(l => l.Course)
               .HasForeignKey(l => l.CourseId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Enrollments)
               .WithOne(e => e.Course)
               .HasForeignKey(e => e.CourseId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
