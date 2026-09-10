using BlastManagement.Api.Application.Abstractions;
using BlastManagement.Api.Domain.Blasts;

namespace BlastManagement.Api.Infrastructure.Repositories;

public sealed class EventSourcedBlastRepository(IEventStoreInMemory eventStore)
    : IBlastRepository
{
    public async ValueTask<Blast?> LoadAsync(
        Guid blastId,
        CancellationToken cancellationToken = default)
    {
        var stream = await eventStore.LoadAsync(blastId, cancellationToken);

        return stream.Count == 0
            ? null
            : Blast.Rehydrate(stream.Select(storedEvent => storedEvent.Event));
    }

    public async ValueTask SaveAsync(
        Blast blast,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(blast);

        var changes = blast.GetUncommittedEvents();

        if (changes.Count == 0)
        {
            return;
        }

        await eventStore.AppendAsync(
            blast.Id,
            blast.Version,
            changes,
            cancellationToken);

        blast.MarkChangesAsCommitted();
    }
}
