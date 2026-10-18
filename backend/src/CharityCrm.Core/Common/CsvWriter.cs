using System.Text;

namespace CharityCrm.Core.Common;

/// <summary>
/// Excel-friendly CSV: UTF-8 with BOM and ';' as separator (FR-MEM-07, FR-REP-03).
/// Cells that Excel would interpret as formulas are prefixed with an apostrophe.
/// </summary>
public sealed class CsvWriter
{
    private const char Separator = ';';
    private readonly StringBuilder _sb = new();

    public CsvWriter Row(params IEnumerable<string?> cells)
    {
        _sb.AppendJoin(Separator, cells.Select(Escape));
        _sb.Append("\r\n");
        return this;
    }

    public byte[] ToBytes() => [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(_sb.ToString())];

    public static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }
        if (value[0] is '=' or '+' or '-' or '@' or '\t' or '\r')
        {
            value = "'" + value;
        }
        return value.IndexOfAny([Separator, '"', '\n', '\r']) >= 0
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;
    }
}
