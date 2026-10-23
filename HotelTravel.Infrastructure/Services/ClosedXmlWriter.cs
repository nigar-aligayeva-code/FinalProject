using ClosedXML.Excel;
using HotelTravel.Application.DTOs.Exports;
using HotelTravel.Application.Interfaces;

namespace HotelTravel.Infrastructure.Services;

// Excel (.xlsx): başlıq, formatlı sütunlar, filtr, sabit başlıq sətri və cəm sətri
public class ClosedXmlWriter : IXlsxWriter
{
    public byte[] Write(ExportTable table)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(SafeSheetName(table.SheetName));

        // 1-ci sətir: başlıq
        sheet.Cell(1, 1).Value = $"Tripio — {table.Title}";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 14;
        sheet.Cell(2, 1).Value = $"Exported {DateTime.Now:dd MMM yyyy, HH:mm} · {table.Rows.Count} rows · amounts in AZN";
        sheet.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;

        const int headerRow = 4;
        var firstDataRow = headerRow + 1;

        // Sütun başlıqları
        for (var c = 0; c < table.Columns.Count; c++)
        {
            var cell = sheet.Cell(headerRow, c + 1);
            cell.Value = table.Columns[c].Header;
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F2421");
        }

        // Məlumat
        for (var r = 0; r < table.Rows.Count; r++)
        {
            var row = table.Rows[r];

            for (var c = 0; c < table.Columns.Count; c++)
            {
                var cell = sheet.Cell(firstDataRow + r, c + 1);
                var value = c < row.Length ? row[c] : null;

                switch (value)
                {
                    case null: break;
                    case DateTime d: cell.Value = d; break;
                    case decimal m: cell.Value = m; break;
                    case int i: cell.Value = i; break;
                    case double db: cell.Value = db; break;
                    default: cell.Value = value.ToString(); break;
                }

                cell.Style.NumberFormat.Format = table.Columns[c].Kind switch
                {
                    ExportColumnKind.Money => "#,##0.00",
                    ExportColumnKind.Date => "dd.mm.yyyy",
                    ExportColumnKind.DateTime => "dd.mm.yyyy hh:mm",
                    ExportColumnKind.Number => "0",
                    _ => cell.Style.NumberFormat.Format
                };
            }
        }

        var lastDataRow = firstDataRow + table.Rows.Count - 1;

        // Cəm sətri (Excel formulası ilə, filtrə görə yox — bütün sətirlər)
        if (table.Rows.Count > 0 && table.Columns.Any(c => c.Sum))
        {
            var totalRow = lastDataRow + 1;
            sheet.Cell(totalRow, 1).Value = "Total";

            for (var c = 0; c < table.Columns.Count; c++)
            {
                if (!table.Columns[c].Sum)
                    continue;

                var letter = XLHelper.GetColumnLetterFromNumber(c + 1);
                var cell = sheet.Cell(totalRow, c + 1);
                cell.FormulaA1 = $"SUM({letter}{firstDataRow}:{letter}{lastDataRow})";
                cell.Style.NumberFormat.Format = table.Columns[c].Kind == ExportColumnKind.Money ? "#,##0.00" : "0";
            }

            var totalRange = sheet.Range(totalRow, 1, totalRow, table.Columns.Count);
            totalRange.Style.Font.Bold = true;
            totalRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F3EEE5");
            totalRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
        }

        // Filtr + sabit başlıq + sütun eni
        if (table.Rows.Count > 0)
            sheet.Range(headerRow, 1, lastDataRow, table.Columns.Count).SetAutoFilter();

        sheet.SheetView.FreezeRows(headerRow);
        sheet.Columns(1, table.Columns.Count).AdjustToContents(headerRow, Math.Max(headerRow, lastDataRow + 1));

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return stream.ToArray();
    }

    // Excel vərəq adı: 31 simvol, \ / ? * [ ] : olmadan
    private static string SafeSheetName(string name)
    {
        var clean = new string((string.IsNullOrWhiteSpace(name) ? "Data" : name)
            .Where(ch => !"\\/?*[]:".Contains(ch)).ToArray());

        return clean.Length > 31 ? clean[..31] : clean;
    }
}
