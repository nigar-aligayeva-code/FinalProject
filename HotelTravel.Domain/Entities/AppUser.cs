using Microsoft.AspNetCore.Identity;

namespace HotelTravel.Domain.Entities;

public class AppUser : IdentityUser
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}