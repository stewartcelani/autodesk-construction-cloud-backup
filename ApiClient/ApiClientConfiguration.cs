using Polly;
using Polly.Retry;

namespace ACC.ApiClient;

public class ApiClientConfiguration : ApiClientOptions
{
    public ApiClientConfiguration(string clientId, string clientSecret, string accountId)
    {
        ClientId = clientId;
        ClientSecret = clientSecret;
        AccountId = accountId;
        RetryPolicy = GetRetryPolicy(RetryAttempts, InitialRetryInSeconds);
    }

    public string ClientId { get; }
    public string ClientSecret { get; }
    public string AccountId { get; }
    public AsyncRetryPolicy RetryPolicy { get; set; }

    public AsyncRetryPolicy GetRetryPolicy(int maxRetryAttempts, int initialRetryDelayInSeconds)
    {
        return Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                maxRetryAttempts,
                retryAttempt => TimeSpan.FromSeconds(retryAttempt * initialRetryDelayInSeconds),
                (exception, sleepDuration, retryCount, context) =>
                {
                    string message = "Error communicating with Autodesk API. " +
                                     "Expecting this to be a transient error. " +
                                     $"Retry {retryCount}/{maxRetryAttempts} in {sleepDuration.Seconds} seconds.";
                    Logger?.Warn(exception, message);
                });
    }
}