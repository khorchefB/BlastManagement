namespace BlastManagement.Api.Application.Exceptions;

public sealed class BlastNotFoundException(Guid blastId)
    : Exception($"Blast '{blastId}' was not found.")
{
    public Guid BlastId { get; } = blastId;
}
