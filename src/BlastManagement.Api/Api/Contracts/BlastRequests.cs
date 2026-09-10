using BlastManagement.Api.Domain.Holes;

namespace BlastManagement.Api.Api.Contracts;

public sealed record CreateBlastRequest(string Name);

public sealed record AddHoleRequest(
    string Name,
    PositionRequest? Position,
    double Direction,
    double Inclination);

public sealed record PositionRequest(double X, double Y, double Z)
{
    public Position ToDomain() => new(X, Y, Z);
}
