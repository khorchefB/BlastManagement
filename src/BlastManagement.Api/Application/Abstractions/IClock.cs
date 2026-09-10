namespace BlastManagement.Api.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
