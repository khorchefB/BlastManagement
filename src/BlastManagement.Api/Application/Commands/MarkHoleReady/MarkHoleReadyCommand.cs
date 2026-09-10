namespace BlastManagement.Api.Application.Commands.MarkHoleReady;

public sealed record MarkHoleReadyCommand(Guid BlastId, Guid HoleId);
