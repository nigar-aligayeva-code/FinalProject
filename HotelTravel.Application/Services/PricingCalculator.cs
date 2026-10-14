using HotelTravel.Application.DTOs.Bookings;
using HotelTravel.Application.Interfaces;
using HotelTravel.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HotelTravel.Application.Services;

// Qiymət + endirim hesablaması.
// Həm rezervasiya formundakı "preview", həm də rezervasiyanın
// özü (BookingCreationService) bu metoddan istifadə edir ki,
// qonaq gördüyü qiyməti ödəsin.
public static class PricingCalculator
{
    public static async Task<PriceQuoteDto> QuoteAsync(
        IAppDbContext db,
        int roomId,
        DateTime checkInDate,
        DateTime checkOutDate,
        string? email,
        string? appUserId,
        string? promoCode,
        DateTime today)
    {
        var room = await db.Rooms
            .Where(r =>
                r.Id == roomId &&
                !r.IsDeleted &&
                !r.Hotel.IsDeleted)
            .Select(r => new
            {
                r.PricePerNight,
                City = r.Hotel.City
            })
            .FirstOrDefaultAsync();

        if (room is null)
        {
            return new PriceQuoteDto
            {
                Success = false,
                Error = "Room not found."
            };
        }

        var nights = (checkOutDate.Date - checkInDate.Date).Days;

        if (nights <= 0)
        {
            return new PriceQuoteDto
            {
                Success = false,
                Error = "Check-out date must be after check-in date."
            };
        }

        var quote = new PriceQuoteDto
        {
            Success = true,
            Nights = nights,
            PricePerNight = room.PricePerNight,
            BaseTotal = BookingRules.CalculateTotal(
                room.PricePerNight,
                checkInDate,
                checkOutDate)
        };


        // =========================
        // 1. PROMO CODE
        // =========================
        int promoPercent = 0;
        string? promoCodeText = null;

        if (!string.IsNullOrWhiteSpace(promoCode))
        {
            var code = promoCode.Trim().ToUpper();

            var promo = await db.PromoCodes
                .FirstOrDefaultAsync(p => p.Code == code);

            if (promo is null)
            {
                quote.PromoError = "Promo code not found.";
            }
            else
            {
                var promoError = BookingRules.ValidatePromo(
                    promo.IsActive,
                    promo.IsDeleted,
                    promo.ValidFrom,
                    promo.ValidUntil,
                    promo.MaxUses,
                    promo.UsedCount,
                    today);

                if (promoError != null)
                {
                    quote.PromoError = promoError;
                }
                else
                {
                    promoPercent = promo.DiscountPercent;
                    promoCodeText = promo.Code;
                }
            }
        }


        // =========================
        // 2. EXPLORER REWARD
        // Başqa şəhərdəki otelimizdə qalışı tamamlayıbsa
        // =========================
        var normalizedEmail = email?.Trim().ToLower();

        if (!string.IsNullOrWhiteSpace(normalizedEmail) ||
            !string.IsNullOrWhiteSpace(appUserId))
        {
            var previousCity = await db.Bookings
                .Where(b =>
                    !b.IsDeleted &&
                    b.Status == BookingStatus.CheckedOut &&
                    b.Room.Hotel.City != room.City &&
                    ((normalizedEmail != null && b.Guest.Email.ToLower() == normalizedEmail) ||
                     (appUserId != null && b.AppUserId == appUserId)))
                .OrderByDescending(b => b.CheckOutDate)
                .Select(b => b.Room.Hotel.City)
                .FirstOrDefaultAsync();

            if (previousCity != null)
            {
                quote.LoyaltyEligible = true;
                quote.LoyaltyCity = previousCity;
            }
        }

        var loyaltyPercent = quote.LoyaltyEligible
            ? BookingRules.ExplorerRewardPercent
            : 0;


        // =========================
        // 3. ƏN YAXŞI ENDİRİM (toplanmır)
        // =========================
        if (promoPercent == 0 && loyaltyPercent == 0)
        {
            quote.Total = quote.BaseTotal;
            return quote;
        }

        if (promoPercent >= loyaltyPercent)
        {
            quote.DiscountPercent = promoPercent;
            quote.AppliedPromoCode = promoCodeText;
            quote.DiscountNote = $"Promo code {promoCodeText} (-{promoPercent}%)";
        }
        else
        {
            quote.DiscountPercent = loyaltyPercent;
            quote.DiscountNote =
                $"Explorer reward: welcome back from {quote.LoyaltyCity} (-{loyaltyPercent}%)";
        }

        var (discount, total) = BookingRules.ApplyDiscount(
            quote.BaseTotal,
            quote.DiscountPercent);

        quote.DiscountAmount = discount;
        quote.Total = total;

        return quote;
    }
}
