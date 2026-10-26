namespace HotelTravel.Application.Services;

public enum ReminderKind
{
    None,
    HotelUpcoming,   // girişə 2–3 gün qalıb
    HotelTomorrow,   // giriş sabahdır
    FlightDayBefore, // uçuşa 24 saatdan az qalıb
    FlightSoon       // uçuşa 3 saatdan az qalıb
}

/// <summary>Hansı xatırlatmanın vaxtıdır? (təmiz qaydalar, test olunur)</summary>
public static class ReminderRules
{
    public const int HotelUpcomingDays = 3;
    public const int FlightDayBeforeHours = 24;
    public const int FlightSoonHours = 3;

    public static ReminderKind ForHotel(DateTime checkInDate, DateTime now)
    {
        var days = (checkInDate.Date - now.Date).Days;

        return days switch
        {
            1 => ReminderKind.HotelTomorrow,
            >= 2 and <= HotelUpcomingDays => ReminderKind.HotelUpcoming,
            _ => ReminderKind.None
        };
    }

    public static ReminderKind ForFlight(DateTime departure, DateTime now)
    {
        var hours = (departure - now).TotalHours;

        if (hours <= 0)
            return ReminderKind.None;

        if (hours <= FlightSoonHours)
            return ReminderKind.FlightSoon;

        return hours <= FlightDayBeforeHours
            ? ReminderKind.FlightDayBefore
            : ReminderKind.None;
    }

    /// <summary>EmailLog-dakı tip: eyni xatırlatma ikinci dəfə göndərilmir.</summary>
    public static string EmailType(ReminderKind kind) => kind switch
    {
        ReminderKind.HotelUpcoming => BookingEmailTemplates.HotelReminderType,
        ReminderKind.HotelTomorrow => BookingEmailTemplates.HotelReminderTomorrowType,
        ReminderKind.FlightDayBefore => BookingEmailTemplates.FlightReminderType,
        ReminderKind.FlightSoon => BookingEmailTemplates.FlightReminderSoonType,
        _ => string.Empty
    };

    /// <summary>Brauzerdəki canlı bildirişin növü (live-activity.js şablonları).</summary>
    public static string NotificationKind(ReminderKind kind) => kind switch
    {
        ReminderKind.HotelUpcoming => "hotel_reminder",
        ReminderKind.HotelTomorrow => "hotel_tomorrow",
        ReminderKind.FlightDayBefore => "flight_reminder",
        ReminderKind.FlightSoon => "flight_soon",
        _ => string.Empty
    };
}
