namespace Novolis.Registry.Updates;

/// <summary>Safe provider error carrying a stable diagnostic code.</summary>
public sealed class UpdateSourceException : IOException
{
    /// <summary>Creates a provider failure.</summary>
    public UpdateSourceException(string code, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Code = string.IsNullOrWhiteSpace(code) ? "source-error" : code;
    }

    /// <summary>Stable code suitable for logs and support diagnostics.</summary>
    public string Code { get; }
}
