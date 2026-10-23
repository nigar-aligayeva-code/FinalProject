using HotelTravel.Application.DTOs.Exports;

namespace HotelTravel.Application.Interfaces;

public interface IExportService
{
    // type: bookings | flights | taxis | cars;  format: xlsx | csv
    Task<ExportFile?> ExportAsync(string type, string format);
}

// Excel faylını yaradır (Infrastructure: ClosedXML)
public interface IXlsxWriter
{
    byte[] Write(ExportTable table);
}
