namespace AutodeskConstructionCloud.Backup;

public class BackupConfiguration
{
    public string BackupDirectory { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string AccountId { get; set; }
    public string HubId { get; set; }
    public int MaxDegreeOfParallelism { get; set; }
    public int RetryAttempts { get; set; }
    public int InitialRetryInSeconds { get; set; }
    public bool DryRun { get; set; }
    public int BackupsToRotate { get; set; }
    public List<string> ProjectsToBackup { get; set; } = new();
    public List<string> ProjectsToExclude { get; set; } = new();
    public bool VerboseLogging { get; set; }
    public int SmtpPort { get; set; } = 25;
    public string? SmtpHost { get; set; }
    public string? SmtpFromAddress { get; set; }
    public string? SmtpFromName { get; set; }
    public string? SmtpToAddress { get; set; }
    public string? SmtpUsername { get; set; }
    public string? SmtpPassword { get; set; }
    public bool SmtpEnableSsl { get; set; } = false;
}