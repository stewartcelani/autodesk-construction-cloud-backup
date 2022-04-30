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
            .WaitAndRetryAsync(
                retryCount: retryAttempts,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(retryAttempt * initialRetryDelayInSeconds),
                onRetry: (exception, sleepDuration, retryAttempt, context) =>
                {
                    string message = "Error communicating with Autodesk API. " +
                                     "Expecting this to be a transient error. " +
                                     $"Retry {retryAttempt}/{retryAttempts} in {sleepDuration.Seconds} seconds.";
                    Logger?.Warn(exception, message);
                });
    }
}