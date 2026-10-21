using HotelTravel.Application.DTOs.Email;
using HotelTravel.Application.Interfaces;
using HotelTravel.Application.Services;
using HotelTravel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelTravel.Tests;

public class AccountEmailTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    // Test üçün: göndərməni yadda saxlayır və ya xəta atır
    private sealed class FakeSender : IEmailSender
    {
        public bool Fail { get; init; }
        public List<EmailMessage> Sent { get; } = new();

        public Task SendAsync(EmailMessage message)
        {
            if (Fail)
                throw new InvalidOperationException("SMTP is down");

            Sent.Add(message);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public void ResetLink_ShouldPointToResetPage()
    {
        var link = AccountEmailTemplates.ResetLink("http://localhost:5233/", "a+b@mail.com", "TOKEN-123");

        Assert.Equal("http://localhost:5233/Account/ResetPassword?email=a%2Bb%40mail.com&token=TOKEN-123", link);
    }

    [Fact]
    public void PasswordReset_ShouldEncodeNameAndContainLink()
    {
        var (subject, html) = AccountEmailTemplates.PasswordReset("<b>Nigar</b>", "http://x/reset?a=1&b=2");

        Assert.Equal("Reset your Tripio password", subject);
        Assert.Contains("&lt;b&gt;Nigar&lt;/b&gt;", html);
        Assert.Contains("http://x/reset?a=1&amp;b=2", html);
    }

    [Fact]
    public async Task Outbox_ShouldSendAndLog()
    {
        await using var context = CreateContext();
        var sender = new FakeSender();
        var outbox = new EmailOutbox(sender, context);

        var result = await outbox.SendAsync(
            new EmailMessage { To = "guest@mail.com", Subject = "Hi", HtmlBody = "<p>Hi</p>" },
            AccountEmailTemplates.PasswordResetType,
            null);

        Assert.True(result.Success);
        Assert.Single(sender.Sent);

        var log = await context.EmailLogs.SingleAsync();
        Assert.True(log.Delivered);
        Assert.Equal("PasswordReset", log.Type);
    }

    [Fact]
    public async Task Outbox_ShouldLogFailureWithoutThrowing()
    {
        await using var context = CreateContext();
        var outbox = new EmailOutbox(new FakeSender { Fail = true }, context);

        var result = await outbox.SendAsync(
            new EmailMessage { To = "guest@mail.com", Subject = "Hi", HtmlBody = "x" },
            "Test",
            "BK-1");

        Assert.False(result.Success);

        var log = await context.EmailLogs.SingleAsync();
        Assert.False(log.Delivered);
        Assert.Equal("SMTP is down", log.Error);
    }

    [Fact]
    public void PasswordResetForLog_ShouldNotContainTheLink()
    {
        var (_, html) = AccountEmailTemplates.PasswordResetForLog("Nigar");

        Assert.DoesNotContain("href=", html);
        Assert.Contains("link hidden for security", html);
    }

    [Fact]
    public async Task Outbox_ShouldLogSafeBodyButSendRealOne()
    {
        await using var context = CreateContext();
        var sender = new FakeSender();
        var outbox = new EmailOutbox(sender, context);

        await outbox.SendAsync(new EmailMessage
        {
            To = "guest@mail.com",
            Subject = "Reset",
            HtmlBody = "<a href=\"http://x/reset?token=SECRET\">Reset</a>",
            LogHtmlBody = "<p>link hidden</p>"
        }, AccountEmailTemplates.PasswordResetType, null);

        Assert.Contains("SECRET", sender.Sent.Single().HtmlBody);

        var log = await context.EmailLogs.SingleAsync();
        Assert.DoesNotContain("SECRET", log.HtmlBody);
    }
}
