using BlastManagement.Api.Application.Abstractions;
using BlastManagement.Api.Application.Exceptions;

namespace BlastManagement.Api.Application.Queries.GetBlast;

public sealed class GetBlastQueryHandler(IBlastReadStore readStore)
{
    public ValueTask<BlastReadModel> Handle(
        GetBlastQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();

        var blast = readStore.Get(query.BlastId)
            ?? throw new BlastNotFoundException(query.BlastId);

        return ValueTask.FromResult(blast);
    }
}
