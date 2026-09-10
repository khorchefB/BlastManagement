using BlastManagement.Api.Application.Abstractions;
using BlastManagement.Api.Application.Exceptions;

namespace BlastManagement.Api.Application.Commands.AddHole;

public sealed class AddHoleCommandHandler(
    IBlastRepository repository,
    IClock clock)
{
    public async ValueTask<Guid> Handle(
        AddHoleCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var blast = await repository.LoadAsync(command.BlastId, cancellationToken)
            ?? throw new BlastNotFoundException(command.BlastId);

        var holeId = Guid.NewGuid();
        blast.AddHole(
            holeId,
            command.Name,
            command.Position,
            command.Direction,
            command.Inclination,
            clock.UtcNow);

        await repository.SaveAsync(blast, cancellationToken);
        return holeId;
    }
}
