namespace BlastManagement.Api.Application.Commands.ChargeHole;

public sealed record ChargeHoleCommand(Guid BlastId, Guid HoleId);
