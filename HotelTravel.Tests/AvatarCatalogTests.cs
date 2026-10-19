using HotelTravel.Application.Services;
using Xunit;

namespace HotelTravel.Tests;

public class AvatarCatalogTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("beard")]
    [InlineData("headscarf")]
    public void IsValid_ShouldAccept_KnownKeysAndEmpty(string? key)
    {
        Assert.True(AvatarCatalog.IsValid(key));
    }

    [Theory]
    [InlineData("dragon")]
    [InlineData("../../web.config")]
    public void IsValid_ShouldReject_UnknownKeys(string key)
    {
        Assert.False(AvatarCatalog.IsValid(key));
    }

    [Fact]
    public void GetUrl_ShouldReturnNull_ForInitials()
    {
        Assert.Null(AvatarCatalog.GetUrl(null));
        Assert.Null(AvatarCatalog.GetUrl("unknown"));
    }

    [Fact]
    public void GetUrl_ShouldPointToSvgFile()
    {
        Assert.Equal("/assets/images/avatars/cap.svg", AvatarCatalog.GetUrl("cap"));
    }

    [Theory]
    [InlineData("Nigar", "Aliyeva", null, "NA")]
    [InlineData("nigar", "", null, "N")]
    [InlineData("", "", "test@mail.com", "T")]
    [InlineData(null, null, null, "?")]
    public void GetInitials_ShouldUseNameThenEmail(string? first, string? last, string? email, string expected)
    {
        Assert.Equal(expected, AvatarCatalog.GetInitials(first, last, email));
    }

    [Fact]
    public void Catalog_ShouldHaveUniqueKeys()
    {
        Assert.Equal(AvatarCatalog.All.Count, AvatarCatalog.All.Select(a => a.Key).Distinct().Count());
    }
}
