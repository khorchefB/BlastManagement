using BlastManagement.Api.Application.Abstractions;
using BlastManagement.Api.Domain.Blasts;

namespace BlastManagement.Api.Application.Commands.CreateBlast;

public sealed class CreateBlastCommandHandler(
    IBlastRepository repository,
    IClock clock)
{
    public async ValueTask<Guid> Handle(
        CreateBlastCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var blastId = Guid.NewGuid();
        var blast = Blast.Create(blastId, command.Name, clock.UtcNow);

        await repository.SaveAsync(blast, cancellationToken);
        return blastId;
    }
}
