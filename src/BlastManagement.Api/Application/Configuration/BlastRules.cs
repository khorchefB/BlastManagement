namespace BlastManagement.Api.Application.Configuration;

/// <summary>
/// Core exercise behaviour accepts Charged or Ready holes.
/// Set RequireReadyToFire to true to enable the stricter bonus rule.
/// </summary>
public sealed record BlastRules(bool RequireReadyToFire);
