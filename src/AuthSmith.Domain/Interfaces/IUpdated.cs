namespace AuthSmith.Domain.Interfaces;

/// <summary>
/// Interface for entities that track last update timestamp.
/// </summary>
public interface IUpdated
{
    DateTimeOffset? UpdatedAt { get; set; }
}

