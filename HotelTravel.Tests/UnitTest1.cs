using HotelTravel.Infrastructure.Services;
using Xunit;

namespace HotelTravel.Tests;

public class BookingRetryTests
{
    [Fact]
    public async Task Retry_ShouldSucceed_OnSecondAttempt()
    {
        // Arrange
        int attempts = 0;

        // Act
        var result = await BookingCreationService.ExecuteWithRetryAsync(
            async () =>
            {
                attempts++;

                if (attempts == 1)
                {
                    throw new InvalidOperationException(
                        "Simulated deadlock");
                }

                await Task.CompletedTask;
                return "Success";
            },
            ex => ex is InvalidOperationException &&
                  ex.Message == "Simulated deadlock"
        );

        // Assert
        Assert.Equal("Success", result);
        Assert.Equal(2, attempts);
    }
    [Fact]
    public async Task Retry_ShouldStop_AfterThreeAttempts()
    {
        // Arrange
        int attempts = 0;

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () =>
            {
                await BookingCreationService.ExecuteWithRetryAsync(
                    async () =>
                    {
                        attempts++;

                        await Task.CompletedTask;

                        throw new InvalidOperationException(
                            "Simulated deadlock");

#pragma warning disable CS0162
                        return "Success";
#pragma warning restore CS0162
                    },
                    ex => ex is InvalidOperationException &&
                          ex.Message == "Simulated deadlock"
                );
            });

        // Assert
        Assert.Equal(3, attempts);
    }
    [Fact]
    public async Task Retry_ShouldNotRetry_NonDeadlockException()
    {
        // Arrange
        int attempts = 0;

        // Act
        await Assert.ThrowsAsync<ArgumentException>(
            async () =>
            {
                await BookingCreationService.ExecuteWithRetryAsync(
                    async () =>
                    {
                        attempts++;

                        await Task.CompletedTask;

                        throw new ArgumentException("Invalid data");

#pragma warning disable CS0162
                        return "Success";
#pragma warning restore CS0162
                    },
                    ex => ex is InvalidOperationException
                );
            });

        // Assert
        Assert.Equal(1, attempts);
    }
    [Fact]
    public void IsDeadlock_ShouldReturnFalse_ForOtherExceptions()
    {
        // Arrange
        var exception = new InvalidOperationException(
            "Database connection timeout");

        // Act
        bool result = SqlServerErrorHelper.IsDeadlock(exception);

        // Assert
        Assert.False(result);
    }
    [Theory]
    [InlineData(1205, true)]
    [InlineData(-2, false)]
    [InlineData(2627, false)]
    public void IsDeadlockErrorNumber_ShouldIdentifyDeadlock(
    int errorNumber,
    bool expected)
    {
        // Act
        bool result = SqlServerErrorHelper
            .IsDeadlockErrorNumber(errorNumber);

        // Assert
        Assert.Equal(expected, result);
    }
}