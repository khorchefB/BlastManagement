using BlastManagement.Api.Domain.Common;
using BlastManagement.Api.Domain.Holes;

namespace BlastManagement.Api.Domain.Events;

public sealed record BlastCreated(
    Guid BlastId,
    string Name,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    Guid IDomainEvent.AggregateId => BlastId;
}

public sealed record HoleAdded(
    Guid BlastId,
    Guid HoleId,
    string Name,
    Position Position,
    double Direction,
    double Inclination,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    Guid IDomainEvent.AggregateId => BlastId;
}

public sealed record HoleCharged(
    Guid BlastId,
    Guid HoleId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    Guid IDomainEvent.AggregateId => BlastId;
}

public sealed record HoleMarkedReady(
    Guid BlastId,
    Guid HoleId,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    Guid IDomainEvent.AggregateId => BlastId;
}

public sealed record BlastFired(
    Guid BlastId,
    DateTimeOffset DateBlasted,
    DateTimeOffset OccurredAt) : IDomainEvent
{
    Guid IDomainEvent.AggregateId => BlastId;
}
