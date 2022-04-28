using Polly;
using Polly.Retry;
using System;

namespace Library.FileDownloader;

public class FileDownloaderConfiguration
{
    public RetryPolicy RetryPolicy { get; }
    public Action<string>? LoggingMethod { get; } = null;
    
    /*
     * initialRetryDelayInSeconds doubles each retryAttempt
     * retryAttempts 12, initialRetryDelayInSeconds 4 = 5.2 minutes
     */
    public FileDownloaderConfiguration(
        int retryAttempts = 12, 
        int initialRetryDelayInSeconds = 4, 
        Action<string>? loggingMethod = null)
    {
        RetryPolicy = GetRetryPolicy(retryAttempts, initialRetryDelayInSeconds);
        if (loggingMethod is not null)
            LoggingMethod = loggingMethod;
    }
    
    private static RetryPolicy GetRetryPolicy(int retryAttempts, int initialRetryDelayInSeconds)
    {
        return Policy
            .Handle<Exception>()
            .WaitAndRetry(retryAttempts, 
                retryAttempt =>
                {
                    int retryDelayInSeconds = retryAttempt * initialRetryDelayInSeconds;
                    return TimeSpan.FromSeconds(retryDelayInSeconds);
                });
    }
}