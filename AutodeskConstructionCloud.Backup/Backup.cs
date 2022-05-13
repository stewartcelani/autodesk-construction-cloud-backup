﻿using System.Net;
using System.Net.Mail;
using System.Reflection;
using AutodeskConstructionCloud.ApiClient;
using AutodeskConstructionCloud.ApiClient.Entities;
using AutodeskConstructionCloud.Backup.Entities;
using Library.Logger;

namespace AutodeskConstructionCloud.Backup;

public class Backup : IBackup
{
    public BackupConfiguration Config { get; }
    public ApiClient.ApiClient ApiClient { get; }
    public ILogger Logger { get; }

    private List<ProjectBackup> _projects = new();

    public Backup(BackupConfiguration config)
    {
        Config = config;
        Logger = new NLogLogger(new NLogLoggerConfiguration()
        {
            LogLevel = Config.VerboseLogging ? Library.Logger.LogLevel.Trace : Library.Logger.LogLevel.Info,
            LogToConsole = true
        });
        Logger.Debug("Creating ApiClient");
        ApiClient = TwoLeggedApiClient
            .Configure()
            .WithClientId(config.ClientId)
            .AndClientSecret(config.ClientSecret)
            .ForAccount(config.AccountId)
            .WithOptions(options =>
            {
                options.Logger = Logger;
                options.DryRun = config.DryRun;
                options.HubId = config.HubId;
                options.RetryAttempts = config.RetryAttempts;
                options.InitialRetryInSeconds = config.InitialRetryInSeconds;
                options.MaxDegreeOfParallelism = config.MaxDegreeOfParallelism;
            })
            .Create();
        Logger.Trace("ApiClient created, constructor exiting.");
    }

    private ProjectBackup CastProjectToProjectBackup(Project project)
    {
        var projectBackup = new ProjectBackup(ApiClient);
        foreach (PropertyInfo propBase in typeof(Project).GetProperties().Where(p => p.CanRead && p.CanWrite))
        {
            PropertyInfo? propDerived = typeof(ProjectBackup).GetProperty(propBase.Name);
            propDerived?.SetValue(projectBackup, propBase.GetValue(project, null), null);
        }

        return projectBackup;
    }

    public async Task Run()
    {
        Logger.Info("=> Starting Autodesk Construction Cloud Backup");
        List<Project> allProjects = await ApiClient.GetProjects();
        IEnumerable<Project> filteredProjects = FilterProjects(allProjects);
        _projects = filteredProjects.Select(CastProjectToProjectBackup).ToList();
        if (_projects.Count == 0)
        {
            Logger.Info("No projects found.");
            Logger.Info("=> Closing Autodesk Construction Cloud Backup");
            return;
        }

        RotateBackupDirectories();
        LogProjectsSelectedForBackup(_projects);
        foreach (ProjectBackup project in _projects)
        {
            Logger.Info($"=> Processing project {project.Name} ({project.ProjectId})");
            project.BackupStartedAt = DateTime.Now;
            Logger.Info("Querying for list of folders and files, this may take a while depending on project size.");
            await project.GetContentsRecursively();
            Logger.Info("Backup beginning.");
            await project.DownloadContentsRecursively(Path.Combine(Config.BackupDirectory, project.Name));
            project.BackupFinishedAt = DateTime.Now;
            Logger.Info($"=> Finished processing project {project.Name} ({project.ProjectId})");
            LogBackupSummaryLine(project);
        }

        LogBackupSummary(_projects);
        EmailBackupSummary(_projects);
        Logger.Info("=> Closing Autodesk Construction Cloud Backup");
    }

    private void LogBackupSummary(List<ProjectBackup> projects)
    {
        IEnumerable<string> summary = GetBackupSummary(projects);
        foreach (string s in summary)
        {
            Logger.Info(s);
        }
    }

