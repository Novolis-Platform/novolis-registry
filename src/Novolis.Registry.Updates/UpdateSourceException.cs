namespace Novolis.Registry.Updates;

/// <summary>Safe provider error carrying a stable diagnostic code.</summary>
public sealed class UpdateSourceException : IOException
{
    /// <summary>Creates a provider failure.</summary>
    public UpdateSourceException(
        string code,
        string message,
        Exception? innerException = null,
        DateTimeOffset? retryAfter = null)
        : base(message, innerException)
    {
        Code = string.IsNullOrWhiteSpace(code) ? "source-error" : code;
        RetryAfter = retryAfter;
    }

    /// <summary>Stable code suitable for logs and support diagnostics.</summary>
    public string Code { get; }

    /// <summary>Optional time after which a retry is reasonable.</summary>
    public DateTimeOffset? RetryAfter { get; }
}
