using BlastManagement.Api.Application.Abstractions;
using BlastManagement.Api.Application.Queries.GetBlast;
using BlastManagement.Api.Domain.Blasts;
using BlastManagement.Api.Domain.Events;
using BlastManagement.Api.Domain.Holes;

namespace BlastManagement.Api.Infrastructure.Projections;

/// <summary>
/// Disposable in-memory read model built only from stored events.
/// Returned records are snapshots, so callers never share its mutable state.
/// </summary>
public sealed class BlastReadModelProjection : IEventProjection, IBlastReadStore
{
    private readonly object _syncRoot = new();
    private readonly Dictionary<Guid, MutableBlastView> _blasts = new();

    public void Project(StoredEvent storedEvent)
    {
        ArgumentNullException.ThrowIfNull(storedEvent);

        lock (_syncRoot)
        {
            switch (storedEvent.Event)
            {
                case BlastCreated created:
                    Project(created, storedEvent.StreamVersion);
                    break;

                case HoleAdded added:
                    Project(added, storedEvent.StreamVersion);
                    break;

                case HoleCharged charged:
                    Project(charged, storedEvent.StreamVersion);
                    break;

                case HoleMarkedReady ready:
                    Project(ready, storedEvent.StreamVersion);
                    break;

                case BlastFired fired:
                    Project(fired, storedEvent.StreamVersion);
                    break;

                default:
                    throw new InvalidOperationException(
                        $"No projection handler exists for event " +
                        $"'{storedEvent.Event.GetType().Name}'.");
            }
        }
    }

    public BlastReadModel? Get(Guid blastId)
    {
        lock (_syncRoot)
        {
            if (!_blasts.TryGetValue(blastId, out var blast))
            {
                return null;
            }

            var holes = blast.Holes.Values
                .OrderBy(hole => hole.Name, StringComparer.Ordinal)
                .Select(hole => new HoleReadModel(
                    hole.Id,
                    hole.BlastId,
                    hole.Name,
                    hole.Position,
                    hole.Direction,
                    hole.Inclination,
                    hole.Status))
                .ToArray();

            return new BlastReadModel(
                blast.Id,
                blast.Name,
                blast.DateBlasted,
                CalculateStatus(blast),
                blast.Version,
                holes);
        }
    }

    private void Project(BlastCreated domainEvent, long streamVersion)
    {
        if (streamVersion != 1)
        {
            throw new InvalidOperationException(
                $"BlastCreated must be the first event, but received version {streamVersion}.");
        }

        if (_blasts.ContainsKey(domainEvent.BlastId))
        {
            throw new InvalidOperationException(
                $"Projection already contains blast '{domainEvent.BlastId}'.");
        }

        _blasts.Add(domainEvent.BlastId, new MutableBlastView
        {
            Id = domainEvent.BlastId,
            Name = domainEvent.Name,
            Version = streamVersion
        });
    }

    private void Project(HoleAdded domainEvent, long streamVersion)
    {
        var blast = GetRequiredBlast(domainEvent.BlastId);
        EnsureNextVersion(blast, streamVersion);

        blast.Holes.Add(domainEvent.HoleId, new MutableHoleView
        {
            Id = domainEvent.HoleId,
            BlastId = domainEvent.BlastId,
            Name = domainEvent.Name,
            Position = domainEvent.Position,
            Direction = domainEvent.Direction,
            Inclination = domainEvent.Inclination,
            Status = HoleStatus.Planned
        });

        blast.Version = streamVersion;
    }

    private void Project(HoleCharged domainEvent, long streamVersion)
    {
        var blast = GetRequiredBlast(domainEvent.BlastId);
        EnsureNextVersion(blast, streamVersion);
        GetRequiredHole(blast, domainEvent.HoleId).Status = HoleStatus.Charged;
        blast.Version = streamVersion;
    }

    private void Project(HoleMarkedReady domainEvent, long streamVersion)
    {
        var blast = GetRequiredBlast(domainEvent.BlastId);
        EnsureNextVersion(blast, streamVersion);
        GetRequiredHole(blast, domainEvent.HoleId).Status = HoleStatus.Ready;
        blast.Version = streamVersion;
    }

    private void Project(BlastFired domainEvent, long streamVersion)
    {
        var blast = GetRequiredBlast(domainEvent.BlastId);
        EnsureNextVersion(blast, streamVersion);
        blast.DateBlasted = domainEvent.DateBlasted;
        blast.Version = streamVersion;
    }

    private MutableBlastView GetRequiredBlast(Guid blastId)
    {
        if (!_blasts.TryGetValue(blastId, out var blast))
        {
            throw new InvalidOperationException(
                $"Projection did not receive BlastCreated for '{blastId}'.");
        }

        return blast;
    }

    private static MutableHoleView GetRequiredHole(
        MutableBlastView blast,
        Guid holeId)
    {
        if (!blast.Holes.TryGetValue(holeId, out var hole))
        {
            throw new InvalidOperationException(
                $"Projection did not receive HoleAdded for '{holeId}'.");
        }

        return hole;
    }

    private static void EnsureNextVersion(
        MutableBlastView blast,
        long incomingVersion)
    {
        if (incomingVersion != blast.Version + 1)
        {
            throw new InvalidOperationException(
                $"Projection version gap for blast '{blast.Id}'. " +
                $"Expected {blast.Version + 1}, received {incomingVersion}.");
        }
    }

    private static BlastStatus CalculateStatus(MutableBlastView blast)
    {
        if (blast.DateBlasted.HasValue)
        {
            return BlastStatus.Blasted;
        }

        return blast.Holes.Count > 0 &&
               blast.Holes.Values.All(hole =>
                   hole.Status is HoleStatus.Charged or HoleStatus.Ready)
            ? BlastStatus.Loaded
            : BlastStatus.Planned;
    }

    private sealed class MutableBlastView
    {
        public Guid Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public DateTimeOffset? DateBlasted { get; set; }

        public long Version { get; set; }

        public Dictionary<Guid, MutableHoleView> Holes { get; } = new();
    }

    private sealed class MutableHoleView
    {
        public Guid Id { get; init; }

        public Guid BlastId { get; init; }

        public string Name { get; init; } = string.Empty;

        public Position Position { get; init; }

        public double Direction { get; init; }

        public double Inclination { get; init; }

        public HoleStatus Status { get; set; }
    }
}
