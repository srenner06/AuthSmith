using AuthSmith.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthSmith.Infrastructure.EntityConfigurations;

/// <summary>
/// Entity configuration for AuditLog.
/// </summary>
public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(a => a.Id);

        // Store enum as string in database
        builder.Property(a => a.EventType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(100);

        builder.Property(a => a.UserName)
            .HasMaxLength(256);

        builder.Property(a => a.ApplicationKey)
            .HasMaxLength(100);

        builder.Property(a => a.IpAddress)
            .HasMaxLength(45); // IPv6 max length

        builder.Property(a => a.UserAgent)
            .HasMaxLength(500);

        builder.Property(a => a.Details)
            .HasColumnType("jsonb"); // PostgreSQL JSONB for efficient querying

        builder.Property(a => a.ErrorMessage)
            .HasMaxLength(1000);

        // Indexes for common queries
        builder.HasIndex(a => a.UserId);
        builder.HasIndex(a => a.ApplicationId);
        builder.HasIndex(a => a.EventType);
        builder.HasIndex(a => a.CreatedAt);
        builder.HasIndex(a => new { a.UserId, a.CreatedAt });
        builder.HasIndex(a => new { a.EventType, a.CreatedAt });

        // Relationships
        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull); // Keep audit logs even if user deleted

        builder.HasOne(a => a.Application)
            .WithMany()
            .HasForeignKey(a => a.ApplicationId)
            .OnDelete(DeleteBehavior.SetNull); // Keep audit logs even if app deleted
    }
}
