namespace HotelTravel.Domain.Entities;

public class Guest : BaseEntity
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}