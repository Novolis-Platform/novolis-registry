using System.Globalization;

namespace Novolis.Registry.Primitives.Updates;

/// <summary>Compares Novolis CalVer versions with optional prerelease labels.</summary>
public static class UpdateVersionComparer
{
    /// <summary>Compares two supported versions.</summary>
    public static int Compare(string? left, string? right)
    {
        var leftVersion = Parse(left);
        var rightVersion = Parse(right);

        for (var index = 0; index < Math.Max(leftVersion.Numbers.Count, rightVersion.Numbers.Count); index++)
        {
            var leftNumber = index < leftVersion.Numbers.Count ? leftVersion.Numbers[index] : 0;
            var rightNumber = index < rightVersion.Numbers.Count ? rightVersion.Numbers[index] : 0;
            var numberComparison = leftNumber.CompareTo(rightNumber);
            if (numberComparison != 0)
                return numberComparison;
        }

        if (leftVersion.PreRelease is null && rightVersion.PreRelease is null)
            return 0;
        if (leftVersion.PreRelease is null)
            return 1;
        if (rightVersion.PreRelease is null)
            return -1;

        return CompareIdentifiers(leftVersion.PreRelease, rightVersion.PreRelease);
    }

    /// <summary>Returns whether a version is a supported CalVer or preview version.</summary>
    public static bool IsValid(string? value)
    {
        try
        {
            _ = Parse(value);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    /// <summary>Parses a supported version and throws for malformed input.</summary>
    public static ParsedUpdateVersion Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new FormatException("An update version is required.");

        var normalized = value.Trim();
        if (normalized.StartsWith('v'))
            normalized = normalized[1..];

        var buildSeparator = normalized.IndexOf('+');
        if (buildSeparator >= 0)
            normalized = normalized[..buildSeparator];

        var preReleaseSeparator = normalized.IndexOf('-');
        var numberPart = preReleaseSeparator >= 0
            ? normalized[..preReleaseSeparator]
            : normalized;
        var preRelease = preReleaseSeparator >= 0
            ? normalized[(preReleaseSeparator + 1)..]
            : null;

        var numberParts = numberPart.Split('.', StringSplitOptions.None);
        if (numberParts.Length is < 3 or > 4
            || numberParts.Any(part => part.Length == 0
                || !part.All(char.IsAsciiDigit)
                || (part.Length > 1 && part[0] == '0')))
        {
            throw new FormatException($"'{value}' is not a supported CalVer.");
        }

        var numbers = numberParts
            .Select(part => int.Parse(part, CultureInfo.InvariantCulture))
            .ToArray();
        if (numbers[0] < 2000 || numbers[1] is < 1 or > 12 || numbers[2] is < 0 or > 99)
            throw new FormatException($"'{value}' is not a supported CalVer.");

        if (preRelease is not null
            && (preRelease.Length == 0
                || preRelease.Split('.').Any(part => part.Length == 0
                    || !part.All(character => char.IsAsciiLetterOrDigit(character) || character == '-'))))
        {
            throw new FormatException($"'{value}' has an invalid prerelease label.");
        }

        return new ParsedUpdateVersion(numbers, preRelease);
    }

    private static int CompareIdentifiers(string left, string right)
    {
        var leftParts = left.Split('.');
        var rightParts = right.Split('.');
        for (var index = 0; index < Math.Max(leftParts.Length, rightParts.Length); index++)
        {
            if (index >= leftParts.Length)
                return -1;
            if (index >= rightParts.Length)
                return 1;

            var leftPart = leftParts[index];
            var rightPart = rightParts[index];
            var leftNumeric = int.TryParse(leftPart, NumberStyles.None, CultureInfo.InvariantCulture, out var leftNumber);
            var rightNumeric = int.TryParse(rightPart, NumberStyles.None, CultureInfo.InvariantCulture, out var rightNumber);

            if (leftNumeric && rightNumeric)
            {
                var comparison = leftNumber.CompareTo(rightNumber);
                if (comparison != 0)
                    return comparison;
            }
            else if (leftNumeric != rightNumeric)
            {
                return leftNumeric ? -1 : 1;
            }
            else
            {
                var comparison = StringComparer.Ordinal.Compare(leftPart, rightPart);
                if (comparison != 0)
                    return comparison;
            }
        }

        return 0;
    }
}

/// <summary>Parsed components of a supported update version.</summary>
public sealed record ParsedUpdateVersion(
    IReadOnlyList<int> Numbers,
    string? PreRelease);
