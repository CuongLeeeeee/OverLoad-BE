using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OverLoad.Domain.Entities;

namespace OverLoad.Repositories.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions");

        builder.HasKey(t => t.TransactionId);

        builder.Property(t => t.TransactionId)
            .HasColumnName("transaction_id")
            .HasMaxLength(50);

        builder.Property(t => t.OrderCode)
            .HasColumnName("order_code")
            .IsRequired();
        builder.HasIndex(t => t.OrderCode).IsUnique();

        builder.Property(t => t.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(t => t.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.Method)
            .HasColumnName("method")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Concurrency token: a Pending -> Success transition only applies once, even when the
        // webhook and the status sync race each other.
        builder.Property(t => t.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired()
            .IsConcurrencyToken();
        builder.HasIndex(t => new { t.UserId, t.Status });

        builder.Property(t => t.CourseId)
            .HasColumnName("course_id");

        builder.Property(t => t.ProPackage)
            .HasColumnName("pro_package")
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(t => t.Amount)
            .HasColumnName("amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(t => t.Currency)
            .HasColumnName("currency")
            .HasMaxLength(10)
            .HasDefaultValue("VND");

        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.Property(t => t.PaidAt)
            .HasColumnName("paid_at");

        // Relationships
        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Course)
            .WithMany()
            .HasForeignKey(t => t.CourseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
