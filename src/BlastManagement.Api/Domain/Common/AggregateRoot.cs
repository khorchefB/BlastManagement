namespace BlastManagement.Api.Domain.Common;

public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _uncommittedEvents = new();

    /// <summary>
    /// Number of persisted events that were replayed for this aggregate.
    /// An empty stream is therefore version 0.
    /// </summary>
    public long Version { get; private set; }

    public IReadOnlyList<IDomainEvent> GetUncommittedEvents() =>
        _uncommittedEvents.ToArray();

    protected void Raise(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        Apply(domainEvent);
        _uncommittedEvents.Add(domainEvent);
    }

    internal void LoadFromHistory(IEnumerable<IDomainEvent> history)
    {
        ArgumentNullException.ThrowIfNull(history);

        if (Version != 0 || _uncommittedEvents.Count != 0)
        {
            throw new InvalidOperationException("An aggregate can only be rehydrated once.");
        }

        foreach (var domainEvent in history)
        {
            Apply(domainEvent);
            Version++;
        }
    }

    public void MarkChangesAsCommitted()
    {
        Version += _uncommittedEvents.Count;
        _uncommittedEvents.Clear();
    }

    protected abstract void Apply(IDomainEvent domainEvent);
}
