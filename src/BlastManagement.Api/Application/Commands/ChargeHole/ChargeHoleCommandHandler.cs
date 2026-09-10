using BlastManagement.Api.Application.Abstractions;
using BlastManagement.Api.Application.Exceptions;

namespace BlastManagement.Api.Application.Commands.ChargeHole;

public sealed class ChargeHoleCommandHandler(
    IBlastRepository repository,
    IClock clock)
{
    public async ValueTask Handle(
        ChargeHoleCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var blast = await repository.LoadAsync(command.BlastId, cancellationToken)
            ?? throw new BlastNotFoundException(command.BlastId);

        blast.ChargeHole(command.HoleId, clock.UtcNow);
        await repository.SaveAsync(blast, cancellationToken);
    }
}
