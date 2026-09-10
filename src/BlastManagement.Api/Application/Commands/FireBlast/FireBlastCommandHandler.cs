using BlastManagement.Api.Application.Abstractions;
using BlastManagement.Api.Application.Configuration;
using BlastManagement.Api.Application.Exceptions;

namespace BlastManagement.Api.Application.Commands.FireBlast;

public sealed class FireBlastCommandHandler(
    IBlastRepository repository,
    IClock clock,
    BlastRules rules)
{
    public async ValueTask Handle(
        FireBlastCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var blast = await repository.LoadAsync(command.BlastId, cancellationToken)
            ?? throw new BlastNotFoundException(command.BlastId);

        blast.Fire(clock.UtcNow, rules.RequireReadyToFire);
        await repository.SaveAsync(blast, cancellationToken);
    }
}