    private void EmailBackupSummary(List<ProjectBackup> projects)
    {
        Logger.Trace("Top");
        if (Config.SmtpHost is null || Config.SmtpFromAddress is null || Config.SmtpToAddress == null)
        {
            Logger.Debug(
                "Returning early as one of Config.SmtpHost, Config.SmtpFromAddress or Config.SmtpToAddress is null.");
            return;
        }

        var htmlSummary = new List<string> { "<h2>ACCBackup Backup Summary</h2>" };
        htmlSummary.AddRange(GetBackupSummary(projects).Select(s => $"<p>{s}</p>"));

        try
        {
            using var smtp = new SmtpClient();
            smtp.Host = Config.SmtpHost;
            smtp.Port = Config.SmtpPort;
            smtp.EnableSsl = Config.SmtpEnableSsl;
            if (Config.SmtpUsername != null && Config.SmtpPassword != null)
            {
                smtp.Credentials = new NetworkCredential(Config.SmtpUsername, Config.SmtpPassword);
            }
            var message = new MailMessage();
            message.From = string.IsNullOrEmpty(Config.SmtpFromName)
                ? new MailAddress(Config.SmtpFromAddress)
                : new MailAddress(Config.SmtpFromAddress, Config.SmtpFromName);
            message.To.Add(new MailAddress(Config.SmtpToAddress));
            message.Subject = GetEmailBackupSummarySubject(projects);
            message.SubjectEncoding = System.Text.Encoding.UTF8;
            message.Body = string.Join("", htmlSummary.ToArray());
            message.BodyEncoding = System.Text.Encoding.UTF8;
            message.IsBodyHtml = true;
            smtp.Send(message);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error emailing backup summary.");
        }
    }

    private IEnumerable<string> GetBackupSummary(List<ProjectBackup> projects)
    {
        if (projects.Count == 0)
        {
            throw new ArgumentNullException(nameof(projects));
        }

        if (projects.Any(project => project.BackupStartedAt is null || project.BackupFinishedAt is null))
        {
            throw new NullReferenceException("Cannot get backup summary with null BackupStartedAt or BackupFinishedAt");
        }

        var summary = new List<string>
        {
            "=================================================================================",
            "=> BACKUP SUMMARY",
            "================================================================================="
        };
        summary.AddRange(projects.Select(GetBackupSummaryLine));
        decimal totalStorageSizeInMb =
            Math.Round(projects.SelectMany(p => p.FilesRecursive).Select(f => f.StorageSizeInMb).Sum(), 2);
        var ts = (TimeSpan)(projects[^1].BackupFinishedAt - projects[0].BackupStartedAt);
        var backupDuration = ts.ToString(@"hh\:mm\:ss");
        summary.Add($"=> Backed up {totalStorageSizeInMb} MB in {backupDuration} to {Config.BackupDirectory}");
        summary.Add("=================================================================================");
        return summary;
    }

    private static string GetEmailBackupSummarySubject(List<ProjectBackup> projects)
    {
        if (projects.Count == 0)
        {
            throw new ArgumentNullException(nameof(projects));
        }

        if (projects.Any(project => project.BackupStartedAt is null || project.BackupFinishedAt is null))
        {
            throw new NullReferenceException("Cannot get backup summary with null BackupStartedAt or BackupFinishedAt");
        }


        decimal totalStorageSizeInMb =
            Math.Round(projects.SelectMany(p => p.FilesRecursive).Select(f => f.StorageSizeInMb).Sum(), 2);
        var ts = (TimeSpan)(projects[^1].BackupFinishedAt - projects[0].BackupStartedAt);
        var backupDuration = ts.ToString(@"hh\:mm\:ss");
        List<string> backupSummaryLines = projects.Select(GetBackupSummaryLine).ToList();
        int successCount = backupSummaryLines.Count(s => s.Contains("[SUCCESS]"));
        int partialFailCount = backupSummaryLines.Count(s => s.Contains("[PARTIAL FAIL]"));
        int errorCount = backupSummaryLines.Count(s => s.Contains("[ERROR]"));

        return
            $"ACCBackup: {projects.Count} projects ({successCount} success, {partialFailCount} partial fail, {errorCount} error) - {totalStorageSizeInMb} MB in {backupDuration} ";
    }

