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

public class AvatarToneAndUploadTests
{
    [Theory]
    [InlineData("beard-t1")]
    [InlineData("afro-t5")]
    [InlineData("turban")]
    [InlineData("/uploads/avatars/0123456789abcdef0123456789abcdef.jpg")]
    [InlineData("/uploads/avatars/0123456789abcdef0123456789abcdef.webp")]
    public void IsValid_ShouldAccept_TonesAndUploads(string key)
    {
        Assert.True(AvatarCatalog.IsValid(key));
    }

    [Theory]
    [InlineData("beard-t6")]
    [InlineData("dragon-t2")]
    [InlineData("/uploads/avatars/../../appsettings.json")]
    [InlineData("/uploads/avatars/abc.jpg")]
    [InlineData("/uploads/avatars/0123456789abcdef0123456789abcdef.exe")]
    [InlineData("https://evil.com/a.jpg")]
    public void IsValid_ShouldReject_BadTonesAndPaths(string key)
    {
        Assert.False(AvatarCatalog.IsValid(key));
    }

    [Fact]
    public void GetUrl_ShouldHandleTonesAndUploads()
    {
        Assert.Equal("/assets/images/avatars/beard-t4.svg", AvatarCatalog.GetUrl("beard-t4"));

        var upload = "/uploads/avatars/0123456789abcdef0123456789abcdef.png";
        Assert.Equal(upload, AvatarCatalog.GetUrl(upload));
    }

    [Fact]
    public void ToneHelpers_ShouldSplitKey()
    {
        Assert.Equal("beard-t3", AvatarCatalog.WithTone("beard", 3));
        Assert.Equal("beard", AvatarCatalog.WithTone("beard", 0));
        Assert.Equal(3, AvatarCatalog.ToneOf("beard-t3"));
        Assert.Equal(0, AvatarCatalog.ToneOf("beard"));
        Assert.Equal("beard", AvatarCatalog.StyleOf("beard-t3"));
        Assert.Null(AvatarCatalog.StyleOf("/uploads/avatars/0123456789abcdef0123456789abcdef.png"));
    }

    [Fact]
    public void Catalog_ShouldIncludeDiverseStyles_AndFiveTones()
    {
        Assert.Contains(AvatarCatalog.All, a => a.Key == "afro");
        Assert.Contains(AvatarCatalog.All, a => a.Key == "turban");
        Assert.Equal(5, AvatarCatalog.Tones.Count);
    }
}
