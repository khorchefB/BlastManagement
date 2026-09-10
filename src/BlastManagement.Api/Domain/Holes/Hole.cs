using BlastManagement.Api.Domain.Common;
using BlastManagement.Api.Domain.Events;
using BlastManagement.Api.Domain.Exceptions;

namespace BlastManagement.Api.Domain.Holes;

/// <summary>
/// Event-sourced child entity owned by the Blast aggregate root.
/// It decides transitions, while the root records the resulting event in one atomic stream.
/// </summary>
public sealed class Hole
{

    public Guid Id;

    public Guid BlastId;

    public string Name;

    public Position Position;

    public double Direction;

    public double Inclination;

    public HoleStatus Status;

    private Hole()
    {
    }

    internal static Hole From(HoleAdded domainEvent)
    {
        var hole = new Hole();
        hole.Apply(domainEvent);
        return hole;
    }

    internal IDomainEvent Charge(DateTimeOffset now)
    {
        if (Status is HoleStatus.Charged or HoleStatus.Ready)
        {
            throw new DomainRuleViolationException(
                $"Hole '{Id}' cannot be charged because it is already {Status}.");
        }

        return new HoleCharged(BlastId, Id, now);
    }

    internal IDomainEvent MarkReady(DateTimeOffset now)
    {
        if (Status != HoleStatus.Charged)
        {
            throw new DomainRuleViolationException(
                $"Hole '{Id}' can only become Ready from Charged; current status is {Status}.");
        }

        return new HoleMarkedReady(BlastId, Id, now);
    }

    internal void Apply(IDomainEvent domainEvent)
    {
        switch (domainEvent)
        {
            case HoleAdded added:
                Id = added.HoleId;
                BlastId = added.BlastId;
                Name = added.Name;
                Position = added.Position;
                Direction = added.Direction;
                Inclination = added.Inclination;
                Status = HoleStatus.Planned;
                break;

            case HoleCharged charged when charged.HoleId == Id:
                Status = HoleStatus.Charged;
                break;

            case HoleMarkedReady ready when ready.HoleId == Id:
                Status = HoleStatus.Ready;
                break;

            default:
                throw new InvalidOperationException(
                    $"Event '{domainEvent.GetType().Name}' cannot be applied to hole '{Id}'.");
        }
    }
}
