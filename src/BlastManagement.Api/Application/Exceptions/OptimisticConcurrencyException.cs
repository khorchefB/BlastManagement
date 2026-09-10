namespace BlastManagement.Api.Application.Exceptions;

public sealed class OptimisticConcurrencyException(
    Guid streamId,
    long expectedVersion,
    long actualVersion)
    : Exception(
        $"Concurrency conflict on stream '{streamId}'. " +
        $"Expected version {expectedVersion}, but the current version is {actualVersion}.")
{
    public Guid StreamId { get; } = streamId;

    public long ExpectedVersion { get; } = expectedVersion;

    public long ActualVersion { get; } = actualVersion;
}
