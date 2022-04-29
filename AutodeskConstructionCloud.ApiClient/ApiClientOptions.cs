using Library.Logger;

namespace AutodeskConstructionCloud.ApiClient;

public class ApiClientOptions
{
    public ILogger? Logger { get; set; }
    
    /*
     * Each retry attempt is RetryAttempt * InitialRetryInSeconds
     */
    public int RetryAttempts { get; set; } = 4;
    public int InitialRetryInSeconds { get; set; } = 2;
}