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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Identity cədvəllərinin konfiqurasiyasını yaradır
        base.OnModelCreating(modelBuilder);

        // HotelChain -> Hotels
        modelBuilder.Entity<HotelChain>()
            .HasMany(hc => hc.Hotels)
            .WithOne(h => h.HotelChain)
            .HasForeignKey(h => h.HotelChainId)
            .OnDelete(DeleteBehavior.SetNull);

        // Hotel -> Rooms
        modelBuilder.Entity<Hotel>()
            .HasMany(h => h.Rooms)
            .WithOne(r => r.Hotel)
            .HasForeignKey(r => r.HotelId)
            .OnDelete(DeleteBehavior.Cascade);

        // Room -> Bookings
        modelBuilder.Entity<Room>()
            .HasMany(r => r.Bookings)
            .WithOne(b => b.Room)
            .HasForeignKey(b => b.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        // Guest -> Bookings
        modelBuilder.Entity<Guest>()
            .HasMany(g => g.Bookings)
            .WithOne(b => b.Guest)
            .HasForeignKey(b => b.GuestId)
            .OnDelete(DeleteBehavior.Restrict);

        // Room <-> Amenity (Many-to-Many)
        modelBuilder.Entity<Room>()
            .HasMany(r => r.Amenities)
            .WithMany(a => a.Rooms)
            .UsingEntity(j => j.ToTable("RoomAmenities"));

        // Decimal precision
        modelBuilder.Entity<Room>()
            .Property(r => r.PricePerNight)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Booking>()
            .Property(b => b.TotalPrice)
            .HasPrecision(18, 2);

        // ConfirmationCode unique olmalıdır
        modelBuilder.Entity<Booking>()
            .HasIndex(b => b.ConfirmationCode)
            .IsUnique();
    }
}