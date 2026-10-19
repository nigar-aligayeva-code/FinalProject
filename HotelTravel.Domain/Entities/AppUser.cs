using Microsoft.AspNetCore.Identity;

namespace HotelTravel.Domain.Entities;

public class AppUser : IdentityUser
{
    public string FirstName { get; set; }
    public string LastName { get; set; }

    // Seçilmiş avatar (məs. "beard"); null -> adın baş hərfi göstərilir
    public string? AvatarKey { get; set; }
}
