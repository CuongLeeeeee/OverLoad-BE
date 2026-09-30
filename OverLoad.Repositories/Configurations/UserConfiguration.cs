using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OverLoad.Domain.Entities;

namespace OverLoad.Repositories.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property(u => u.FullName).IsRequired().HasMaxLength(200);
        builder.Property(u => u.AvatarUrl).HasMaxLength(500);
        builder.Property(u => u.Bio).HasMaxLength(1000);
        builder.Property(u => u.Role).HasConversion<string>();
        builder.Property(u => u.RefreshToken).HasMaxLength(500);

        // Columns that already exist in production, with the same types and defaults.
        builder.Property(u => u.IsLocked).HasDefaultValue(false);
        builder.Property(u => u.StudentVerificationStatus).IsRequired().HasMaxLength(50).HasDefaultValue("NONE");
        builder.Property(u => u.StudentCardPath).HasMaxLength(500);
        builder.Property(u => u.HasSeenStudentRejection).HasDefaultValue(false);
        builder.Property(u => u.AiQuestionsAskedToday).HasDefaultValue(0);
        builder.Property(u => u.LastAiQuestionDate).HasDefaultValue(DateTime.MinValue);

        // Concurrency token: a wallet debit is written as "UPDATE ... WHERE WalletBalance = <value read>",
        // so two simultaneous purchases cannot both spend the same money.
        builder.Property(u => u.WalletBalance).HasPrecision(18, 2).HasDefaultValue(0m).IsConcurrencyToken();
        // Same for PRO: two renewals landing at once must stack, not overwrite each other.
        builder.Property(u => u.ProExpiresAt).IsConcurrencyToken();

        builder.HasMany(u => u.Enrollments)
               .WithOne(e => e.User)
               .HasForeignKey(e => e.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.LessonProgresses)
               .WithOne(p => p.User)
               .HasForeignKey(p => p.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
