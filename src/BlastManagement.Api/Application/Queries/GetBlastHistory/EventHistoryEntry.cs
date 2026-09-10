using BlastManagement.Api.Domain.Common;

namespace BlastManagement.Api.Application.Queries.GetBlastHistory;

public sealed record EventHistoryEntry(
    long StreamVersion,
    string EventType,
    DateTimeOffset OccurredAt,
    IDomainEvent Event);
