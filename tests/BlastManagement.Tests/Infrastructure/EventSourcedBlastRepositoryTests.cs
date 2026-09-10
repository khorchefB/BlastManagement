using BlastManagement.Api.Application.Abstractions;
using BlastManagement.Api.Domain.Blasts;
using BlastManagement.Api.Domain.Holes;
using BlastManagement.Api.Infrastructure.EventStore;
using BlastManagement.Api.Infrastructure.Repositories;

namespace BlastManagement.Tests.Infrastructure;

public sealed class EventSourcedBlastRepositoryTests
{
    [Fact]
    public async Task Load_RehydratesStateOnlyFromTheEventStream()
    {
        var eventStore = new EventStoreInMemory(Array.Empty<IEventProjection>());
        var repository = new EventSourcedBlastRepository(eventStore);
        var now = DateTimeOffset.UtcNow;
        var blastId = Guid.NewGuid();
        var holeId = Guid.NewGuid();

        var original = Blast.Create(blastId, "B-042", now);
        original.AddHole(
            holeId,
            "H-01",
            new Position(10, 20, -3),
            90,
            15,
            now.AddMinutes(1));
        original.ChargeHole(holeId, now.AddMinutes(2));

        await repository.SaveAsync(original);
        var rehydrated = await repository.LoadAsync(blastId);

        Assert.NotNull(rehydrated);
        Assert.Equal(3, rehydrated.Version);
        Assert.Empty(rehydrated.GetUncommittedEvents());
        Assert.Equal(BlastStatus.Loaded, rehydrated.Status);
        Assert.Equal(HoleStatus.Charged, Assert.Single(rehydrated.Holes).Status);
    }
}
