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
