using Library.Logger;

namespace AutodeskConstructionCloud.ApiClient;

public class ApiClientOptions
{
    public HttpClient HttpClient { get; set; } = new HttpClient();
    public ILogger? Logger { get; set; }
    
    public ApiClientType ApiClientType { get; set; } = ApiClientType.BIM360;
    
    /*
     * Each retry attempt is RetryAttempt * InitialRetryInSeconds
     */
    public int RetryAttempts { get; set; } = 4;
    public int InitialRetryInSeconds { get; set; } = 2;
}