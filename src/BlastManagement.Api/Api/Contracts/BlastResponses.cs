namespace BlastManagement.Api.Api.Contracts;

public sealed record CreatedBlastResponse(Guid Id);

public sealed record AddedHoleResponse(Guid Id, Guid BlastId);

public sealed record EventHistoryResponse(
    long StreamVersion,
    string EventType,
    DateTimeOffset OccurredAt,
    object Payload);
