using HotelTravel.Application.Services;
using Xunit;

namespace HotelTravel.Tests;

// "Şifrəni unutdum" formu: bot qoruması və email maskası
public class BotGuardTests
{
    private static readonly DateTime Now = new(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Human_FillsFormInSeconds_AndLeavesHiddenFieldEmpty()
        => Assert.True(BotGuardRules.LooksHuman("", Now.AddSeconds(-12), Now));

    [Fact]
    public void Bot_FillsHiddenField()
        => Assert.False(BotGuardRules.LooksHuman("http://spam.example", Now.AddSeconds(-12), Now));

    [Fact]
    public void Bot_SubmitsTooFast_OrWithoutTimestamp()
    {
        Assert.False(BotGuardRules.LooksHuman("", Now.AddMilliseconds(-500), Now));
        Assert.False(BotGuardRules.LooksHuman("", null, Now));
    }

    [Fact]
    public void OldForm_ShouldBeRefreshed()
        => Assert.False(BotGuardRules.LooksHuman("", Now.AddHours(-3), Now));

    [Theory]
    [InlineData("nigaraligayeva@gmail.com", "ni***@gmail.com")]
    [InlineData("a@b.az", "a***@b.az")]
    [InlineData("  ab@mail.ru ", "a***@mail.ru")]
    [InlineData("not-an-email", "not-an-email")]
    public void MaskEmail_ShowsStartAndDomain(string input, string expected)
        => Assert.Equal(expected, BotGuardRules.MaskEmail(input));
}
