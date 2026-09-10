using BlastManagement.Api.Application.Abstractions;
using BlastManagement.Api.Application.Exceptions;
using BlastManagement.Api.Domain.Common;
using System.Collections.Concurrent;

namespace BlastManagement.Api.Infrastructure.EventStore;

/// <summary>
/// In-memory append-only event store.
/// Stream version is the number of persisted events: empty = 0, first event = 1.
/// Appends compare expectedVersion with the current stream version to provide
/// optimistic concurrency control.
/// </summary>
public sealed class EventStoreInMemory : IEventStoreInMemory
{
    private readonly ConcurrentDictionary<Guid, EventStream> _streams = new();
    private readonly IReadOnlyList<IEventProjection> _projections;

    public EventStoreInMemory(IEnumerable<IEventProjection> projections)
    {
        _projections = projections.ToArray();
    }

    public ValueTask<IReadOnlyList<StoredEvent>> LoadAsync(
        Guid streamId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_streams.TryGetValue(streamId, out var stream))
        {
            return ValueTask.FromResult<IReadOnlyList<StoredEvent>>(
                Array.Empty<StoredEvent>());
        }

        lock (stream.SyncRoot)
        {
            IReadOnlyList<StoredEvent> snapshot = stream.Events.ToArray();
            return ValueTask.FromResult(snapshot);
        }
    }

    public ValueTask<IReadOnlyList<StoredEvent>> AppendAsync(
        Guid streamId,
        long expectedVersion,
        IReadOnlyCollection<IDomainEvent> events,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(events);
        cancellationToken.ThrowIfCancellationRequested();

        if (streamId == Guid.Empty)
        {
            throw new ArgumentException("Stream id cannot be empty.", nameof(streamId));
        }

        if (expectedVersion < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expectedVersion),
                "Expected version cannot be negative.");
        }

        if (events.Count == 0)
        {
            return ValueTask.FromResult<IReadOnlyList<StoredEvent>>(
                Array.Empty<StoredEvent>());
        }

        if (events.Any(domainEvent => domainEvent.AggregateId != streamId))
        {
            throw new ArgumentException(
                "Every appended event must belong to the target stream.",
                nameof(events));
        }

        var stream = _streams.GetOrAdd(streamId, _ => new EventStream());

        lock (stream.SyncRoot)
        {
            var currentVersion = stream.Events.Count;

            if (currentVersion != expectedVersion)
            {
                throw new OptimisticConcurrencyException(
                    streamId,
                    expectedVersion,
                    currentVersion);
            }

            var appended = events
                .Select((domainEvent, index) => new StoredEvent(
                    streamId,
                    currentVersion + index + 1L,
                    domainEvent))
                .ToArray();

            stream.Events.AddRange(appended);

            // Synchronous publication keeps this in-memory projection immediately
            // consistent for the exercise. A durable system would normally track
            // projection checkpoints and allow asynchronous replay/recovery.
            foreach (var storedEvent in appended)
            {
                foreach (var projection in _projections)
                {
                    projection.Project(storedEvent);
                }
            }

            return ValueTask.FromResult<IReadOnlyList<StoredEvent>>(appended);
        }
    }

    private sealed class EventStream
    {
        public object SyncRoot { get; } = new();

        public List<StoredEvent> Events { get; } = new();
    }
}
