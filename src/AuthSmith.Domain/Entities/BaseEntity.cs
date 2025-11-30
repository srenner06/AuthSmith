using AuthSmith.Domain.Interfaces;

namespace AuthSmith.Domain.Entities;

/// <summary>
/// Base entity class that all domain entities inherit from.
/// Provides common properties and implements audit interfaces.
/// </summary>
public abstract class BaseEntity : ICreated, IUpdated
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

