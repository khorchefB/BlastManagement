namespace BlastManagement.Api.Domain.Exceptions;

public sealed class HoleNotFoundException(Guid blastId, Guid holeId)
    : Exception($"Hole '{holeId}' was not found in blast '{blastId}'.")
{
    public Guid BlastId { get; } = blastId;

    public Guid HoleId { get; } = holeId;
}
