using BlastManagement.Api.Application.Abstractions;
using BlastManagement.Api.Application.Exceptions;

namespace BlastManagement.Api.Application.Queries.GetBlastHistory;

public sealed class GetBlastHistoryQueryHandler(IEventStoreInMemory eventStore)
{
    public async ValueTask<IReadOnlyList<EventHistoryEntry>> Handle(
        GetBlastHistoryQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var stream = await eventStore.LoadAsync(query.BlastId, cancellationToken);

        if (stream.Count == 0)
        {
            throw new BlastNotFoundException(query.BlastId);
        }

        return stream
            .OrderBy(storedEvent => storedEvent.StreamVersion)
            .Select(storedEvent => new EventHistoryEntry(
                storedEvent.StreamVersion,
                storedEvent.Event.GetType().Name,
                storedEvent.Event.OccurredAt,
                storedEvent.Event))
            .ToArray();
    }
}
