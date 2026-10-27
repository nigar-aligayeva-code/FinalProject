using Microsoft.AspNetCore.Identity;

namespace HotelTravel.Domain.Entities;

public class AppUser : IdentityUser
{
    public string FirstName { get; set; }
    public string LastName { get; set; }

    // Seçilmiş avatar (məs. "beard"); null -> adın baş hərfi göstərilir
    public string? AvatarKey { get; set; }

    // Otel partnyoru (HotelPartner rolu): yalnız bu otelin sorğularını və otaqlarını idarə edir
    public int? PartnerHotelId { get; set; }
}
