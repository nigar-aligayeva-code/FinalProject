using System.Text;
using HotelTravel.Application.DTOs.Exports;
using HotelTravel.Application.Services;
using Xunit;

namespace HotelTravel.Tests;

public class CsvExportTests
{
    private static string Text(byte[] bytes) => Encoding.UTF8.GetString(bytes).TrimStart('﻿');

    [Fact]
    public void Write_ShouldStartWithBomAndHeader()
    {
        var table = new ExportTable
        {
            Columns = { new("Code"), new("Total", ExportColumnKind.Money) },
            Rows = { new object?[] { "BK-1", 450.5m } }
        };

        var bytes = CsvExport.Write(table);

        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes.Take(3).ToArray());
        Assert.Equal("Code,Total\r\nBK-1,450.50\r\n", Text(bytes));
    }

    [Theory]
    [InlineData("Baku, Azerbaijan", "\"Baku, Azerbaijan\"")]
    [InlineData("He said \"hi\"", "\"He said \"\"hi\"\"\"")]
    [InlineData("plain", "plain")]
    public void Quote_ShouldFollowCsvRules(string value, string expected)
    {
        Assert.Equal(expected, CsvExport.Quote(value));
    }

    [Fact]
    public void Quote_ShouldBlockFormulaInjection()
    {
        Assert.Equal("'=HYPERLINK(\"x\")", CsvExport.Quote("=HYPERLINK(\"x\")").Trim('"').Replace("\"\"", "\""));
        Assert.StartsWith("'", CsvExport.Quote("@SUM(A1)"));
    }

    [Fact]
    public void Quote_ShouldKeepNegativeNumbers()
    {
        Assert.Equal("-50.00", CsvExport.Quote("-50.00"));
    }

    [Fact]
    public void Format_ShouldUseIsoDates()
    {
        var date = new DateTime(2026, 11, 10, 14, 30, 0);

        Assert.Equal("2026-11-10", CsvExport.Format(date, ExportColumnKind.Date));
        Assert.Equal("2026-11-10 14:30", CsvExport.Format(date, ExportColumnKind.DateTime));
    }
}
