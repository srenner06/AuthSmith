using AuthSmith.Domain.Entities;
using AuthSmith.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AuthSmith.Infrastructure;

/// <summary>
/// Entity Framework Core DbContext for AuthSmith.
/// Automatically sets CreatedAt and UpdatedAt via ChangeTracker.
/// </summary>
public class AuthSmithDbContext : DbContext
{
    public AuthSmithDbContext(DbContextOptions<AuthSmithDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Application> Applications => Set<Application>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserPermission> UserPermissions => Set<UserPermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuthSmithDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is ICreated || e.Entity is IUpdated)
            .ToList();

        var now = DateTimeOffset.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.Entity is ICreated created && entry.State == EntityState.Added)
            {
                created.CreatedAt = now;
            }

            if (entry.Entity is IUpdated updated && (entry.State == EntityState.Added || entry.State == EntityState.Modified))
            {
                updated.UpdatedAt = now;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}

