using BlastManagement.Api.Domain.Blasts;

namespace BlastManagement.Api.Application.Abstractions;

public interface IBlastRepository
{
    ValueTask<Blast?> LoadAsync(
        Guid blastId,
        CancellationToken cancellationToken = default);

    ValueTask SaveAsync(
        Blast blast,
        CancellationToken cancellationToken = default);
}
