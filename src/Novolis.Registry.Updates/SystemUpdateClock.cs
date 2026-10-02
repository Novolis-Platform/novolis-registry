namespace Novolis.Registry.Updates;

/// <summary>System clock implementation for production hosts.</summary>
public sealed class SystemUpdateClock : IUpdateClock
{
    /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
