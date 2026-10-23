namespace HotelTravel.Application.DTOs.Exports;

public enum ExportColumnKind
{
    Text,
    Number,
    Money,
    Date,
    DateTime
}

public record ExportColumn(string Header, ExportColumnKind Kind = ExportColumnKind.Text, bool Sum = false);

// Excel / CSV üçün cədvəl (formatdan asılı deyil)
public class ExportTable
{
    public string Title { get; set; } = string.Empty;
    public string SheetName { get; set; } = "Data";

    public List<ExportColumn> Columns { get; set; } = new();
    public List<object?[]> Rows { get; set; } = new();
}

public class ExportFile
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
}
