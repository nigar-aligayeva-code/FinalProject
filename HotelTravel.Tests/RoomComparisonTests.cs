using HotelTravel.Application.DTOs.Rooms;
using HotelTravel.Application.Services;
using Xunit;

namespace HotelTravel.Tests;

public class RoomComparisonTests
{
    private static GetRoomDto Room(int id, decimal price, int capacity,
        double rating = 0, int reviews = 0, params string[] amenities) => new()
    {
        Id = id,
        Name = $"Room {id}",
        PricePerNight = price,
        Capacity = capacity,
        AverageRating = rating,
        ReviewCount = reviews,
        AmenityNames = amenities.ToList()
    };

    [Theory]
    [InlineData("3,7,12", new[] { 3, 7, 12 })]
    [InlineData("3, 3 ,abc,-1,0,7", new[] { 3, 7 })]
    [InlineData("1,2,3,4,5", new[] { 1, 2, 3 })]
    [InlineData("", new int[0])]
    [InlineData(null, new int[0])]
    public void ParseIds_ShouldKeepValidDistinctIds_UpToThree(string? input, int[] expected)
    {
        Assert.Equal(expected, RoomComparisonRules.ParseIds(input));
    }

    [Fact]
    public void Build_ShouldPickWinnerForEachCriterion()
    {
        var result = RoomComparisonRules.Build(new[]
        {
            Room(1, 120, 2, 4.5, 10, "Wi-Fi"),
            Room(2, 90, 3, 3.8, 4, "Wi-Fi", "Pool", "Spa"),
            Room(3, 200, 4, 4.9, 2, "Wi-Fi", "Pool")
        });

        Assert.Equal(2, result.CheapestRoomId);
        Assert.Equal(3, result.LargestRoomId);
        Assert.Equal(3, result.BestRatedRoomId);
        Assert.Equal(2, result.MostAmenitiesRoomId);
    }

    [Fact]
    public void Build_ShouldNotPickWinner_WhenValuesAreTied()
    {
        var result = RoomComparisonRules.Build(new[]
        {
            Room(1, 100, 2),
            Room(2, 100, 2)
        });

        Assert.Null(result.CheapestRoomId);
        Assert.Null(result.LargestRoomId);
        Assert.Null(result.MostAmenitiesRoomId);
    }

    [Fact]
    public void Build_ShouldIgnoreRoomsWithoutReviews_ForBestRating()
    {
        var result = RoomComparisonRules.Build(new[]
        {
            Room(1, 100, 2, rating: 0, reviews: 0),
            Room(2, 150, 2, rating: 3.1, reviews: 1)
        });

        Assert.Equal(2, result.BestRatedRoomId);
    }

    [Fact]
    public void Build_ShouldMergeAmenities_WithoutDuplicates()
    {
        var result = RoomComparisonRules.Build(new[]
        {
            Room(1, 100, 2, 0, 0, "Wi-Fi", "Pool"),
            Room(2, 120, 2, 0, 0, "wi-fi", "Air conditioning")
        });

        Assert.Equal(new[] { "Air conditioning", "Pool", "Wi-Fi" }, result.AllAmenities);
        Assert.True(result.HasAmenity(result.Rooms[1], "Wi-Fi"));
        Assert.False(result.HasAmenity(result.Rooms[1], "Pool"));
    }

    [Fact]
    public void Build_WithOneRoom_ShouldNotAssignBadges()
    {
        var result = RoomComparisonRules.Build(new[] { Room(1, 100, 2, 5, 3) });

        Assert.Null(result.CheapestRoomId);
        Assert.Null(result.BestRatedRoomId);
    }
}
