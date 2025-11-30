using AuthSmith.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AuthSmith.Infrastructure.EntityConfigurations;

public class ApplicationConfiguration : IEntityTypeConfiguration<Application>
{
    public void Configure(EntityTypeBuilder<Application> builder)
    {
        builder.ToTable("Applications");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Key)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(a => a.ApiKeyHash)
            .HasMaxLength(512);

        // Store enum as string in database
        builder.Property(a => a.SelfRegistrationMode)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasIndex(a => a.Key)
            .IsUnique();

        builder.HasOne(a => a.DefaultRole)
            .WithMany()
            .HasForeignKey(a => a.DefaultRoleId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(a => a.Roles)
            .WithOne(r => r.Application)
            .HasForeignKey(r => r.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.Permissions)
            .WithOne(p => p.Application)
            .HasForeignKey(p => p.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

