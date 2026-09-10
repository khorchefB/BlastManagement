using BlastManagement.Api.Domain.Common;

namespace BlastManagement.Api.Application.Abstractions;

public interface IEventStoreInMemory
{
    ValueTask<IReadOnlyList<StoredEvent>> LoadAsync(
        Guid streamId,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<StoredEvent>> AppendAsync(
        Guid streamId,
        long expectedVersion,
        IReadOnlyCollection<IDomainEvent> events,
        CancellationToken cancellationToken = default);
}
