using BlastManagement.Api.Domain.Blasts;
using BlastManagement.Api.Domain.Holes;

namespace BlastManagement.Api.Application.Queries.GetBlast;

public sealed record BlastReadModel(
    Guid Id,
    string Name,
    DateTimeOffset? DateBlasted,
    BlastStatus Status,
    long Version,
    IReadOnlyList<HoleReadModel> Holes);

public sealed record HoleReadModel(
    Guid Id,
    Guid BlastId,
    string Name,
    Position Position,
    double Direction,
    double Inclination,
    HoleStatus Status);
