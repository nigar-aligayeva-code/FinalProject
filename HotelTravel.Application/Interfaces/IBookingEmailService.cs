namespace HotelTravel.Application.Interfaces;

public interface IBookingEmailService
{
    // Status Pending-dirsə "sorğu qəbul olundu", əks halda təsdiq məktubu + PDF voucher
    Task<(bool Success, string Message)> SendConfirmationAsync(string confirmationCode);

    // byHotel: otel / admin ləğv edib (qonaqdan cərimə tutulmur)
    Task<(bool Success, string Message)> SendCancellationAsync(string confirmationCode, bool byHotel = false);

    // Otel sorğunu rədd etdi (boş yer yoxdur)
    Task<(bool Success, string Message)> SendRejectionAsync(string confirmationCode);

    // Otel 24 saatda cavab vermədi - sorğu avtomatik bağlandı
    Task<(bool Success, string Message)> SendExpiredAsync(string confirmationCode);

    // Saytdakı rezervasiya linki (QR kod və məktub üçün)
    string GetBookingUrl(string confirmationCode);
}
