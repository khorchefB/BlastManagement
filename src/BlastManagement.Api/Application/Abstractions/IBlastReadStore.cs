using BlastManagement.Api.Application.Queries.GetBlast;

namespace BlastManagement.Api.Application.Abstractions;

public interface IBlastReadStore
{
    BlastReadModel? Get(Guid blastId);
}
