using HotelTravel.Application.DTOs.Cars;
using HotelTravel.Application.Interfaces;
using HotelTravel.Domain.Entities;
using HotelTravel.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HotelTravel.Application.Services;

public class CarRentalService : ICarRentalService
{
    private readonly IAppDbContext _context;

    public CarRentalService(IAppDbContext context)
    {
        _context = context;
    }


    // =========================
    // CITIES
    // =========================
    public async Task<List<string>> GetCitiesAsync()
    {
        return await _context.RentalCars
            .Where(c => !c.IsDeleted && c.IsActive)
            .Select(c => c.City)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }


    // =========================
    // SEARCH
    // =========================
    public async Task<List<CarDto>> SearchAsync(
        string city,
        DateTime? pickupDate,
        DateTime? returnDate)
    {
        if (string.IsNullOrWhiteSpace(city))
            return new List<CarDto>();

        var name = city.Trim().ToLower();

        var cars = await ProjectCars(
                _context.RentalCars.Where(c =>
                    !c.IsDeleted &&
                    c.IsActive &&
                    c.City.ToLower() == name))
            .OrderBy(c => c.PricePerDay)
            .ToListAsync();

        await FillAvailabilityAsync(cars, pickupDate, returnDate);

        return cars;
    }

    public async Task<CarDto?> GetCarAsync(
        int id,
        DateTime? pickupDate,
        DateTime? returnDate)
    {
        var car = await ProjectCars(
                _context.RentalCars.Where(c =>
                    c.Id == id &&
                    !c.IsDeleted &&
                    c.IsActive))
            .FirstOrDefaultAsync();

        if (car is null)
            return null;

        await FillAvailabilityAsync(new List<CarDto> { car }, pickupDate, returnDate);

        return car;
    }


