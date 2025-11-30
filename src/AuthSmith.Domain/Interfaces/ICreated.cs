namespace AuthSmith.Domain.Interfaces;

/// <summary>
/// Interface for entities that track creation timestamp.
/// </summary>
public interface ICreated
{
    DateTimeOffset CreatedAt { get; set; }
}

