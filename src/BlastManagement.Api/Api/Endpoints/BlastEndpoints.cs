using BlastManagement.Api.Api.Contracts;
using BlastManagement.Api.Application.Commands.AddHole;
using BlastManagement.Api.Application.Commands.ChargeHole;
using BlastManagement.Api.Application.Commands.CreateBlast;
using BlastManagement.Api.Application.Commands.FireBlast;
using BlastManagement.Api.Application.Commands.MarkHoleReady;
using BlastManagement.Api.Application.Queries.GetBlast;
using BlastManagement.Api.Application.Queries.GetBlastHistory;
using BlastManagement.Api.Domain.Exceptions;

namespace BlastManagement.Api.Api.Endpoints;

public static class BlastEndpoints
{
    public static IEndpointRouteBuilder MapBlastEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/blasts")
            .WithTags("Blasts");

        group.MapPost("", CreateBlast)
            .WithName("CreateBlast");

        group.MapPost("/{blastId:guid}/holes", AddHole)
            .WithName("AddHole");

        group.MapPut("/{blastId:guid}/holes/{holeId:guid}/charge", ChargeHole)
            .WithName("ChargeHole");

        // Bonus endpoint. Enable BlastRules:RequireReadyToFire to make it
        // mandatory before firing every hole.
        group.MapPut("/{blastId:guid}/holes/{holeId:guid}/ready", MarkHoleReady)
            .WithName("MarkHoleReady");


        group.MapPost("/{blastId:guid}/fire", FireBlast)
            .WithName("FireBlast");

        group.MapGet("/{blastId:guid}", GetBlast)
            .WithName("GetBlast");

        group.MapGet("/{blastId:guid}/history", GetBlastHistory)
            .WithName("GetBlastHistory");

        return endpoints;
    }

    private static async Task<IResult> CreateBlast(
        CreateBlastRequest request,
        CreateBlastCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var blastId = await handler.Handle(
            new CreateBlastCommand(request.Name),
            cancellationToken);

        return Results.Created(
            $"/blasts/{blastId}",
            new CreatedBlastResponse(blastId));
    }

    private static async Task<IResult> AddHole(
        Guid blastId,
        AddHoleRequest request,
        AddHoleCommandHandler handler,
        CancellationToken cancellationToken)
    {
        if (request.Position is null)
        {
            throw new DomainRuleViolationException("Hole position is required.");
        }

        var holeId = await handler.Handle(
            new AddHoleCommand(
                blastId,
                request.Name,
                request.Position.ToDomain(),
                request.Direction,
                request.Inclination),
            cancellationToken);

        return Results.Created(
            $"/blasts/{blastId}",
            new AddedHoleResponse(holeId, blastId));
    }

    private static async Task<IResult> ChargeHole(
        Guid blastId,
        Guid holeId,
        ChargeHoleCommandHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.Handle(
            new ChargeHoleCommand(blastId, holeId),
            cancellationToken);

        return Results.NoContent();
    }

    private static async Task<IResult> MarkHoleReady(
        Guid blastId,
        Guid holeId,
        MarkHoleReadyCommandHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.Handle(
            new MarkHoleReadyCommand(blastId, holeId),
            cancellationToken);

        return Results.NoContent();
    }

    private static async Task<IResult> FireBlast(
        Guid blastId,
        FireBlastCommandHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.Handle(
            new FireBlastCommand(blastId),
            cancellationToken);

        return Results.NoContent();
    }

    private static async Task<IResult> GetBlast(
        Guid blastId,
        GetBlastQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var blast = await handler.Handle(
            new GetBlastQuery(blastId),
            cancellationToken);

        return Results.Ok(blast);
    }

    private static async Task<IResult> GetBlastHistory(
        Guid blastId,
        GetBlastHistoryQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var history = await handler.Handle(
            new GetBlastHistoryQuery(blastId),
            cancellationToken);

        var response = history
            .Select(entry => new EventHistoryResponse(
                entry.StreamVersion,
                entry.EventType,
                entry.OccurredAt,
                entry.Event))
            .ToArray();

        return Results.Ok(response);
    }
}
