namespace HotelTravel.Application.DTOs.Common;

// Status dəyişmək üçün sorğu (məs. Booking -> Cancelled)
public class StatusUpdateDto<TStatus>
    where TStatus : struct, Enum
{
    public TStatus Status { get; set; }
}
