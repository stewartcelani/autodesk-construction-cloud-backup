using System;
using System.Collections;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using ACC.ApiClient;
using FluentAssertions;
using NSubstitute;
using Polly;
using Library.Logger;
using Xunit;

namespace ApiClient.UnitTests;

public class RetryPolicyTests
{
    [Fact]
    public async Task RetryPolicy_With429AndRetryAfter_ShouldRespectServerDelay()
    {
        // Arrange
        const string clientId = "testClientId";
        const string clientSecret = "testSecret";
        const string accountId = "testAccountId";
        const int retryAfterSeconds = 5;
        
        var config = new ApiClientConfiguration(clientId, clientSecret, accountId);
        var mockLogger = Substitute.For<ILogger>();
        config.Logger = mockLogger;
        
        var retryPolicy = config.GetRetryPolicy(3, 2);
        
        var attempt = 0;
        var startTime = DateTime.UtcNow;
        
        // Create a 429 exception with RetryAfter data
        var exception = new HttpRequestException("Rate limited", null, HttpStatusCode.TooManyRequests);
        exception.Data["RetryAfter"] = retryAfterSeconds;
        
        // Act
        try
        {
            await retryPolicy.ExecuteAsync(async () =>
            {
                attempt++;
                if (attempt <= 2)
                {
                    throw exception;
                }
                await Task.CompletedTask;
            });
        }
        catch
        {
            // We expect it to succeed after retries
        }
        
        var elapsed = DateTime.UtcNow - startTime;
        
        // Assert
        attempt.Should().Be(3); // Initial attempt + 2 retries
        elapsed.TotalSeconds.Should().BeGreaterThan(retryAfterSeconds * 2); // Should wait at least 2x RetryAfter
        elapsed.TotalSeconds.Should().BeLessThan((retryAfterSeconds + 1) * 2 + 5); // With buffer and some tolerance
        
        // Verify logging
        mockLogger.Received(2).Warn(Arg.Any<Exception>(), Arg.Is<string>(s => s.Contains("Rate limit (429)")));
    }
    
    [Fact]
    public async Task RetryPolicy_With429NoRetryAfter_ShouldUseExponentialBackoff()
    {
        // Arrange
        const string clientId = "testClientId";
        const string clientSecret = "testSecret";
        const string accountId = "testAccountId";
        
        var config = new ApiClientConfiguration(clientId, clientSecret, accountId);
        var mockLogger = Substitute.For<ILogger>();
        config.Logger = mockLogger;
        
        var retryPolicy = config.GetRetryPolicy(3, 2);
        
        var attempt = 0;
        var startTime = DateTime.UtcNow;
        
        // Create a 429 exception without RetryAfter data
        var exception = new HttpRequestException("Rate limited", null, HttpStatusCode.TooManyRequests);
        
        // Act
        try
        {
            await retryPolicy.ExecuteAsync(async () =>
            {
                attempt++;
                if (attempt <= 2)
                {
                    throw exception;
                }
                await Task.CompletedTask;
            });
        }
        catch
        {
            // We expect it to succeed after retries
        }
        
        var elapsed = DateTime.UtcNow - startTime;
        
        // Assert
        attempt.Should().Be(3); // Initial attempt + 2 retries
        // Should use exponential backoff: 2 seconds for first retry, 4 seconds for second retry = 6 seconds total
        elapsed.TotalSeconds.Should().BeGreaterThan(5); // At least 6 seconds minus some tolerance
        elapsed.TotalSeconds.Should().BeLessThan(10); // But not too long
        
        // Verify logging mentions no Retry-After header
        mockLogger.Received(2).Warn(Arg.Any<Exception>(), Arg.Is<string>(s => s.Contains("no Retry-After header")));
    }
    
    [Fact]
    public async Task RetryPolicy_With429AndLargeRetryAfter_ShouldCapAt600Seconds()
    {
        // Arrange
        const string clientId = "testClientId";
        const string clientSecret = "testSecret";
        const string accountId = "testAccountId";
        const int retryAfterSeconds = 1200; // 20 minutes
        
        var config = new ApiClientConfiguration(clientId, clientSecret, accountId);
        var mockLogger = Substitute.For<ILogger>();
        config.Logger = mockLogger;
        
        var retryPolicy = config.GetRetryPolicy(1, 2);
        
        var attempt = 0;
        
        // Create a 429 exception with large RetryAfter data
        var exception = new HttpRequestException("Rate limited", null, HttpStatusCode.TooManyRequests);
        exception.Data["RetryAfter"] = retryAfterSeconds;
        
        // Act
        Exception caughtException = null;
        try
        {
            await retryPolicy.ExecuteAsync(async () =>
            {
                attempt++;
                throw exception;
            });
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }
        
        // Assert
        attempt.Should().Be(2); // Initial attempt + 1 retry
        caughtException.Should().NotBeNull();
        
        // Verify logging shows capped value (600 seconds + 1 buffer = 601)
        mockLogger.Received(1).Warn(Arg.Any<Exception>(), Arg.Is<string>(s => s.Contains("601 seconds")));
    }
    
