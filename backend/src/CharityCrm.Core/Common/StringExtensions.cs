namespace CharityCrm.Core.Common;

public static class StringExtensions
{
    public static string ToCamelCase(this string value) =>
        string.IsNullOrEmpty(value) ? value : char.ToLowerInvariant(value[0]) + value[1..];

    /// <summary>Trims and converts empty strings to null (for optional text fields).</summary>
    public static string? TrimToNull(this string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
