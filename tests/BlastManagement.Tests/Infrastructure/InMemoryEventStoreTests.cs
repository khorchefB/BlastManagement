using BlastManagement.Api.Application.Abstractions;
using BlastManagement.Api.Application.Exceptions;
using BlastManagement.Api.Domain.Common;
using BlastManagement.Api.Domain.Events;
using BlastManagement.Api.Infrastructure.EventStore;

namespace BlastManagement.Tests.Infrastructure;

public sealed class InMemoryEventStoreTests
{
    [Fact]
    public async Task Append_AssignsMonotonicStreamVersions()
    {
        var store = new EventStoreInMemory(Array.Empty<IEventProjection>());
        var blastId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        await store.AppendAsync(
            blastId,
            expectedVersion: 0,
            new IDomainEvent[] { new BlastCreated(blastId, "B-001", now) });

        await store.AppendAsync(
            blastId,
            expectedVersion: 1,
            new IDomainEvent[] { new BlastFired(blastId, now, now) });

        var stream = await store.LoadAsync(blastId);

        Assert.Collection(
            stream,
            first => Assert.Equal(1, first.StreamVersion),
            second => Assert.Equal(2, second.StreamVersion));
    }

    [Fact]
    public async Task Append_WithStaleExpectedVersion_ThrowsConflict()
    {
        var store = new EventStoreInMemory(Array.Empty<IEventProjection>());
        var blastId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        await store.AppendAsync(
            blastId,
            expectedVersion: 0,
            new IDomainEvent[] { new BlastCreated(blastId, "B-001", now) });

        var exception = await Assert.ThrowsAsync<OptimisticConcurrencyException>(
            async () =>
            {
                await store.AppendAsync(
                    blastId,
                    expectedVersion: 0,
                    new IDomainEvent[] { new BlastFired(blastId, now, now) });
            });

        Assert.Equal(0, exception.ExpectedVersion);
        Assert.Equal(1, exception.ActualVersion);
    }

    [Fact]
    public async Task Append_PublishesEveryStoredEventToProjection()
    {
        var projection = new RecordingProjection();
        var store = new EventStoreInMemory(new[] { projection });
        var blastId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        await store.AppendAsync(
            blastId,
            expectedVersion: 0,
            new IDomainEvent[] { new BlastCreated(blastId, "B-001", now) });

        var projected = Assert.Single(projection.Events);
        Assert.Equal(1, projected.StreamVersion);
        Assert.IsType<BlastCreated>(projected.Event);
    }

    private sealed class RecordingProjection : IEventProjection
    {
        public List<StoredEvent> Events { get; } = new();

        public void Project(StoredEvent storedEvent) => Events.Add(storedEvent);
    }
}
