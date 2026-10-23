using System.Globalization;
using System.Text;
using HotelTravel.Application.DTOs.Exports;

namespace HotelTravel.Application.Services;

// CSV (RFC 4180): vergül, lazım olanda dırnaq; Excel Unicode-u tanısın deyə UTF-8 BOM
public static class CsvExport
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static byte[] Write(ExportTable table)
    {
        var sb = new StringBuilder();

        sb.Append(string.Join(",", table.Columns.Select(c => Quote(c.Header)))).Append("\r\n");

        foreach (var row in table.Rows)
        {
            var cells = table.Columns.Select((column, i) => Quote(Format(i < row.Length ? row[i] : null, column.Kind)));
            sb.Append(string.Join(",", cells)).Append("\r\n");
        }

        var preamble = Encoding.UTF8.GetPreamble();
        var body = Encoding.UTF8.GetBytes(sb.ToString());

        return preamble.Concat(body).ToArray();
    }

    public static string Format(object? value, ExportColumnKind kind) => value switch
    {
        null => string.Empty,
        DateTime d when kind == ExportColumnKind.Date => d.ToString("yyyy-MM-dd", Inv),
        DateTime d => d.ToString("yyyy-MM-dd HH:mm", Inv),
        decimal m => m.ToString("0.00", Inv),
        IFormattable f => f.ToString(null, Inv),
        _ => value.ToString() ?? string.Empty
    };

    // Vergül, dırnaq və ya sətir sonu varsa dırnağa al; dırnaqları ikiləşdir.
    // =, +, -, @ ilə başlayan mətn Excel-də formula kimi işləməsin deyə qabağına ' qoyulur (CSV injection).
    public static string Quote(string value)
    {
        if (value.Length > 0 && "=+-@".Contains(value[0]) && !decimal.TryParse(value, NumberStyles.Any, Inv, out _))
            value = "'" + value;

        return value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;
    }
}