    // =========================
    // BOOK
    // =========================
    public async Task<(bool Success, string Message, string? ConfirmationCode)>
        BookAsync(CreateCarRentalDto dto, string? appUserId)
    {
        var dateError = CarRentalRules.ValidateDates(
            dto.PickupDate,
            dto.ReturnDate,
            DateTime.Today);

        if (dateError != null)
            return (false, dateError, null);

        var car = await _context.RentalCars
            .FirstOrDefaultAsync(c =>
                c.Id == dto.CarId &&
                !c.IsDeleted &&
                c.IsActive);

        if (car is null)
            return (false, "This car is no longer available.", null);

        var pickup = dto.PickupDate.Date;
        var dropoff = dto.ReturnDate.Date;

        // Bu modelin bütün maşınları həmin günlərdə icarədədirsə - olmaz
        var overlapping = await _context.CarRentals
            .Where(r =>
                r.RentalCarId == car.Id &&
                !r.IsDeleted &&
                r.Status != CarRentalStatus.Cancelled &&
                pickup < r.ReturnDate &&
                dropoff > r.PickupDate)
            .Select(r => new { r.PickupDate, r.ReturnDate })
            .ToListAsync();

        var inUse = CarRentalRules.PeakInUse(overlapping.Select(r => (r.PickupDate, r.ReturnDate)), pickup, dropoff);

        if (CarRentalRules.FreeCount(car.Quantity, inUse) == 0)
            return (false, "This car is already booked for the selected dates.", null);

        var days = CarRentalRules.CountDays(pickup, dropoff);

        var code = $"CR-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

        await _context.CarRentals.AddAsync(new CarRental
        {
            ConfirmationCode = code,
            RentalCarId = car.Id,
            PickupDate = pickup,
            ReturnDate = dropoff,
            Days = days,
            PricePerDay = car.PricePerDay,
            TotalPrice = CarRentalRules.CalculateTotal(car.PricePerDay, days),
            Status = CarRentalStatus.Confirmed,
            DriverName = dto.DriverName.Trim(),
            Email = dto.Email.Trim(),
            Phone = dto.Phone.Trim(),
            AppUserId = appUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        await _context.SaveChangesAsync();

        return (true, "Car rental confirmed.", code);
    }


    // =========================
    // LOOKUP
    // =========================
    public async Task<CarRentalDto?> GetByCodeAsync(string confirmationCode)
    {
        if (string.IsNullOrWhiteSpace(confirmationCode))
            return null;

        var code = confirmationCode.Trim().ToUpper();

        var rental = await ProjectRentals(
                _context.CarRentals.Where(r =>
                    !r.IsDeleted &&
                    r.ConfirmationCode == code))
            .FirstOrDefaultAsync();

        return FillCanCancel(rental);
    }

    public async Task<List<CarRentalDto>> GetByUserAsync(string appUserId)
    {
        if (string.IsNullOrWhiteSpace(appUserId))
            return new List<CarRentalDto>();

        var rentals = await ProjectRentals(
                _context.CarRentals.Where(r =>
                    !r.IsDeleted &&
                    r.AppUserId == appUserId))
            .OrderByDescending(r => r.PickupDate)
            .ToListAsync();

        rentals.ForEach(r => FillCanCancel(r));

        return rentals;
    }

    public async Task<(bool Success, string Message)> CancelByUserAsync(
        int rentalId,
        string appUserId)
    {
        var rental = await _context.CarRentals
            .FirstOrDefaultAsync(r =>
                r.Id == rentalId &&
                !r.IsDeleted &&
                r.AppUserId == appUserId);

        if (rental is null)
            return (false, "Car rental was not found.");

        if (!CarRentalRules.CanCancel(rental.Status, rental.PickupDate, DateTime.Today))
            return (false, "This car rental can no longer be cancelled.");

        rental.Status = CarRentalStatus.Cancelled;
        rental.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return (true, "Car rental cancelled.");
    }


    // =========================
    // ADMIN
    // =========================
    public async Task<List<CarDto>> GetFleetAsync(string? city)
    {
        var query = _context.RentalCars.Where(c => !c.IsDeleted);

        if (!string.IsNullOrWhiteSpace(city))
        {
            var name = city.Trim().ToLower();
            query = query.Where(c => c.City.ToLower() == name);
        }

        return await ProjectCars(query)
            .OrderBy(c => c.City)
            .ThenBy(c => c.PricePerDay)
            .ToListAsync();
    }

    public async Task<SaveCarDto?> GetCarForEditAsync(int id)
    {
        return await _context.RentalCars
            .Where(c => c.Id == id && !c.IsDeleted)
            .Select(c => new SaveCarDto
            {
                Id = c.Id,
                City = c.City,
                Model = c.Model,
                Category = c.Category,
                Seats = c.Seats,
                IsAutomatic = c.IsAutomatic,
                HasAirConditioning = c.HasAirConditioning,
                PricePerDay = c.PricePerDay,
                ImageUrl = c.ImageUrl,
                IsActive = c.IsActive,
                Quantity = c.Quantity
            })
            .FirstOrDefaultAsync();
    }

    public async Task SaveCarAsync(SaveCarDto dto)
    {
        RentalCar? car = null;

        if (dto.Id.HasValue)
        {
            car = await _context.RentalCars
                .FirstOrDefaultAsync(c => c.Id == dto.Id.Value && !c.IsDeleted);
        }

        if (car is null)
        {
            car = new RentalCar
            {
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            await _context.RentalCars.AddAsync(car);
        }

        car.City = dto.City.Trim();
        car.Model = dto.Model.Trim();
        car.Category = dto.Category;
        car.Seats = dto.Seats;
        car.IsAutomatic = dto.IsAutomatic;
        car.HasAirConditioning = dto.HasAirConditioning;
        car.PricePerDay = dto.PricePerDay;
        car.ImageUrl = string.IsNullOrWhiteSpace(dto.ImageUrl) ? null : dto.ImageUrl.Trim();
        car.IsActive = dto.IsActive;
        car.Quantity = Math.Clamp(dto.Quantity, 1, 50);
        car.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task<bool> ToggleCarAsync(int id)
    {
        var car = await _context.RentalCars
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

        if (car is null)
            return false;

        car.IsActive = !car.IsActive;
        car.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<List<CarRentalDto>> GetRentalsAsync()
    {
        var rentals = await ProjectRentals(
                _context.CarRentals.Where(r => !r.IsDeleted))
            .OrderByDescending(r => r.PickupDate)
            .Take(200)
            .ToListAsync();

        rentals.ForEach(r => FillCanCancel(r));

        return rentals;
    }

    public async Task<bool> SetRentalStatusAsync(int id, CarRentalStatus status)
    {
        var rental = await _context.CarRentals
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);

        if (rental is null || rental.Status != CarRentalStatus.Confirmed)
            return false;

        // Yalnız Confirmed -> Completed / Cancelled
        if (status == CarRentalStatus.Confirmed)
            return false;

        rental.Status = status;
        rental.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }


    // =========================
    // HELPERS
    // =========================
    private async Task FillAvailabilityAsync(
        List<CarDto> cars,
        DateTime? pickupDate,
        DateTime? returnDate)
    {
        if (!pickupDate.HasValue ||
            !returnDate.HasValue ||
            returnDate.Value.Date <= pickupDate.Value.Date ||
            cars.Count == 0)
        {
            return;
        }

        var pickup = pickupDate.Value.Date;
        var dropoff = returnDate.Value.Date;
        var days = CarRentalRules.CountDays(pickup, dropoff);

        var carIds = cars.Select(c => c.Id).ToList();

        var rentals = await _context.CarRentals
            .Where(r =>
                carIds.Contains(r.RentalCarId) &&
                !r.IsDeleted &&
                r.Status != CarRentalStatus.Cancelled &&
                pickup < r.ReturnDate &&
                dropoff > r.PickupDate)
            .Select(r => new { r.RentalCarId, r.PickupDate, r.ReturnDate })
            .ToListAsync();

        foreach (var car in cars)
        {
            var inUse = CarRentalRules.PeakInUse(
                rentals.Where(r => r.RentalCarId == car.Id).Select(r => (r.PickupDate, r.ReturnDate)),
                pickup,
                dropoff);

            car.Days = days;
            car.TotalPrice = CarRentalRules.CalculateTotal(car.PricePerDay, days);
            car.AvailableCount = CarRentalRules.FreeCount(car.Quantity, inUse);
            car.IsAvailable = car.AvailableCount > 0;
        }
    }

    private static CarRentalDto? FillCanCancel(CarRentalDto? rental)
    {
        if (rental != null)
        {
            rental.CanCancel = CarRentalRules.CanCancel(
                rental.Status,
                rental.PickupDate,
                DateTime.Today);
        }

        return rental;
    }

    private static IQueryable<CarDto> ProjectCars(IQueryable<RentalCar> query)
    {
        return query.Select(c => new CarDto
        {
            Id = c.Id,
            City = c.City,
            Model = c.Model,
            Category = c.Category,
            Seats = c.Seats,
            IsAutomatic = c.IsAutomatic,
            HasAirConditioning = c.HasAirConditioning,
            PricePerDay = c.PricePerDay,
            ImageUrl = c.ImageUrl,
            IsActive = c.IsActive,
            Quantity = c.Quantity,
            AvailableCount = c.Quantity
        });
    }

    private static IQueryable<CarRentalDto> ProjectRentals(IQueryable<CarRental> query)
    {
        return query.Select(r => new CarRentalDto
        {
            Id = r.Id,
            ConfirmationCode = r.ConfirmationCode,
            CarId = r.RentalCarId,
            CarModel = r.RentalCar.Model,
            CarCategory = r.RentalCar.Category,
            City = r.RentalCar.City,
            CarImage = r.RentalCar.ImageUrl,
            PickupDate = r.PickupDate,
            ReturnDate = r.ReturnDate,
            Days = r.Days,
            PricePerDay = r.PricePerDay,
            TotalPrice = r.TotalPrice,
            Status = r.Status,
            DriverName = r.DriverName,
            Email = r.Email,
            Phone = r.Phone
        });
    }
}
