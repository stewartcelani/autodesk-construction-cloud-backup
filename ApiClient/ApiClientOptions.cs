using Library.Logger;

namespace ACC.ApiClient;

public class ApiClientOptions
{
    public bool DryRun = false; // Instead of downloading files will create 0 byte placeholders
    public int MaxDegreeOfParallelism = 8; // Used by DownloadFiles Parallel.ForEachAsync

    public string HubId { get; set; } =
        string.Empty; // If empty it gets set to b.AccountId for BIM360 as even the ACC Build trials still use this convention

    public HttpClient HttpClient { get; set; } = new();
    public ILogger? Logger { get; set; }
    public int RetryAttempts { get; set; } = 15; // Each retry attempt is RetryAttempt# * InitialRetryInSeconds
    public int InitialRetryInSeconds { get; set; } = 2; // RetryAttempts 15 with InitialRetryInSeconds 2 = 4 minutes
}