using BlastManagement.Api.Domain.Exceptions;

namespace BlastManagement.Api.Domain.Holes;

public readonly record struct Position(double X, double Y, double Z)
{
    internal void EnsureValid()
    {
        if (!double.IsFinite(X) || !double.IsFinite(Y) || !double.IsFinite(Z))
        {
            throw new DomainRuleViolationException(
                "Hole position coordinates must be finite numbers.");
        }
    }
}
