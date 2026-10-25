using HotelTravel.Application.Services;
using Xunit;

namespace HotelTravel.Tests;

public class EmailAddressRulesTests
{
    [Theory]
    [InlineData("nigar@gmail.com")]
    [InlineData("n.aliyeva@mail.az")]
    [InlineData("user+tag@company.co.uk")]
    public void CheckOffline_ShouldAccept_RealLookingEmails(string email)
    {
        var result = EmailAddressRules.CheckOffline(email);

        Assert.True(result.IsValid);
        Assert.Null(result.Suggestion);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("a@b")]
    [InlineData("name@domain.")]
    [InlineData("two@@signs.com")]
    public void CheckOffline_ShouldReject_BadFormat(string email)
    {
        Assert.False(EmailAddressRules.CheckOffline(email).IsValid);
    }

    [Theory]
    [InlineData("bot@mailinator.com")]
    [InlineData("x@10minutemail.com")]
    [InlineData("y@YOPMAIL.com")]
    [InlineData("z@temp-mail.org")]
    public void CheckOffline_ShouldReject_DisposableDomains(string email)
    {
        var result = EmailAddressRules.CheckOffline(email);

        Assert.False(result.IsValid);
        Assert.Contains("disposable", result.Reason);
    }

    [Theory]
    [InlineData("nigar@gmial.com", "nigar@gmail.com")]
    [InlineData("nigar@gmail.co", "nigar@gmail.com")]
    [InlineData("nigar@hotmial.com", "nigar@hotmail.com")]
    [InlineData("nigar@yaho.com", "nigar@yahoo.com")]
    public void SuggestCorrection_ShouldFixTypos(string email, string expected)
    {
        Assert.Equal(expected, EmailAddressRules.SuggestCorrection(email));
    }

    [Theory]
    [InlineData("nigar@gmail.com")]
    [InlineData("nigar@university.edu.az")]
    public void SuggestCorrection_ShouldNotTouchCorrectOrUnrelatedDomains(string email)
    {
        Assert.Null(EmailAddressRules.SuggestCorrection(email));
    }

    [Fact]
    public void ConfirmationEmail_ShouldContainLink_AndLogVersionShouldHideIt()
    {
        var link = AccountEmailTemplates.ConfirmLink("http://localhost:5233", "a@b.com", "TOKEN");
        var (_, html) = AccountEmailTemplates.EmailConfirmation("Nigar", link);
        var (_, logHtml) = AccountEmailTemplates.EmailConfirmationForLog("Nigar");

        Assert.Equal("http://localhost:5233/Account/ConfirmEmail?email=a%40b.com&token=TOKEN", link);
        Assert.Contains("ConfirmEmail", html);
        Assert.DoesNotContain("ConfirmEmail", logHtml);
        Assert.Contains("hidden for security", logHtml);
    }
}
