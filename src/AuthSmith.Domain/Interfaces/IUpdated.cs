namespace AuthSmith.Domain.Interfaces;

/// <summary>
/// Interface for entities that track update timestamp.
/// </summary>
public interface IUpdated
{
    DateTime UpdatedAt { get; set; }
}

