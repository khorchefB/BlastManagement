using BlastManagement.Api.Domain.Common;
using BlastManagement.Api.Domain.Events;
using BlastManagement.Api.Domain.Exceptions;
using BlastManagement.Api.Domain.Holes;

namespace BlastManagement.Api.Domain.Blasts;

public sealed class Blast : AggregateRoot
{
    private Blast()
    {
    }

    public Guid Id;

    public string Name;

    public DateTimeOffset? DateBlasted;

    private readonly Dictionary<Guid, Hole> holes = new();


    public BlastStatus Status
    {
        get
        {
            if (DateBlasted.HasValue)
            {
                return BlastStatus.Blasted;
            }

            return holes.Count > 0 &&
                   holes.Values.All(hole =>
                       hole.Status is HoleStatus.Charged or HoleStatus.Ready)
                ? BlastStatus.Loaded
                : BlastStatus.Planned;
        }
    }

    public IReadOnlyCollection<Hole> Holes =>
        holes.Values.OrderBy(hole => hole.Name, StringComparer.Ordinal).ToArray();

    public static Blast Create(Guid id, string name, DateTimeOffset now)
    {
        if (id == Guid.Empty)
        {
            throw new DomainRuleViolationException("Blast id cannot be empty.");
        }

        var blast = new Blast();
        blast.Raise(new BlastCreated(id, name, now));
        return blast;
    }

    internal static Blast Rehydrate(IEnumerable<IDomainEvent> history)
    {
        var blast = new Blast();
        blast.LoadFromHistory(history);

        if (blast.Id == Guid.Empty)
        {
            throw new InvalidOperationException(
                "A blast stream must start with a BlastCreated event.");
        }

        return blast;
    }

    public void AddHole(
        Guid holeId,
        string name,
        Position position,
        double direction,
        double inclination,
        DateTimeOffset now)
    {
        EnsureNotBlasted();

        // This keeps the public lifecycle monotonic: Planned -> Loaded -> Blasted.
        if (Status != BlastStatus.Planned)
        {
            throw new DomainRuleViolationException(
                "A hole can only be added while the blast is Planned.");
        }

        if (holeId == Guid.Empty)
        {
            throw new DomainRuleViolationException("Hole id cannot be empty.");
        }

        if (holes.ContainsKey(holeId))
        {
            throw new DomainRuleViolationException(
                $"Hole '{holeId}' already exists in blast '{Id}'.");
        }

        position.EnsureValid();
        EnsureDirectionIsValid(direction);
        EnsureFinite(inclination, "Inclination");

        Raise(new HoleAdded(
            Id,
            holeId,
            name,
            position,
            direction,
            inclination,
            now));
    }

    public void ChargeHole(Guid holeId, DateTimeOffset now)
    {
        EnsureNotBlasted();
        var hole = GetHole(holeId);
        Raise(hole.Charge(now));
    }

    public void MarkHoleReady(Guid holeId, DateTimeOffset now)
    {
        EnsureNotBlasted();
        var hole = GetHole(holeId);
        Raise(hole.MarkReady(now));
    }

    public void Fire(DateTimeOffset now, bool requireReadyToFire = false)
    {
        EnsureNotBlasted();

        if (holes.Count == 0)
        {
            throw new DomainRuleViolationException(
                "A blast cannot be fired without at least one hole.");
        }

        var invalidHoles = GetInvalidHoles(holes.Values, requireReadyToFire);

        if (invalidHoles.Count > 0)
        {
            var requiredState = requireReadyToFire ? "Ready" : "Charged or Ready";
            var invalidIds = string.Join(", ", invalidHoles.Select(hole => hole.Id));

            throw new DomainRuleViolationException(
                $"Blast '{Id}' cannot be fired. Every hole must be {requiredState}. " +
                $"Invalid holes: {invalidIds}.");
        }

        Raise(new BlastFired(Id, now, now));
    }

    private IList<Hole> GetInvalidHoles(IEnumerable<Hole> holes, bool requireReadyToFire)
        => requireReadyToFire ? holes.Where(hole => hole.Status != HoleStatus.Ready).ToList() :
                          holes.Where(hole => hole.Status is not (HoleStatus.Charged or HoleStatus.Ready)).ToArray();

    protected override void Apply(IDomainEvent domainEvent)
    {
        if (Id != Guid.Empty && domainEvent.AggregateId != Id)
        {
            throw new InvalidOperationException(
                $"Event for aggregate '{domainEvent.AggregateId}' cannot be applied to blast '{Id}'.");
        }

        switch (domainEvent)
        {
            case BlastCreated created when Id == Guid.Empty:
                Id = created.BlastId;
                Name = created.Name;
                break;

            case HoleAdded added:
                if (holes.ContainsKey(added.HoleId))
                {
                    throw new InvalidOperationException(
                        $"The event stream contains duplicate hole id '{added.HoleId}'.");
                }

                holes.Add(added.HoleId, Hole.From(added));
                break;

            case HoleCharged charged:
                GetHole(charged.HoleId).Apply(charged);
                break;

            case HoleMarkedReady ready:
                GetHole(ready.HoleId).Apply(ready);
                break;

            case BlastFired fired:
                DateBlasted = fired.DateBlasted;
                break;

            default:
                throw new InvalidOperationException(
                    $"Unknown blast event '{domainEvent.GetType().Name}'.");
        }
    }

    private Hole GetHole(Guid holeId)
    {
        if (!holes.TryGetValue(holeId, out var hole))
        {
            throw new HoleNotFoundException(Id, holeId);
        }

        return hole;
    }

    private void EnsureNotBlasted()
    {
        if (DateBlasted.HasValue)
        {
            throw new DomainRuleViolationException(
                $"Blast '{Id}' has already been fired at {DateBlasted:O}.");
        }
    }

    private static void EnsureDirectionIsValid(double direction)
    {
        EnsureFinite(direction, "Direction");

        if (direction is < 0 or > 360)
        {
            throw new DomainRuleViolationException(
                "Direction must be between 0 and 360 degrees.");
        }
    }

    private static void EnsureFinite(double value, string fieldName)
    {
        if (!double.IsFinite(value))
        {
            throw new DomainRuleViolationException(
                $"{fieldName} must be a finite number.");
        }
    }
}
