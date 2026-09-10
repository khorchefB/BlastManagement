using BlastManagement.Api.Application.Abstractions;

namespace BlastManagement.Api.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
