using System.CodeDom.Compiler;
using Polly;
using Polly.Retry;

namespace AutodeskConstructionCloud.ApiClient;

public class ApiClientConfiguration : ApiClientOptions
{
    public string ClientId { get; }
    public string ClientSecret { get; }
    public string AccountId { get; }
    public AsyncRetryPolicy RetryPolicy { get; set; }

    public ApiClientConfiguration(string clientId, string clientSecret, string accountId)
    {
        ClientId = clientId;
        ClientSecret = clientSecret;
        AccountId = accountId;
        RetryPolicy = GetRetryPolicy(RetryAttempts, InitialRetryInSeconds);
    }

    public AsyncRetryPolicy GetRetryPolicy(int retryAttempts, int initialRetryDelayInSeconds)
    {
        return Policy
            .Handle<Exception>()
            .Or<HttpRequestException>()
            .WaitAndRetryAsync(
                retryCount: retryAttempts,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(retryAttempt * initialRetryDelayInSeconds),
                onRetry: (exception, sleepDuration, retryAttempt, context) =>
                {
                    int attemptsRemaining = retryAttempts - retryAttempt;
                    string message = $"Error communicating with Autodesk API. " +
                                     $"Retry {retryAttempt}/{retryAttempts} in {sleepDuration.Seconds} seconds.";
                    if (attemptsRemaining == 0)
                    {
                        Logger?.Error(exception, message);
                    }
                    else
                    {
                        Logger?.Warn(exception, message);
                    }
                });
    }
}