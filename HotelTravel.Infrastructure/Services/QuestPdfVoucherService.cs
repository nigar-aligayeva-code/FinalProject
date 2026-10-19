using System.Globalization;
using HotelTravel.Application.DTOs.Bookings;
using HotelTravel.Application.Interfaces;
using HotelTravel.Application.Services;
using HotelTravel.Domain.Enums;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HotelTravel.Infrastructure.Services;

// Rezervasiya voucher-i: A4 PDF (QuestPDF) + QR kod (QRCoder)
public class QuestPdfVoucherService : IVoucherPdfService
{
    private const string Dark = "#1F2421";
    private const string Green = "#52614E";
    private const string Beige = "#F3EEE5";
    private const string Muted = "#6B7280";
    private const string Line = "#E5E7EB";

    private static readonly CultureInfo En = CultureInfo.InvariantCulture;

    static QuestPdfVoucherService()
    {
        // Pulsuz "Community" lisenziyası (tələbə layihəsi üçün uyğundur)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Create(GetBookingDto b, string bookingUrl)
    {
        var qr = CreateQr(bookingUrl);
        var nights = Math.Max(1, (b.CheckOutDate.Date - b.CheckInDate.Date).Days);

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(t => t.FontSize(10.5f).FontColor(Dark));

                // HEADER
                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Tripio").FontSize(26).Bold();
                        col.Item().Text("Hotel booking voucher").FontColor(Muted);
                    });

                    row.ConstantItem(170).AlignRight().Column(col =>
                    {
                        col.Item().AlignRight().Text("CONFIRMATION CODE").FontSize(8).Bold().FontColor(Green);
                        col.Item().AlignRight().Text(b.ConfirmationCode).FontSize(17).Bold();
                        col.Item().AlignRight().Text(StatusText(b.Status)).FontSize(9).Bold().FontColor(StatusColor(b.Status));
                    });
                });

                // CONTENT
                page.Content().PaddingTop(22).Column(col =>
                {
                    col.Spacing(16);

                    // Otel
                    col.Item().Background(Beige).Padding(18).Column(hotel =>
                    {
                        hotel.Item().Text(b.HotelName ?? "Hotel").FontSize(18).Bold();
                        hotel.Item().PaddingTop(2).Text(JoinParts(b.HotelAddress, b.HotelCity, b.HotelCountry)).FontColor(Muted);

                        if (!string.IsNullOrWhiteSpace(b.HotelPhone))
                            hotel.Item().PaddingTop(2).Text($"Phone: {b.HotelPhone}").FontColor(Muted);
                    });

                    // Tarixlər
                    col.Item().Row(row =>
                    {
                        row.Spacing(12);
                        row.RelativeItem().Element(c => DateBox(c, "CHECK-IN", b.CheckInDate, "from 14:00"));
                        row.RelativeItem().Element(c => DateBox(c, "CHECK-OUT", b.CheckOutDate, "until 12:00"));
                        row.RelativeItem().Element(c => InfoBox(c, "STAY", $"{nights} night{(nights == 1 ? "" : "s")}",
                            BookingEmailTemplates.Guests(b.AdultCount, b.ChildrenCount)));
                    });

                    // Detallar
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2);
                            c.RelativeColumn(3);
                        });

                        void Row(string label, string value, bool bold = false)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Line).PaddingVertical(7).Text(label).FontColor(Muted);

                            var text = table.Cell().BorderBottom(1).BorderColor(Line).PaddingVertical(7).Text(value);

                            if (bold)
                                text.Bold();
                        }

                        Row("Guest", b.GuestName ?? "");
                        Row("Email", b.GuestEmail ?? "");
                        Row("Room", $"{b.RoomName} (No. {b.RoomNumber})");

                        if (b.DiscountAmount > 0)
                        {
                            Row("Original price", BookingEmailTemplates.Money(b.OriginalPrice));
                            Row("Discount", $"-{BookingEmailTemplates.Money(b.DiscountAmount)}" +
                                (string.IsNullOrWhiteSpace(b.DiscountNote) ? "" : $" ({b.DiscountNote})"));
                        }

                        Row("Total paid at hotel", BookingEmailTemplates.Money(b.TotalPrice), bold: true);
                    });

                    // QR
                    col.Item().PaddingTop(6).Row(row =>
                    {
                        row.ConstantItem(120).Image(qr);

                        row.RelativeItem().PaddingLeft(18).AlignMiddle().Column(info =>
                        {
                            info.Spacing(4);
                            info.Item().Text("Scan at check-in").FontSize(13).Bold();
                            info.Item().Text("The QR code opens this booking online. You can also find it with your confirmation code on Tripio.")
                                .FontColor(Muted);
                            info.Item().Text("Free cancellation until the day before check-in.").FontColor(Green).Bold();
                        });
                    });
                });

                // FOOTER
                page.Footer().AlignCenter().Text(t =>
                {
                    t.DefaultTextStyle(s => s.FontSize(8).FontColor(Muted));
                    t.Span($"Generated {DateTime.Now.ToString("dd MMM yyyy, HH:mm", En)} · Tripio Hotel & Travel");
                });
            });
        }).GeneratePdf();
    }

    private static void DateBox(IContainer container, string label, DateTime date, string note)
        => InfoBox(container, label, date.ToString("ddd, dd MMM yyyy", En), note);

    private static void InfoBox(IContainer container, string label, string value, string note)
    {
        container.Border(1).BorderColor(Line).Padding(12).Column(col =>
        {
            col.Item().Text(label).FontSize(8).Bold().FontColor(Green);
            col.Item().PaddingTop(3).Text(value).FontSize(12).Bold();
            col.Item().Text(note).FontSize(9).FontColor(Muted);
        });
    }

    private static byte[] CreateQr(string text)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);

        return new PngByteQRCode(data).GetGraphic(10);
    }

    private static string StatusText(BookingStatus status) => status switch
    {
        BookingStatus.Cancelled => "CANCELLED",
        BookingStatus.CheckedIn => "CHECKED IN",
        BookingStatus.CheckedOut => "COMPLETED",
        _ => "CONFIRMED"
    };

    private static string StatusColor(BookingStatus status)
        => status == BookingStatus.Cancelled ? "#B42318" : Green;

    private static string JoinParts(params string?[] parts)
        => string.Join(", ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
}
