namespace BlastManagement.Api.Domain.Common;

/// <summary>
/// A fact that has already happened in the domain.
/// Events are immutable and are the only source used to rebuild aggregate state.
/// </summary>
public interface IDomainEvent
{
    Guid AggregateId { get; }

    DateTimeOffset OccurredAt { get; }
}
