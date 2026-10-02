namespace Novolis.Registry.Updates;

/// <summary>Clock seam used to make polling and snooze behavior deterministic.</summary>
public interface IUpdateClock
{
    /// <summary>Current UTC time.</summary>
    DateTimeOffset UtcNow { get; }
}
