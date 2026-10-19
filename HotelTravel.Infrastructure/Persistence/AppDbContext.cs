using HotelTravel.Application.Interfaces;
using HotelTravel.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
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
    public DbSet<HotelImage> HotelImages { get; set; }

    public DbSet<Review> Reviews { get; set; }

    public DbSet<WishlistItem> WishlistItems { get; set; }

    public DbSet<PromoCode> PromoCodes { get; set; }

    public DbSet<CityPlace> CityPlaces { get; set; }

    public DbSet<RentalCar> RentalCars { get; set; }

    public DbSet<CarRental> CarRentals { get; set; }

    public DbSet<TaxiBooking> TaxiBookings { get; set; }

    public DbSet<FlightRoute> FlightRoutes { get; set; }

    public DbSet<FlightBooking> FlightBookings { get; set; }

    public DbSet<EmailLog> EmailLogs { get; set; }

    public async Task<IDbContextTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        return await Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // EMAIL LOG
        modelBuilder.Entity<EmailLog>(entity =>
        {
            entity.Property(e => e.To).HasMaxLength(256);
            entity.Property(e => e.Subject).HasMaxLength(300);
            entity.Property(e => e.Type).HasMaxLength(50);
            entity.Property(e => e.ReferenceCode).HasMaxLength(50);
            entity.Property(e => e.Error).HasMaxLength(500);
            entity.HasIndex(e => e.CreatedAt);
        });


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


        // APP USER -> BOOKINGS (My Bookings)
        // İstifadəçi silinsə, rezervasiya qalır, sadəcə əlaqə kəsilir
        modelBuilder.Entity<Booking>()
            .HasOne(b => b.AppUser)
            .WithMany()
            .HasForeignKey(b => b.AppUserId)
            .OnDelete(DeleteBehavior.SetNull);


        // BOOKING -> REVIEW (1:1)
        // BookingId üzərində unique index avtomatik yaranır,
        // yəni bir rezervasiyaya yalnız bir rəy yazıla bilər.
        modelBuilder.Entity<Booking>()
            .HasOne(b => b.Review)
            .WithOne(r => r.Booking)
            .HasForeignKey<Review>(r => r.BookingId)
            .OnDelete(DeleteBehavior.Restrict);


        // ROOM -> REVIEWS
        modelBuilder.Entity<Room>()
            .HasMany(r => r.Reviews)
            .WithOne(rv => rv.Room)
            .HasForeignKey(rv => rv.RoomId)
            .OnDelete(DeleteBehavior.Restrict);


        // CAR RENTAL
        modelBuilder.Entity<RentalCar>(entity =>
        {
            entity.HasIndex(c => c.City);
            entity.Property(c => c.City).HasMaxLength(100).IsRequired();
            entity.Property(c => c.Model).HasMaxLength(100).IsRequired();
            entity.Property(c => c.ImageUrl).HasMaxLength(400);
            entity.Property(c => c.PricePerDay).HasPrecision(18, 2);
        });

        modelBuilder.Entity<CarRental>(entity =>
        {
            entity.HasIndex(r => r.ConfirmationCode).IsUnique();
            entity.Property(r => r.ConfirmationCode).HasMaxLength(20).IsRequired();
            entity.Property(r => r.DriverName).HasMaxLength(100).IsRequired();
            entity.Property(r => r.Email).HasMaxLength(150).IsRequired();
            entity.Property(r => r.Phone).HasMaxLength(30).IsRequired();
            entity.Property(r => r.PricePerDay).HasPrecision(18, 2);
            entity.Property(r => r.TotalPrice).HasPrecision(18, 2);

            entity.HasOne(r => r.RentalCar)
                .WithMany(c => c.Rentals)
                .HasForeignKey(r => r.RentalCarId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.AppUser)
                .WithMany()
                .HasForeignKey(r => r.AppUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });


        // FLIGHTS
        modelBuilder.Entity<FlightRoute>(entity =>
        {
            entity.HasIndex(r => new { r.FromCity, r.ToCity });

            entity.Property(r => r.FlightNumber).HasMaxLength(10).IsRequired();
            entity.Property(r => r.Airline).HasMaxLength(60).IsRequired();
            entity.Property(r => r.FromCity).HasMaxLength(100).IsRequired();
            entity.Property(r => r.FromCode).HasMaxLength(10).IsRequired();
            entity.Property(r => r.ToCity).HasMaxLength(100).IsRequired();
            entity.Property(r => r.ToCode).HasMaxLength(10).IsRequired();
            entity.Property(r => r.EconomyPrice).HasPrecision(18, 2);
            entity.Property(r => r.BusinessPrice).HasPrecision(18, 2);
        });

        modelBuilder.Entity<FlightBooking>(entity =>
        {
            entity.HasIndex(b => b.ConfirmationCode).IsUnique();
            entity.HasIndex(b => new { b.FlightRouteId, b.FlightDate });

            entity.Property(b => b.ConfirmationCode).HasMaxLength(20).IsRequired();
            entity.Property(b => b.FlightNumber).HasMaxLength(10).IsRequired();
            entity.Property(b => b.Airline).HasMaxLength(60).IsRequired();
            entity.Property(b => b.FromCity).HasMaxLength(100).IsRequired();
            entity.Property(b => b.FromCode).HasMaxLength(10).IsRequired();
            entity.Property(b => b.ToCity).HasMaxLength(100).IsRequired();
            entity.Property(b => b.ToCode).HasMaxLength(10).IsRequired();
            entity.Property(b => b.PassengerNames).HasMaxLength(800).IsRequired();
            entity.Property(b => b.ContactName).HasMaxLength(100).IsRequired();
            entity.Property(b => b.Email).HasMaxLength(150).IsRequired();
            entity.Property(b => b.Phone).HasMaxLength(30).IsRequired();
            entity.Property(b => b.FarePerPassenger).HasPrecision(18, 2);
            entity.Property(b => b.TotalPrice).HasPrecision(18, 2);

            entity.HasOne(b => b.FlightRoute)
                .WithMany(r => r.Bookings)
                .HasForeignKey(b => b.FlightRouteId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(b => b.AppUser)
                .WithMany()
                .HasForeignKey(b => b.AppUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });


        // AIRPORT TAXIS
        modelBuilder.Entity<TaxiBooking>(entity =>
        {
            entity.HasIndex(t => t.ConfirmationCode).IsUnique();
            entity.HasIndex(t => t.PickupTime);

            entity.Property(t => t.ConfirmationCode).HasMaxLength(20).IsRequired();
            entity.Property(t => t.City).HasMaxLength(100).IsRequired();
            entity.Property(t => t.AirportCode).HasMaxLength(10).IsRequired();
            entity.Property(t => t.AirportName).HasMaxLength(150).IsRequired();
            entity.Property(t => t.FlightNumber).HasMaxLength(10);
            entity.Property(t => t.PassengerName).HasMaxLength(100).IsRequired();
            entity.Property(t => t.Email).HasMaxLength(150).IsRequired();
            entity.Property(t => t.Phone).HasMaxLength(30).IsRequired();
            entity.Property(t => t.Notes).HasMaxLength(200);
            entity.Property(t => t.Price).HasPrecision(18, 2);

            entity.HasOne(t => t.Hotel)
                .WithMany()
                .HasForeignKey(t => t.HotelId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.AppUser)
                .WithMany()
                .HasForeignKey(t => t.AppUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });


        // CITY GUIDE
        modelBuilder.Entity<CityPlace>(entity =>
        {
            entity.HasIndex(p => p.City);

            entity.Property(p => p.City).HasMaxLength(100).IsRequired();
            entity.Property(p => p.Name).HasMaxLength(150).IsRequired();
            entity.Property(p => p.Description).HasMaxLength(600).IsRequired();
            entity.Property(p => p.Address).HasMaxLength(200);
            entity.Property(p => p.ImageUrl).HasMaxLength(400);
            entity.Property(p => p.Duration).HasMaxLength(50);
            entity.Property(p => p.PriceFrom).HasPrecision(18, 2);
        });


        // DISCOUNTS
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.Property(b => b.OriginalPrice).HasPrecision(18, 2);
            entity.Property(b => b.DiscountAmount).HasPrecision(18, 2);
            entity.Property(b => b.DiscountNote).HasMaxLength(200);
            entity.Property(b => b.PromoCode).HasMaxLength(30);
        });

        modelBuilder.Entity<PromoCode>(entity =>
        {
            entity.HasIndex(p => p.Code).IsUnique();

            entity.Property(p => p.Code)
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(p => p.Description).HasMaxLength(200);

            entity.ToTable(t =>
                t.HasCheckConstraint(
                    "CK_PromoCodes_DiscountPercent",
                    "[DiscountPercent] BETWEEN 1 AND 90"));
        });


        // WISHLIST (Saved rooms)
        modelBuilder.Entity<WishlistItem>(entity =>
        {
            // Eyni otaq bir istifadəçidə yalnız bir dəfə
            entity.HasIndex(w => new { w.AppUserId, w.RoomId })
                .IsUnique();

            entity.HasOne(w => w.AppUser)
                .WithMany()
                .HasForeignKey(w => w.AppUserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(w => w.Room)
                .WithMany()
                .HasForeignKey(w => w.RoomId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // REVIEW VALIDATION
        modelBuilder.Entity<Review>(entity =>
        {
            entity.Property(r => r.Comment)
                .HasMaxLength(1000)
                .IsRequired();

            entity.ToTable(t =>
                t.HasCheckConstraint(
                    "CK_Reviews_Rating",
                    "[Rating] BETWEEN 1 AND 5"));
        });
    }
}
