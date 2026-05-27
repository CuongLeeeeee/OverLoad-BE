using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OverLoad.Domain.Entities;

namespace OverLoad.Repositories.Configurations;

public class UserLessonProgressConfiguration : IEntityTypeConfiguration<UserLessonProgress>
{
    public void Configure(EntityTypeBuilder<UserLessonProgress> builder)
    {
        builder.ToTable("UserLessonProgresses");
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => new { p.UserId, p.LessonId }).IsUnique();
        builder.Property(p => p.LastScrollPercentage).HasPrecision(5, 2);
    }
}
