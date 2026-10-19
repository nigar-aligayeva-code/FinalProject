namespace HotelTravel.Application.Interfaces;

public interface IBookingEmailService
{
    // Təsdiq məktubu + PDF voucher
    Task<(bool Success, string Message)> SendConfirmationAsync(string confirmationCode);

    Task<(bool Success, string Message)> SendCancellationAsync(string confirmationCode);

    // Saytdakı rezervasiya linki (QR kod və məktub üçün)
    string GetBookingUrl(string confirmationCode);
}
