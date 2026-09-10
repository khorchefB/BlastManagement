using BlastManagement.Api.Application.Abstractions;
using BlastManagement.Api.Application.Exceptions;

namespace BlastManagement.Api.Application.Commands.MarkHoleReady;

public sealed class MarkHoleReadyCommandHandler(
    IBlastRepository repository,
    IClock clock)
{
    public async ValueTask Handle(
        MarkHoleReadyCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var blast = await repository.LoadAsync(command.BlastId, cancellationToken)
            ?? throw new BlastNotFoundException(command.BlastId);

        blast.MarkHoleReady(command.HoleId, clock.UtcNow);
        await repository.SaveAsync(blast, cancellationToken);
    }
}
