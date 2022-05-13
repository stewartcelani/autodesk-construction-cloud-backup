using CommandLine;

namespace ACC.Backup;

public class CommandLineArgs
{
    [Option(Required = true, HelpText = "Backup directory.")]
    public string BackupDirectory { get; set; }
    [Option(Required = true, HelpText = "Client ID of Autodesk Forge app.")]
    public string ClientId { get; set; }
    [Option(Required = true, HelpText = "Client secret of Autodesk Forge app.")]
    public string ClientSecret { get; set; }
    [Option(Required = true, HelpText = "Autodesk Construction Cloud account ID.")]
    public string AccountId { get; set; }
    [Option(Required = false,
        HelpText =
            "Autodesk Construction Cloud HubId, defaults to b.AccountId. See https://forge.autodesk.com/en/docs/data/v2/reference/http/hubs-hub_id-projects-project_id-GET/ for more information.")]
    public string HubId { get; set; } = string.Empty;
    [Option(Required = false, Default = 8,
        HelpText = "Number of files to download in parallel.")]
    public int MaxDegreeOfParallelism { get; set; }
    [Option(Required = false, Default = 15,
        HelpText = "Amount of times to retry when there are errors communicating with the Autodesk API.")]
    public int RetryAttempts { get; set; }
    [Option(Required = false, Default = 2,
        HelpText = "Each subsequent retry is RetryAttempt# * InitialRetryInSeconds. Default settings of 15 RetryAttempts with InitialRetryInSeconds 2 totals 4 minutes of retrying.")]
    public int InitialRetryInSeconds { get; set; }
    [Option(Required = false, Default = false,
        HelpText =
            "Backup will only create 0 byte placeholder files instead of downloading them, will still create full file structure.")]
    public bool DryRun { get; set; }
    [Option(Required = false, Default = 1, HelpText = "Number of backups to to maintain.")]
    public int BackupsToRotate { get; set; }
    [Option("projectstobackup", Required = false, Separator = ',',
        HelpText = "Comma separated list of project names to backup. If none given, all projects will be backed up.")]
    public IEnumerable<string> ProjectsToBackup { get; set; }

    [Option("projectstoexclude", Required = false, Separator = ',',
        HelpText =
            "Comma separated list of project names to exclude from the backup. Takes priority over 'projectstobackup'.")]
    public IEnumerable<string> ProjectsToExclude { get; set; }
    [Option(Required = false, Default = false, HelpText = "Enable verbose logging.")]
    public bool VerboseLogging { get; set; }
    [Option(Required = false, Default = 25, HelpText = "Backup summary notification email: SMTP port.")]
    public int SmtpPort { get; set; }
    [Option(Required = false, Default = null, HelpText = "Backup summary notification email: SMTP server name.")]
    public string? SmtpHost { get; set; }
    [Option(Required = false, Default = null, HelpText = "Backup summary notification email: SMTP from address.")]
    public string? SmtpFromAddress { get; set; }
    [Option(Required = false, Default = null, HelpText = "Backup summary notification email: SMTP from name.")]
    public string? SmtpFromName { get; set; }
    [Option(Required = false, Default = null, HelpText = "Backup summary notification email: SMTP to address.")]
    public string? SmtpToAddress { get; set; }
    [Option(Required = false, Default = null, HelpText = "Backup summary notification email: SMTP username.")]
    public string? SmtpUsername { get; set; }
    [Option(Required = false, Default = null, HelpText = "Backup summary notification email: SMTP password.")]
    public string? SmtpPassword { get; set; }
    [Option(Required = false, Default = false, HelpText = "Backup summary notification email: SMTP over SSL.")]
    public bool SmtpEnableSsl { get; set; } = false;
}