    private static string GetBackupSummaryLine(ProjectBackup project)
    {
        if (project.BackupStartedAt is null || project.BackupFinishedAt is null)
        {
            throw new NullReferenceException("Cannot get backup summary with null BackupStartedAt or BackupFinishedAt");
        }

        decimal projectStorageSizeInMb = Math.Round(project.FilesRecursive.Select(f => f.StorageSizeInMb).Sum(), 2);
        var ts = (TimeSpan)(project.BackupFinishedAt - project.BackupStartedAt);
        var backupDuration = ts.ToString(@"hh\:mm\:ss");
        int totalFiles = project.FilesRecursive.Count();
        int filesDownloaded = project.FilesRecursive.Select(f => f.Downloaded).Count();
        int totalFolders = project.SubfoldersRecursive.Count();
        int foldersCreated = project.SubfoldersRecursive.Select(f => f.Created).Count();
        bool allFilesDownloaded = project.FilesRecursive.All(f => f.Downloaded);
        bool allFoldersCreated = project.SubfoldersRecursive.All(f => f.Created);
        var summary = string.Empty;
        if (allFilesDownloaded && allFoldersCreated)
        {
            summary += "  + [SUCCESS] ";
        }
        else if (filesDownloaded > 0)
        {
            summary += "  + [PARTIAL FAIL] ";
        }
        else
        {
            summary += "  + [ERROR] ";
        }

        summary +=
            $"{project.Name} ({project.ProjectId}) - {projectStorageSizeInMb} MB in {backupDuration} - {filesDownloaded}/{totalFiles} files backed up in {foldersCreated}/{totalFolders} folders";
        return summary;
    }


    private void LogBackupSummaryLine(ProjectBackup project)
    {
        string summary = GetBackupSummaryLine(project);
        if (summary.Contains("[SUCCESS]"))
        {
            Logger.Info(summary);
        }
        else if (summary.Contains("[PARTIAL FAIL]"))
        {
            Logger.Warn(summary);
        }
        else
        {
            Logger.Error(summary);
        }
    }

    private void LogProjectsSelectedForBackup(List<ProjectBackup> projects)
    {
        Logger.Info("=================================================================================");
        Logger.Info($"=> Found {projects.Count} projects to backup:");
        foreach (ProjectBackup project in projects)
        {
            Logger.Info($"    - {project.Name} ({project.ProjectId})");
        }

        Logger.Info("=================================================================================");
    }

    private void RotateBackupDirectories()
    {
        Logger.Debug("Rotating backup directories.");
        do
        {
            List<DirectoryInfo> backupDirectories = GetDirectories(Config.BackupDirectory);
            Logger.Trace(
                $"Root backup directory ({Config.BackupDirectory}) contains {backupDirectories.Count}/{Config.BackupsToRotate} directories");
            if (backupDirectories.Count < Config.BackupsToRotate)
            {
                break;
            }

            DirectoryInfo oldestDirectory = backupDirectories.MinBy(x => x.CreationTime)!;
            Logger.Trace($"Deleting oldest backup directory {oldestDirectory.FullName}");
            oldestDirectory.Delete(true);
        } while (true);

        Config.BackupDirectory = Path.Combine(Config.BackupDirectory, DateTime.Now.ToString("yyyy-MM-dd_HH-mm"));
        Logger.Trace($"Exited backup rotation while loop with Config.BackupDirectory: {Config.BackupDirectory}.");
    }

    private static List<DirectoryInfo> GetDirectories(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        string[] backupDirectoriesArr = Directory.GetDirectories(path);
        return backupDirectoriesArr.Select(p => new DirectoryInfo(p)).ToList();
    }


    private IEnumerable<Project> FilterProjects(IEnumerable<Project> projects)
    {
        List<Project> filteredProjects = new();
        string[] projectsToBackup = Config.ProjectsToBackup.ConvertAll(x => x.ToLower()).ToArray();
        string[] projectsToExclude = Config.ProjectsToExclude.ConvertAll(x => x.ToLower()).ToArray();
        foreach (Project project in projects.Where(project => project.Name != "Sample Project"))
        {
            if (Config.ProjectsToExclude.Count > 0)
            {
                if (projectsToExclude.Contains(project.Name.ToLower()))
                {
                    continue;
                }
            }

            if (Config.ProjectsToBackup.Count > 0)
            {
                if (projectsToBackup.Contains(project.Name.ToLower()))
                {
                    filteredProjects.Add(project);
                }
            }
            else
            {
                filteredProjects.Add(project);
            }
        }

        return filteredProjects;
    }
}