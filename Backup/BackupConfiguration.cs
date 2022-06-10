using CommandLine;

namespace ACC.Backup;

public class BackupConfiguration
{
    public BackupConfiguration(IEnumerable<string> args)
    {
        Parser.Default.ParseArguments<CommandLineArgs>(args)
            .WithParsed(MapArgs);
    }

    public string BackupDirectory { get; set; }
    public string ClientId { get; private set; }
    public string ClientSecret { get; private set; }
    public string AccountId { get; private set; }
    public string HubId { get; private set; }
    public int MaxDegreeOfParallelism { get; private set; }
    public int RetryAttempts { get; private set; }
    public int InitialRetryInSeconds { get; private set; }
    public bool DryRun { get; private set; }
    public int BackupsToRotate { get; private set; }
    public List<string> ProjectsToBackup { get; private set; } = new();
    public List<string> ProjectsToExclude { get; private set; } = new();
    public bool DebugLogging { get; private set; }
    public bool TraceLogging { get; private set; }
    public int SmtpPort { get; private set; } = 25;
    public string? SmtpHost { get; private set; }
    public string? SmtpFromAddress { get; private set; }
    public string? SmtpFromName { get; private set; }
    public string? SmtpToAddress { get; private set; }
    public string? SmtpUsername { get; private set; }
    public string? SmtpPassword { get; private set; }
    public bool SmtpEnableSsl { get; private set; }

    private void MapArgs(CommandLineArgs commandLineArgs)
    {
        BackupDirectory = commandLineArgs.BackupDirectory.EndsWith(@"\")
            ? commandLineArgs.BackupDirectory
            : $@"{commandLineArgs.BackupDirectory}\";
        ClientId = commandLineArgs.ClientId;
        ClientSecret = commandLineArgs.ClientSecret;
        AccountId = commandLineArgs.AccountId;
        HubId = string.IsNullOrEmpty(commandLineArgs.HubId)
            ? $"b.{commandLineArgs.AccountId}"
            : commandLineArgs.HubId;
        MaxDegreeOfParallelism = commandLineArgs.MaxDegreeOfParallelism;
        RetryAttempts = commandLineArgs.RetryAttempts;
        InitialRetryInSeconds = commandLineArgs.InitialRetryInSeconds;
        DryRun = commandLineArgs.DryRun;
        BackupsToRotate = commandLineArgs.BackupsToRotate == 0 ? 1 : commandLineArgs.BackupsToRotate;
        ProjectsToBackup = commandLineArgs.ProjectsToBackup.Select(s => s.Trim()).ToList();
        ProjectsToExclude = commandLineArgs.ProjectsToExclude.Select(s => s.Trim()).ToList();
        DebugLogging = commandLineArgs.DebugLogging;
        TraceLogging = commandLineArgs.TraceLogging;
        SmtpPort = commandLineArgs.SmtpPort;
        SmtpHost = commandLineArgs.SmtpHost;
        SmtpFromAddress = commandLineArgs.SmtpFromAddress;
        SmtpFromName = commandLineArgs.SmtpFromName;
        SmtpToAddress = commandLineArgs.SmtpToAddress;
        SmtpUsername = commandLineArgs.SmtpUsername;
        SmtpPassword = commandLineArgs.SmtpPassword;
        SmtpEnableSsl = commandLineArgs.SmtpEnableSsl;
    }
}