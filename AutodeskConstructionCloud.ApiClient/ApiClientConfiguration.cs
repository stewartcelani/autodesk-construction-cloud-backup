using Polly;
using Polly.Retry;

namespace AutodeskConstructionCloud.ApiClient;

public class ApiClientConfiguration : ApiClientOptions
{
    public string ClientId { get; }
    public string ClientSecret { get; }
    public string AccountId { get; }
    public RetryPolicy RetryPolicy { get; }

    public ApiClientConfiguration(string clientId, string clientSecret, string accountId)
    {
        ClientId = clientId;
        ClientSecret = clientSecret;
        AccountId = accountId;
        RetryPolicy = GetRetryPolicy(RetryAttempts, InitialRetryInSeconds);
    }

    private static RetryPolicy GetRetryPolicy(int retryAttempts, int initialRetryDelayInSeconds)
    {
        return Policy
            .Handle<Exception>()
            .WaitAndRetry(retryAttempts, 
                retryAttempt => TimeSpan.FromSeconds(retryAttempt * initialRetryDelayInSeconds)
            );
    }
}