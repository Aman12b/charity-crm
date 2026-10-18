namespace CharityCrm.Core.Common;

/// <summary>Builds LIKE patterns from user input so that '%' and '_' are matched literally.</summary>
public static class SearchPattern
{
    public const string EscapeChar = "\\";

    public static string Contains(string term) => "%" + Escape(term.Trim()) + "%";

    public static string Escape(string term) =>
        term.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
