namespace HotelTravel.Application.DTOs.Profile;

// null və ya boş -> avatar silinir, baş hərflər göstərilir
public class UpdateAvatarDto
{
    public string? AvatarKey { get; set; }
}
