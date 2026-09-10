using BlastManagement.Api.Domain.Holes;

namespace BlastManagement.Api.Application.Commands.AddHole;

public sealed record AddHoleCommand(
    Guid BlastId,
    string Name,
    Position Position,
    double Direction,
    double Inclination);
