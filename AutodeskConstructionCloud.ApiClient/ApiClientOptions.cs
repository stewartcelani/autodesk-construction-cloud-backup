using Library.Logger;

namespace AutodeskConstructionCloud.ApiClient;

public class ApiClientOptions
{
    public ILogger? Logger { get; set; } = new NLogLogger();
    
    /*
     * RetryAttempts 15, InitialRetryInSeconds 2 is be 4 total minutes of retrying
     * Each retry attempt is RetryAttempt * InitialRetryInSeconds
     */
    public int RetryAttempts { get; set; } = 15;
    public int InitialRetryInSeconds { get; set; } = 2;
}