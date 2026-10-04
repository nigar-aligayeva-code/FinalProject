using HotelTravel.Application.Interfaces;
using HotelTravel.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HotelTravel.Infrastructure.Persistence;

public class AppDbContext
    : IdentityDbContext<AppUser>, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<HotelChain> HotelChains { get; set; }

    public DbSet<Hotel> Hotels { get; set; }

    public DbSet<Room> Rooms { get; set; }

    public DbSet<Guest> Guests { get; set; }

    public DbSet<Booking> Bookings { get; set; }

    public DbSet<Amenity> Amenities { get; set; }

    public DbSet<RoomImage> RoomImages { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);


        // HOTEL CHAIN -> HOTELS
        modelBuilder.Entity<HotelChain>()
            .HasMany(hc => hc.Hotels)
            .WithOne(h => h.HotelChain)
            .HasForeignKey(h => h.HotelChainId)
            .OnDelete(DeleteBehavior.SetNull);


        // HOTEL -> ROOMS
        modelBuilder.Entity<Hotel>()
            .HasMany(h => h.Rooms)
            .WithOne(r => r.Hotel)
            .HasForeignKey(r => r.HotelId)
            .OnDelete(DeleteBehavior.Cascade);


        // ROOM -> BOOKINGS
        modelBuilder.Entity<Room>()
            .HasMany(r => r.Bookings)
            .WithOne(b => b.Room)
            .HasForeignKey(b => b.RoomId)
            .OnDelete(DeleteBehavior.Restrict);


        // GUEST -> BOOKINGS
        modelBuilder.Entity<Guest>()
            .HasMany(g => g.Bookings)
            .WithOne(b => b.Guest)
            .HasForeignKey(b => b.GuestId)
            .OnDelete(DeleteBehavior.Restrict);


        // ROOM <-> AMENITIES
        modelBuilder.Entity<Room>()
            .HasMany(r => r.Amenities)
            .WithMany(a => a.Rooms)
            .UsingEntity(j =>
                j.ToTable("RoomAmenities"));


        // ROOM -> ROOM IMAGES
        modelBuilder.Entity<Room>()
            .HasMany(r => r.Images)
            .WithOne(i => i.Room)
            .HasForeignKey(i => i.RoomId)
            .OnDelete(DeleteBehavior.Cascade);


        // DECIMAL PRECISION
        modelBuilder.Entity<Room>()
            .Property(r => r.PricePerNight)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Booking>()
            .Property(b => b.TotalPrice)
            .HasPrecision(18, 2);


        // UNIQUE CONFIRMATION CODE
        modelBuilder.Entity<Booking>()
            .HasIndex(b => b.ConfirmationCode)
            .IsUnique();
    }
}