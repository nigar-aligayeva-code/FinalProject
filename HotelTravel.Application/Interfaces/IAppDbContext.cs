using HotelTravel.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelTravel.Application.Interfaces;

public interface IAppDbContext
{
    DbSet<HotelChain> HotelChains { get; set; }

    DbSet<Hotel> Hotels { get; set; }

    DbSet<Room> Rooms { get; set; }

    DbSet<Guest> Guests { get; set; }

    DbSet<Booking> Bookings { get; set; }

    DbSet<Amenity> Amenities { get; set; }

    DbSet<RoomImage> RoomImages { get; set; }

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}