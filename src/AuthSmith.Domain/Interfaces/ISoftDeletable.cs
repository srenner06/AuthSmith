namespace AuthSmith.Domain.Interfaces;

/// <summary>
/// Interface for entities that support soft deletion.
/// Reserved for future use.
/// </summary>
public interface ISoftDeletable
{
    DateTime? DeletedAt { get; set; }
}

