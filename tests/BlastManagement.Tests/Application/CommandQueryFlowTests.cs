using BlastManagement.Api.Application.Abstractions;
using BlastManagement.Api.Application.Commands.AddHole;
using BlastManagement.Api.Application.Commands.ChargeHole;
using BlastManagement.Api.Application.Commands.CreateBlast;
using BlastManagement.Api.Application.Queries.GetBlast;
using BlastManagement.Api.Domain.Blasts;
using BlastManagement.Api.Domain.Holes;
using BlastManagement.Api.Infrastructure.EventStore;
using BlastManagement.Api.Infrastructure.Projections;
using BlastManagement.Api.Infrastructure.Repositories;

namespace BlastManagement.Tests.Application;

public sealed class CommandQueryFlowTests
{
    [Fact]
    public async Task CommandsUpdateProjection_AndQueryReadsIndependentView()
    {
        var projection = new BlastReadModelProjection();
        var store = new EventStoreInMemory(new IEventProjection[] { projection });
        var repository = new EventSourcedBlastRepository(store);
        var clock = new StubClock(
            new DateTimeOffset(2026, 9, 9, 8, 0, 0, TimeSpan.Zero));

        var create = new CreateBlastCommandHandler(repository, clock);
        var addHole = new AddHoleCommandHandler(repository, clock);
        var charge = new ChargeHoleCommandHandler(repository, clock);
        var query = new GetBlastQueryHandler(projection);

        var blastId = await create.Handle(new CreateBlastCommand("B-042"));
        var holeId = await addHole.Handle(new AddHoleCommand(
            blastId,
            "H-01",
            new Position(1, 2, 3),
            180,
            10));
        await charge.Handle(new ChargeHoleCommand(blastId, holeId));

        var view = await query.Handle(new GetBlastQuery(blastId));

        Assert.Equal(BlastStatus.Loaded, view.Status);
        Assert.Equal(3, view.Version);
        Assert.Equal(HoleStatus.Charged, Assert.Single(view.Holes).Status);
    }

    private sealed class StubClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }
}
