using BlastManagement.Api.Domain.Common;

namespace BlastManagement.Api.Application.Abstractions;

public sealed record StoredEvent(
    Guid StreamId,
    long StreamVersion,
    IDomainEvent Event);
