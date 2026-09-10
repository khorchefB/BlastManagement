using BlastManagement.Api.Domain.Blasts;
using BlastManagement.Api.Domain.Events;
using BlastManagement.Api.Domain.Exceptions;
using BlastManagement.Api.Domain.Holes;

namespace BlastManagement.Tests.Domain;

public sealed class BlastTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 9, 9, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateBlast_RaisesBlastCreated()
    {
        var blastId = Guid.NewGuid();

        var blast = Blast.Create(blastId, " B-042 ", Now);

        var domainEvent = Assert.IsType<BlastCreated>(
            Assert.Single(blast.GetUncommittedEvents()));

        Assert.Equal(blastId, blast.Id);
        Assert.Equal("B-042", blast.Name);
        Assert.Equal(BlastStatus.Planned, blast.Status);
        Assert.Equal(blastId, domainEvent.BlastId);
    }

    [Fact]
    public void ChargeHole_WhenAlreadyCharged_IsRejected()
    {
        var (blast, holeId) = CreateBlastWithOneHole();
        blast.ChargeHole(holeId, Now.AddMinutes(1));

        var exception = Assert.Throws<DomainRuleViolationException>(() =>
            blast.ChargeHole(holeId, Now.AddMinutes(2)));

        Assert.Contains("already Charged", exception.Message);
    }

    [Fact]
    public void Fire_WhenAHoleIsStillPlanned_IsRejected()
    {
        var (blast, _) = CreateBlastWithOneHole();

        var exception = Assert.Throws<DomainRuleViolationException>(() =>
            blast.Fire(Now.AddMinutes(1)));

        Assert.Contains("Every hole must be Charged or Ready", exception.Message);
    }

    [Fact]
    public void Fire_CoreRule_AcceptsChargedHoles()
    {
        var (blast, holeId) = CreateBlastWithOneHole();
        blast.ChargeHole(holeId, Now.AddMinutes(1));

        blast.Fire(Now.AddMinutes(2));

        Assert.Equal(BlastStatus.Blasted, blast.Status);
        Assert.Equal(Now.AddMinutes(2), blast.DateBlasted);
        Assert.IsType<BlastFired>(blast.GetUncommittedEvents().Last());
    }

    [Fact]
    public void Fire_StrictBonusRule_RequiresReadyHoles()
    {
        var (blast, holeId) = CreateBlastWithOneHole();
        blast.ChargeHole(holeId, Now.AddMinutes(1));

        Assert.Throws<DomainRuleViolationException>(() =>
            blast.Fire(Now.AddMinutes(2), requireReadyToFire: true));

        blast.MarkHoleReady(holeId, Now.AddMinutes(3));
        blast.Fire(Now.AddMinutes(4), requireReadyToFire: true);

        Assert.Equal(BlastStatus.Blasted, blast.Status);
    }

    [Fact]
    public void MarkHoleReady_OnlyAllowsChargedToReady()
    {
        var (blast, holeId) = CreateBlastWithOneHole();

        Assert.Throws<DomainRuleViolationException>(() =>
            blast.MarkHoleReady(holeId, Now.AddMinutes(1)));

        blast.ChargeHole(holeId, Now.AddMinutes(2));
        blast.MarkHoleReady(holeId, Now.AddMinutes(3));

        Assert.Equal(HoleStatus.Ready, Assert.Single(blast.Holes).Status);
    }

    [Fact]
    public void Status_IsDerivedFromReplayedDomainFacts()
    {
        var (blast, holeId) = CreateBlastWithOneHole();
        Assert.Equal(BlastStatus.Planned, blast.Status);

        blast.ChargeHole(holeId, Now.AddMinutes(1));
        Assert.Equal(BlastStatus.Loaded, blast.Status);

        blast.Fire(Now.AddMinutes(2));
        Assert.Equal(BlastStatus.Blasted, blast.Status);
    }

    private static (Blast Blast, Guid HoleId) CreateBlastWithOneHole()
    {
        var blast = Blast.Create(Guid.NewGuid(), "B-042", Now);
        var holeId = Guid.NewGuid();

        blast.AddHole(
            holeId,
            "H-01",
            new Position(1, 2, 3),
            direction: 180,
            inclination: 10,
            now: Now);

        return (blast, holeId);
    }
}
