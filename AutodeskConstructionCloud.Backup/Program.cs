using AutodeskConstructionCloud.ApiClient;
using AutodeskConstructionCloud.ApiClient.Entities;
using AutodeskConstructionCloud.Backup;
using CommandLine;
using LogLevel = NLog.LogLevel;

BackupConfiguration? backupConfiguration = null;
Parser.Default.ParseArguments<CommandLineArgs>(args)
    .WithParsed(a =>
    {
        backupConfiguration = new BackupConfiguration()
        {
            BackupDirectory = a.BackupDirectory,
            ClientId = a.ClientId,
            ClientSecret = a.ClientSecret,
            AccountId = a.AccountId,
            HubId = string.IsNullOrEmpty(a.HubId) ? $"b.{a.AccountId}" : a.HubId,
            MaxDegreeOfParallelism = a.MaxDegreeOfParallelism,
            RetryAttempts = a.RetryAttempts,
            InitialRetryInSeconds = a.InitialRetryInSeconds,
            DryRun = a.DryRun,
            BackupsToRotate = a.BackupsToRotate,
            ProjectsToBackup = a.ProjectsToBackup.Select(s => s.Trim()).ToList(),
            ProjectsToExclude = a.ProjectsToExclude.Select(s => s.Trim()).ToList(),
            VerboseLogging = a.VerboseLogging,
            SmtpPort = a.SmtpPort,
            SmtpHost = a.SmtpHost,
            SmtpFromAddress = a.SmtpFromAddress,
            SmtpFromName = a.SmtpFromName,
            SmtpToAddress = a.SmtpToAddress,
            SmtpUsername = a.SmtpUsername,
            SmtpPassword = a.SmtpPassword,
            SmtpEnableSsl = a.SmtpEnableSsl
        };
    })
    .WithNotParsed(e =>
    {
        Console.ReadLine();
    });
if (backupConfiguration is null)
{
    throw new Exception("Error mapping command line arguments.");
}

var backup = new Backup(backupConfiguration);
List<Project> projects = await backup.ApiClient.GetProjects();
List<Project> filteredProjects = await backup.FilterProjects(projects);
foreach (Project filteredProject in filteredProjects)
{
    await filteredProject.GetContentsRecursively();
    await filteredProject.DownloadContentsRecursively(Path.Combine(backup.Config.BackupDirectory, filteredProject.Name));
}

Console.ReadLine();