    [Fact]
    public async Task RetryPolicy_With429AndInvalidRetryAfter_ShouldFallbackToExponentialBackoff()
    {
        // Arrange
        const string clientId = "testClientId";
        const string clientSecret = "testSecret";
        const string accountId = "testAccountId";
        
        var config = new ApiClientConfiguration(clientId, clientSecret, accountId);
        var mockLogger = Substitute.For<ILogger>();
        config.Logger = mockLogger;
        
        var retryPolicy = config.GetRetryPolicy(1, 2);
        
        var attempt = 0;
        
        // Create a 429 exception with invalid RetryAfter data
        var exception = new HttpRequestException("Rate limited", null, HttpStatusCode.TooManyRequests);
        exception.Data["RetryAfter"] = "invalid string"; // Invalid data type
        
        // Act
        Exception caughtException = null;
        try
        {
            await retryPolicy.ExecuteAsync(async () =>
            {
                attempt++;
                throw exception;
            });
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }
        
        // Assert
        attempt.Should().Be(2); // Initial attempt + 1 retry
        caughtException.Should().NotBeNull();
        
        // Verify it logged the fallback
        mockLogger.Received(1).Debug(Arg.Is<string>(s => s.Contains("Failed to parse RetryAfter")));
    }
    
    [Fact]
    public async Task RetryPolicy_WithNon429Error_ShouldUseExponentialBackoff()
    {
        // Arrange
        const string clientId = "testClientId";
        const string clientSecret = "testSecret";
        const string accountId = "testAccountId";
        
        var config = new ApiClientConfiguration(clientId, clientSecret, accountId);
        var mockLogger = Substitute.For<ILogger>();
        config.Logger = mockLogger;
        
        var retryPolicy = config.GetRetryPolicy(2, 3);
        
        var attempt = 0;
        var startTime = DateTime.UtcNow;
        
        // Create a non-429 exception
        var exception = new HttpRequestException("Server error", null, HttpStatusCode.InternalServerError);
        
        // Act
        try
        {
            await retryPolicy.ExecuteAsync(async () =>
            {
                attempt++;
                if (attempt <= 1)
                {
                    throw exception;
                }
                await Task.CompletedTask;
            });
        }
        catch
        {
            // We expect it to succeed after retry
        }
        
        var elapsed = DateTime.UtcNow - startTime;
        
        // Assert
        attempt.Should().Be(2); // Initial attempt + 1 retry
        // Should use exponential backoff: 3 seconds for first retry
        elapsed.TotalSeconds.Should().BeGreaterThan(2.5);
        elapsed.TotalSeconds.Should().BeLessThan(5);
        
        // Verify logging mentions transient error
        mockLogger.Received(1).Warn(Arg.Any<Exception>(), Arg.Is<string>(s => s.Contains("transient error")));
    }
    
    [Fact]
    public async Task RetryPolicy_WithForbidden_ShouldNotRetry()
    {
        // Arrange
        const string clientId = "testClientId";
        const string clientSecret = "testSecret";
        const string accountId = "testAccountId";
        
        var config = new ApiClientConfiguration(clientId, clientSecret, accountId);
        var mockLogger = Substitute.For<ILogger>();
        config.Logger = mockLogger;
        
        var retryPolicy = config.GetRetryPolicy(3, 2);
        
        var attempt = 0;
        
        // Create a 403 Forbidden exception (should not retry)
        var exception = new HttpRequestException("Forbidden", null, HttpStatusCode.Forbidden);
        
        // Act
        Exception caughtException = null;
        try
        {
            await retryPolicy.ExecuteAsync(async () =>
            {
                attempt++;
                throw exception;
            });
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }
        
        // Assert
        attempt.Should().Be(1); // Only initial attempt, no retries
        caughtException.Should().NotBeNull();
        caughtException.Should().BeOfType<HttpRequestException>();
        ((HttpRequestException)caughtException).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        
        // Verify no retry logging occurred
        mockLogger.DidNotReceive().Warn(Arg.Any<Exception>(), Arg.Any<string>());
    }
}