using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OverLoad.Domain.Entities;

namespace OverLoad.Repositories.Configurations;

public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.ToTable("Enrollments");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.UserId, e.CourseId }).IsUnique();
        builder.Property(e => e.ProgressPercentage).HasPrecision(5, 2);
    }
}
