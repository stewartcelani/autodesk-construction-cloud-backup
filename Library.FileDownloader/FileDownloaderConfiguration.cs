using Polly;
using Polly.Retry;
using Library.Logger;

namespace Library.FileDownloader;

public class FileDownloaderConfiguration
{
    public RetryPolicy RetryPolicy { get; }
    public ILogger? Logger { get; }

    /*
     * initialRetryDelayInSeconds doubles each retryAttempt
     * retryAttempts 12, initialRetryDelayInSeconds 4 = 5.2 minutes
     */
    public FileDownloaderConfiguration(
        int retryAttempts = 12,
        int initialRetryDelayInSeconds = 4,
        ILogger? logger = null)
    {
        RetryPolicy = GetRetryPolicy(retryAttempts, initialRetryDelayInSeconds);
        if (logger is not null)
            Logger = logger;
    }

    private static RetryPolicy GetRetryPolicy(int retryAttempts, int initialRetryDelayInSeconds)
    {
        return Policy
            .Handle<Exception>()
            .WaitAndRetry(retryAttempts,
                retryAttempt => TimeSpan.FromSeconds(retryAttempt * initialRetryDelayInSeconds));
    }
}