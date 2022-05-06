using System.CodeDom.Compiler;
using Polly;
using Polly.Retry;

namespace AutodeskConstructionCloud.ApiClient;

public class ApiClientConfiguration : ApiClientOptions
{
    public string ClientId { get; }
    public string ClientSecret { get; }
    public string AccountId { get; }
    public string HubId { get; set; }
    public AsyncRetryPolicy RetryPolicy { get; set; }

    public ApiClientConfiguration(string clientId, string clientSecret, string accountId)
    {
        ClientId = clientId;
        ClientSecret = clientSecret;
        AccountId = accountId;
        HubId = accountId;
        RetryPolicy = GetRetryPolicy(RetryAttempts, InitialRetryInSeconds);
    }

    public AsyncRetryPolicy GetRetryPolicy(int maxRetryAttempts, int initialRetryDelayInSeconds)
    {
        return Policy
            .Handle<HttpRequestException>()
            .WaitAndRetryAsync(
                retryCount: maxRetryAttempts,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(retryAttempt * initialRetryDelayInSeconds),
                onRetry: (exception, sleepDuration, retryCount, context) =>
                {
                    string message = "Error communicating with Autodesk API. " +
                                     "Expecting this to be a transient error. " +
                                     $"Retry {retryCount}/{maxRetryAttempts} in {sleepDuration.Seconds} seconds.";
                    Logger?.Warn(exception, message);
                });
    }
}