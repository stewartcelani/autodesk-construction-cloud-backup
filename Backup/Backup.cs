using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using ACC.ApiClient;
using ACC.ApiClient.Entities;
using ACC.Backup.Entities;
using Library.Logger;
using Newtonsoft.Json;
using File = System.IO.File;

namespace ACC.Backup;

public class Backup : IBackup
{
    private List<ProjectBackup> _projects = new();
    private DateTime? _backupStartTime;
    private DateTime? _backupEndTime;
    private TimeSpan _totalWaitTime = TimeSpan.Zero;
    private TimeSpan _totalDownloadTime = TimeSpan.Zero;
    private int _projectsProcessed = 0;

    public Backup(BackupConfiguration config, ILogger logger)
    {
        Config = config;
        Logger = logger;
        Logger.Debug("Building ApiClient");
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

    public BackupConfiguration Config { get; }
    public ApiClient.ApiClient ApiClient { get; }
    public ILogger Logger { get; }

    public async Task Run()
    {
        Logger.Info("=> Starting Autodesk Construction Cloud Backup");
        _backupStartTime = DateTime.Now;
        var allProjects = await ApiClient.GetProjects();
        var filteredProjects = FilterProjects(allProjects);
        _projects = filteredProjects.Select(CastProjectToProjectBackup).ToList();
        if (_projects.Count == 0)
        {
            Logger.Info("No projects found.");
            Logger.Info("=> Closing Autodesk Construction Cloud Backup");
            return;
        }

        // Create the timestamped backup directory first
        Config.BackupDirectory = Path.Combine(Config.BackupDirectory, DateTime.Now.ToString("yyyy-MM-dd_HH-mm"));

        // Load previous backup manifest for incremental backup (before any rotation)
        var previousManifest = LoadPreviousBackupManifest();
        if (previousManifest != null) ApiClient.SetPreviousBackupManifest(previousManifest);

        LogProjectsSelectedForBackup(_projects);

        // Create a channel for the producer-consumer pipeline
        var enumerationChannel = Channel.CreateUnbounded<ProjectBackup>(new UnboundedChannelOptions
        {
            SingleWriter = false, // Multiple enumeration tasks will write
            SingleReader = true   // Single download task will read
        });

        // Producer task: Enumerate projects with controlled concurrency
        var enumerationTask = Task.Run(async () =>
        {
            var semaphore = new SemaphoreSlim(4, 4); // Allow up to 4 concurrent enumerations
            var enumerationTasks = new List<Task>();
            
            foreach (var project in _projects)
            {
                var projectTask = Task.Run(async () =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        Logger.Info($"=> Enumerating project {project.Name} ({project.ProjectId})");
                        Logger.Info("Querying for list of folders and files, this may take a while depending on project size.");
                        project.BackupStartedAt = DateTime.Now;
                        await project.GetContentsRecursively();
                        Logger.Info($"=> Enumeration complete for {project.Name}, queuing for download");
                        await enumerationChannel.Writer.WriteAsync(project);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, $"Failed to enumerate project {project.Name} ({project.ProjectId})");
                        // Still mark as started/finished for summary reporting
                        if (project.BackupStartedAt == null)
                            project.BackupStartedAt = DateTime.Now;
                        project.BackupFinishedAt = DateTime.Now;
                        // Queue it anyway so it appears in the summary as failed
                        await enumerationChannel.Writer.WriteAsync(project);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });
                enumerationTasks.Add(projectTask);
            }
            
            await Task.WhenAll(enumerationTasks);
            enumerationChannel.Writer.Complete();
        });

        // Consumer task: Download projects sequentially with wait time tracking
        var downloadTask = Task.Run(async () =>
        {
            DateTime? lastDownloadEndTime = null;
            var isFirstProject = true;
            
            await foreach (var project in enumerationChannel.Reader.ReadAllAsync())
            {
                // Track wait time (time between downloads)
                var projectReceivedTime = DateTime.Now;
                if (lastDownloadEndTime.HasValue)
                {
                    var waitTime = projectReceivedTime - lastDownloadEndTime.Value;
                    _totalWaitTime = _totalWaitTime.Add(waitTime);
                    Logger.Debug($"Waited {waitTime.TotalSeconds:F1}s for next project to be enumerated");
                }
                else if (!isFirstProject)
                {
                    // For the first project after initialization
                    var waitTime = projectReceivedTime - _backupStartTime!.Value;
                    _totalWaitTime = _totalWaitTime.Add(waitTime);
                }
                isFirstProject = false;
                
                // Download the project and track active download time
                var downloadStartTime = DateTime.Now;
                Logger.Info($"=> Downloading project {project.Name} ({project.ProjectId})");
                Logger.Info("Backup beginning.");
                
                // Use sanitized project name for directory creation
                var sanitizedProjectName = SanitizeProjectName(project.Name);
                await project.DownloadContentsRecursively(Path.Combine(Config.BackupDirectory, sanitizedProjectName));
                
                var downloadEndTime = DateTime.Now;
                var downloadTime = downloadEndTime - downloadStartTime;
                _totalDownloadTime = _totalDownloadTime.Add(downloadTime);
                lastDownloadEndTime = downloadEndTime;
                _projectsProcessed++;
                
                project.BackupFinishedAt = downloadEndTime;
                Logger.Info($"=> Finished downloading project {project.Name} ({project.ProjectId})");
                LogBackupSummaryLine(project);
            }
        });

        // Wait for both tasks to complete
        await Task.WhenAll(enumerationTask, downloadTask);
        _backupEndTime = DateTime.Now;

        LogBackupSummary(_projects);

        // Log incremental backup statistics
        LogIncrementalBackupStats();
        
        // Log pipeline efficiency statistics
        LogPipelineEfficiencyStats();

        // Generate and save backup manifest for incremental backups
        GenerateBackupManifest(_projects);

        // Rotate old backups AFTER successful backup completion
        RotateBackupDirectories();

        if (Config.SmtpHost is not null && Config.SmtpFromAddress is not null && Config.SmtpToAddress is not null)
            EmailBackupSummary(_projects);

        Logger.Info("=> Closing Autodesk Construction Cloud Backup");
    }

    private ProjectBackup CastProjectToProjectBackup(Project project)
    {
        var projectBackup = new ProjectBackup(ApiClient);
        foreach (var propBase in typeof(Project).GetProperties().Where(p => p.CanRead && p.CanWrite))
        {
            var propDerived = typeof(ProjectBackup).GetProperty(propBase.Name);
            propDerived?.SetValue(projectBackup, propBase.GetValue(project, null), null);
        }

        return projectBackup;
    }

    private void LogBackupSummary(List<ProjectBackup> projects)
    {
        var summary = new List<string>
        {
            "=================================================================================",
            " => BACKUP SUMMARY",
            "================================================================================="
        };
        summary.AddRange(GetBackupSummary(projects));
        summary.Add("=================================================================================");
        foreach (var s in summary) Logger.Info(s);
    }

    private void EmailBackupSummary(List<ProjectBackup> projects)
    {
        Logger.Trace("Top");
        if (Config.SmtpHost is null || Config.SmtpFromAddress is null || Config.SmtpToAddress is null)
        {
            Logger.Debug(
                "Returning early as one of Config.SmtpHost, Config.SmtpFromAddress or Config.SmtpToAddress is null.");
            return;
        }

        var htmlSummary = new List<string> { "<h2>ACCBackup Backup Summary</h2>" };
        htmlSummary.AddRange(GetBackupSummary(projects).Select(s => $"<p>{s}</p>"));
        
        // Add incremental backup statistics to email
        var incrementalStatsHtml = GetIncrementalBackupStatsHtml();
        if (!string.IsNullOrEmpty(incrementalStatsHtml))
        {
            htmlSummary.Add(incrementalStatsHtml);
        }
        
        // Add pipeline efficiency statistics to email
        var pipelineStatsHtml = GetPipelineEfficiencyHtml();
        if (!string.IsNullOrEmpty(pipelineStatsHtml))
        {
            htmlSummary.Add(pipelineStatsHtml);
        }

        try
        {
            using var smtp = new SmtpClient();
            smtp.Host = Config.SmtpHost;
            smtp.Port = Config.SmtpPort;
            smtp.EnableSsl = Config.SmtpEnableSsl;
            if (Config.SmtpUsername != null && Config.SmtpPassword != null)
                smtp.Credentials = new NetworkCredential(Config.SmtpUsername, Config.SmtpPassword);

            var message = new MailMessage();
            message.From = string.IsNullOrEmpty(Config.SmtpFromName)
                ? new MailAddress(Config.SmtpFromAddress)
                : new MailAddress(Config.SmtpFromAddress, Config.SmtpFromName);
            message.To.Add(new MailAddress(Config.SmtpToAddress));
            message.Subject = GetBackupSummaryHeader(projects);
            message.SubjectEncoding = Encoding.UTF8;
            message.Body = string.Join("", htmlSummary.ToArray());
            message.BodyEncoding = Encoding.UTF8;
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
        if (projects.Count == 0) throw new ArgumentNullException(nameof(projects));

        if (projects.Any(project => project.BackupStartedAt is null || project.BackupFinishedAt is null))
            throw new NullReferenceException("Cannot get backup summary with null BackupStartedAt or BackupFinishedAt");

        var summary = new List<string> { $"  => {GetBackupSummaryHeader(projects)}" };
        summary.AddRange(projects.Select(GetBackupSummaryLine));
        var totalApiReportedStorageSizeInMb =
            projects.SelectMany(p => p.FilesRecursive).Select(f => f.ApiReportedStorageSizeInMb).Sum();
        var totalFileSizeOnDiskInMb =
            projects.SelectMany(p => p.FilesRecursive).Select(f => f.FileSizeOnDiskInMb).Sum();
        // Calculate actual total backup time using min start and max end times
        var earliestStart = projects.Where(p => p.BackupStartedAt.HasValue)
            .Min(p => p.BackupStartedAt!.Value);
        var latestEnd = projects.Where(p => p.BackupFinishedAt.HasValue)
            .Max(p => p.BackupFinishedAt!.Value);
        var ts = latestEnd - earliestStart;
        var backupDuration = ts.ToString(@"hh\:mm\:ss");
        summary.Add(
            @$"  => Backed up {totalFileSizeOnDiskInMb}/{totalApiReportedStorageSizeInMb} MB in {backupDuration} to {Config.BackupDirectory}");
        return summary;
    }

    private static string GetBackupSummaryHeader(List<ProjectBackup> projects)
    {
        if (projects.Count == 0) throw new ArgumentNullException(nameof(projects));

        if (projects.Any(project => project.BackupStartedAt is null || project.BackupFinishedAt is null))
            throw new NullReferenceException("Cannot get backup summary with null BackupStartedAt or BackupFinishedAt");

        var totalFileSizeOnDiskInMb =
            projects.SelectMany(p => p.FilesRecursive).Select(f => f.FileSizeOnDiskInMb).Sum();
        // Calculate actual total backup time using min start and max end times
        var earliestStart = projects.Where(p => p.BackupStartedAt.HasValue)
            .Min(p => p.BackupStartedAt!.Value);
        var latestEnd = projects.Where(p => p.BackupFinishedAt.HasValue)
            .Max(p => p.BackupFinishedAt!.Value);
        var ts = latestEnd - earliestStart;
        var backupDuration = ts.ToString(@"hh\:mm\:ss");
        var backupSummaryLines = projects.Select(GetBackupSummaryLine).ToList();
        var successCount = backupSummaryLines.Count(s => s.Contains("[SUCCESS]"));
        var partialFailCount = backupSummaryLines.Count(s => s.Contains("[PARTIAL FAIL]"));
        var errorCount = backupSummaryLines.Count(s => s.Contains("[ERROR]"));

        return
            @$"ACCBackup: {projects.Count} projects ({successCount} success, {partialFailCount} partial fail, {errorCount} error) - {totalFileSizeOnDiskInMb} MB in {backupDuration} ";
    }

    private static string GetBackupSummaryLine(ProjectBackup project)
    {
        if (project.BackupStartedAt is null || project.BackupFinishedAt is null)
            throw new NullReferenceException("Cannot get backup summary with null BackupStartedAt or BackupFinishedAt");

        var totalApiReportedStorageSizeInMb =
            project.FilesRecursive.Select(f => f.ApiReportedStorageSizeInMb).Sum();
        var totalFileSizeOnDiskInMb = project.FilesRecursive.Select(f => f.FileSizeOnDiskInMb).Sum();
        var ts = (TimeSpan)(project.BackupFinishedAt - project.BackupStartedAt);
        var backupDuration = ts.ToString(@"hh\:mm\:ss");
        var totalFiles = project.FilesRecursive.Count();
        var filesDownloaded = project.FilesRecursive.Select(f => f.Downloaded).Count();
        var totalFolders = project.SubfoldersRecursive.Count();
        var foldersCreated = project.SubfoldersRecursive.Select(f => f.Created).Count();
        var allFilesDownloaded = project.FilesRecursive.All(f => f.Downloaded);
        var allFoldersCreated = project.SubfoldersRecursive.All(f => f.Created);
        var totalFileSizeOnDiskMatchesFileSizeReportedByApi =
            totalFileSizeOnDiskInMb == totalApiReportedStorageSizeInMb;
        var summary = string.Empty;
        if (allFilesDownloaded && allFoldersCreated && totalFileSizeOnDiskMatchesFileSizeReportedByApi)
        {
            summary +=
                @$"  + [SUCCESS] {project.Name} ({project.ProjectId}) - {totalFileSizeOnDiskInMb} MB in {backupDuration} - {filesDownloaded} files backed up in {foldersCreated} folders";
        }
        else
        {
            if (filesDownloaded > 0)
                summary += "  + [PARTIAL FAIL] ";
            else
                summary += "  + [ERROR] ";

            summary +=
                @$"{project.Name} ({project.ProjectId}) - {totalFileSizeOnDiskInMb}/{totalApiReportedStorageSizeInMb} MB in {backupDuration} - {filesDownloaded}/{totalFiles} files backed up in {foldersCreated}/{totalFolders} folders";
        }

        return summary;
    }


    private void LogBackupSummaryLine(ProjectBackup project)
    {
        var summary = GetBackupSummaryLine(project);
        if (summary.Contains("[SUCCESS]"))
            Logger.Info(summary);
        else if (summary.Contains("[PARTIAL FAIL]"))
            Logger.Warn(summary);
        else
            Logger.Error(summary);
    }

    private void LogProjectsSelectedForBackup(List<ProjectBackup> projects)
    {
        Logger.Info("=================================================================================");
        Logger.Info($"=> Found {projects.Count} projects to backup:");
        foreach (var project in projects) Logger.Info($"    - {project.Name} ({project.ProjectId})");

        Logger.Info("=================================================================================");
    }

    private void RotateBackupDirectories()
    {
        Logger.Debug("Rotating backup directories.");

        // Get the executable's directory to ensure we never try to delete it
        var executablePath = Assembly.GetExecutingAssembly().Location;
        var executableDirectory = Path.GetDirectoryName(executablePath) ?? "";

        // Get the parent directory and current backup directory
        var currentBackupDir = Config.BackupDirectory.TrimEnd('\\');
        var parentDir = Directory.GetParent(currentBackupDir)?.FullName;

        if (string.IsNullOrEmpty(parentDir))
        {
            Logger.Debug("No parent directory found, skipping rotation");
            return;
        }

        do
        {
            // Get all backup directories except the current one
            var backupDirectories = GetDirectories(parentDir)
                .Where(d => !d.FullName.Equals(currentBackupDir, StringComparison.OrdinalIgnoreCase))
                .ToList();

            Logger.Trace(
                $"Root backup directory ({parentDir}) contains {backupDirectories.Count + 1} backup directories (including current), keeping {Config.BackupsToRotate} previous + 1 current");

            // Keep BackupsToRotate number of previous backups (plus the current one)
            // This ensures incremental backup always has at least one previous backup to compare
            if (backupDirectories.Count <= Config.BackupsToRotate) break;

            var oldestDirectory = backupDirectories.MinBy(x => x.CreationTime)!;

            // Additional safety check: never delete the directory containing the executable
            if (oldestDirectory.FullName.Equals(executableDirectory, StringComparison.OrdinalIgnoreCase) ||
                executableDirectory.StartsWith(oldestDirectory.FullName, StringComparison.OrdinalIgnoreCase))
            {
                Logger.Warn($"Skipping deletion of {oldestDirectory.FullName} as it contains the running executable");
                break;
            }

            Logger.Info($"=> Deleting old backup directory {oldestDirectory.Name} to maintain rotation limit");
            oldestDirectory.Delete(true);
        } while (true);

        Logger.Trace("Backup rotation complete.");
    }

    private static List<DirectoryInfo> GetDirectories(string path)
    {
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);

        // Only return directories that match the backup timestamp pattern (yyyy-MM-dd_HH-mm)
        // This prevents attempting to delete non-backup directories like the executable's folder
        var backupDirPattern = new Regex(@"^\d{4}-\d{2}-\d{2}_\d{2}-\d{2}$");

        var allDirectories = Directory.GetDirectories(path);
        return allDirectories
            .Select(p => new DirectoryInfo(p))
            .Where(dir => backupDirPattern.IsMatch(dir.Name))
            .ToList();
    }


    /*
     * Filters down ApiClient.GetProjects() (all projects) based on commandline parameters --projectstobackup
     * and --projectstoexclude. Will filter based on either project name or project id. As project name can change
     * the project id is the recommended way.
     */
    private List<Project> FilterProjects(IEnumerable<Project> projects)
    {
        List<Project> filteredProjects = new();
        List<string> toBackup = new();
        foreach (var s in Config.ProjectsToBackup.ConvertAll(x => x.ToLower()))
        {
            toBackup.Add(s);
            toBackup.Add(
                $"b.{s}"); // ProjectId from Autodesk API will usually be b.GUID even if, in the web browser, the URL/ID contains just GUID. This will allow users to just enter a GUID in --projectstobackup or --projectstoexclude flag without worrying about quirks of the API.
        }

        List<string> toExclude = new();
        foreach (var s in Config.ProjectsToExclude.ConvertAll(x => x.ToLower()))
        {
            toExclude.Add(s);
            toExclude.Add($"b.{s}");
        }

        var projectsToBackup = toBackup.ToArray();
        var projectsToExclude = toExclude.ToArray();
        foreach (var project in projects.Where(project => project.Name != "Sample Project"))
        {
            if (Config.ProjectsToExclude.Count > 0)
                if (projectsToExclude.Contains(project.Name.ToLower()) ||
                    projectsToExclude.Contains(project.ProjectId.ToLower()))
                    continue;

            if (Config.ProjectsToBackup.Count > 0)
            {
                if (projectsToBackup.Contains(project.Name.ToLower()) ||
                    projectsToBackup.Contains(project.ProjectId.ToLower()))
                    filteredProjects.Add(project);
            }
            else
            {
                filteredProjects.Add(project);
            }
        }

        return filteredProjects;
    }

    private void LogIncrementalBackupStats()
    {
        var stats = ApiClient.GetIncrementalStats();
        if (stats.copied > 0 || stats.downloaded > 0)
        {
            Logger.Info("=================================================================================");
            Logger.Info("=> Incremental Backup Statistics:");
            Logger.Info($"    Files copied from previous backup: {stats.copied} ({FormatBytes(stats.bytesCopied)})");
            Logger.Info(
                $"    Files downloaded from Autodesk: {stats.downloaded} ({FormatBytes(stats.bytesDownloaded)})");
            Logger.Info($"    Total files processed: {stats.copied + stats.downloaded}");

            if (stats.copied > 0)
            {
                var timeSaved = EstimateTimeSaved(stats.bytesCopied);
                Logger.Info($"    Estimated time saved: {timeSaved}");
                Logger.Info($"    Bandwidth saved: {FormatBytes(stats.bytesCopied)}");
                
                // Show efficiency percentage
                var totalBytes = stats.bytesCopied + stats.bytesDownloaded;
                if (totalBytes > 0)
                {
                    var efficiencyPercent = (stats.bytesCopied * 100.0) / totalBytes;
                    // If it rounds to 100% but we downloaded files, show 99.99% to be clear
                    if (Math.Round(efficiencyPercent, 2) >= 100.0 && stats.bytesDownloaded > 0)
                    {
                        efficiencyPercent = 99.99;
                    }
                    Logger.Info($"    Incremental efficiency: {efficiencyPercent:F2}% data reused from previous backup");
                }
            }

            Logger.Info("=================================================================================");
        }
    }

    private string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        var order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    private string EstimateTimeSaved(long bytesCopied)
    {
        // Estimate based on 5 MB/s download speed (adjust based on your typical speeds)
        double estimatedDownloadSpeed = 5 * 1024 * 1024; // 5 MB/s in bytes
        var secondsSaved = bytesCopied / estimatedDownloadSpeed;
        var timeSaved = TimeSpan.FromSeconds(secondsSaved);

        if (timeSaved.TotalHours >= 1)
            return $"{timeSaved.Hours}h {timeSaved.Minutes}m";
        if (timeSaved.TotalMinutes >= 1)
            return $"{timeSaved.Minutes}m {timeSaved.Seconds}s";
        return $"{timeSaved.Seconds}s";
    }
    
    private string GetIncrementalBackupStatsHtml()
    {
        var stats = ApiClient.GetIncrementalStats();
        if (stats.copied == 0 && stats.downloaded == 0)
            return string.Empty;
        
        var html = new List<string>();
        html.Add("<hr>");
        html.Add("<h3>Incremental Backup Statistics</h3>");
        html.Add("<table style='border-collapse: collapse; margin: 10px 0;'>");
        
        // Data copied from previous backup
        html.Add($"<tr><td style='padding: 5px 15px 5px 0;'><strong>Data copied from previous backup:</strong></td>");
        html.Add($"<td style='padding: 5px;'>{stats.copied} files ({FormatBytes(stats.bytesCopied)})</td></tr>");
        
        // Data downloaded from Autodesk
        html.Add($"<tr><td style='padding: 5px 15px 5px 0;'><strong>Data downloaded from Autodesk:</strong></td>");
        html.Add($"<td style='padding: 5px;'>{stats.downloaded} files ({FormatBytes(stats.bytesDownloaded)})</td></tr>");
        
        // Total files processed
        html.Add($"<tr><td style='padding: 5px 15px 5px 0;'><strong>Total files processed:</strong></td>");
        html.Add($"<td style='padding: 5px;'>{stats.copied + stats.downloaded}</td></tr>");
        
        if (stats.copied > 0)
        {
            // Bandwidth saved
            html.Add($"<tr><td style='padding: 5px 15px 5px 0;'><strong>Bandwidth saved:</strong></td>");
            html.Add($"<td style='padding: 5px;'>{FormatBytes(stats.bytesCopied)}</td></tr>");
            
            // Estimated time saved
            var timeSaved = EstimateTimeSaved(stats.bytesCopied);
            html.Add($"<tr><td style='padding: 5px 15px 5px 0;'><strong>Estimated time saved:</strong></td>");
            html.Add($"<td style='padding: 5px;'>{timeSaved}</td></tr>");
            
            // Efficiency percentage
            var totalBytes = stats.bytesCopied + stats.bytesDownloaded;
            if (totalBytes > 0)
            {
                var efficiencyPercent = (stats.bytesCopied * 100.0) / totalBytes;
                // If it rounds to 100% but we downloaded files, show 99.99% to be clear
                if (Math.Round(efficiencyPercent, 2) >= 100.0 && stats.bytesDownloaded > 0)
                {
                    efficiencyPercent = 99.99;
                }
                html.Add($"<tr><td style='padding: 5px 15px 5px 0;'><strong>Incremental efficiency:</strong></td>");
                html.Add($"<td style='padding: 5px;'>{efficiencyPercent:F2}% data reused from previous backup</td></tr>");
            }
        }
        
        html.Add("</table>");
        
        return string.Join("", html);
    }
    
    private void LogPipelineEfficiencyStats()
    {
        if (_projectsProcessed == 0)
            return;
            
        var totalPipelineTime = _totalDownloadTime + _totalWaitTime;
        if (totalPipelineTime == TimeSpan.Zero)
            return;
            
        var efficiencyPercent = (_totalDownloadTime.TotalSeconds / totalPipelineTime.TotalSeconds) * 100;
        var averageWaitTime = _projectsProcessed > 1 ? 
            TimeSpan.FromSeconds(_totalWaitTime.TotalSeconds / (_projectsProcessed - 1)) : 
            TimeSpan.Zero;
        
        Logger.Info("=================================================================================");
        Logger.Info("=> Pipeline Efficiency Statistics:");
        Logger.Info($"    Total pipeline time: {FormatTimeSpan(totalPipelineTime)}");
        Logger.Info($"    Active download time: {FormatTimeSpan(_totalDownloadTime)} ({efficiencyPercent:F1}%)");
        Logger.Info($"    Wait time (idle): {FormatTimeSpan(_totalWaitTime)} ({(100 - efficiencyPercent):F1}%)");
        Logger.Info($"    Projects processed: {_projectsProcessed}");
        
        if (_projectsProcessed > 1)
        {
            Logger.Info($"    Average wait between projects: {FormatTimeSpan(averageWaitTime)}");
        }
        
        if (_totalWaitTime.TotalMinutes > 1)
        {
            Logger.Info($"    Note: {FormatTimeSpan(_totalWaitTime)} spent waiting for project enumeration");
            Logger.Info($"          Consider increasing concurrent enumeration limit if bottlenecked");
        }
        
        Logger.Info("=================================================================================");
    }
    
    private string GetPipelineEfficiencyHtml()
    {
        if (_projectsProcessed == 0)
            return string.Empty;
            
        var totalPipelineTime = _totalDownloadTime + _totalWaitTime;
        if (totalPipelineTime == TimeSpan.Zero)
            return string.Empty;
            
        var efficiencyPercent = (_totalDownloadTime.TotalSeconds / totalPipelineTime.TotalSeconds) * 100;
        var averageWaitTime = _projectsProcessed > 1 ? 
            TimeSpan.FromSeconds(_totalWaitTime.TotalSeconds / (_projectsProcessed - 1)) : 
            TimeSpan.Zero;
        
        var html = new List<string>();
        html.Add("<hr>");
        html.Add("<h3>Pipeline Efficiency Statistics</h3>");
        html.Add("<table style='border-collapse: collapse; margin: 10px 0;'>");
        
        // Total pipeline time
        html.Add($"<tr><td style='padding: 5px 15px 5px 0;'><strong>Total pipeline time:</strong></td>");
        html.Add($"<td style='padding: 5px;'>{FormatTimeSpan(totalPipelineTime)}</td></tr>");
        
        // Active download time
        html.Add($"<tr><td style='padding: 5px 15px 5px 0;'><strong>Active download time:</strong></td>");
        html.Add($"<td style='padding: 5px;'>{FormatTimeSpan(_totalDownloadTime)} ({efficiencyPercent:F1}%)</td></tr>");
        
        // Wait time
        html.Add($"<tr><td style='padding: 5px 15px 5px 0;'><strong>Wait time (idle):</strong></td>");
        html.Add($"<td style='padding: 5px;'>{FormatTimeSpan(_totalWaitTime)} ({(100 - efficiencyPercent):F1}%)</td></tr>");
        
        // Projects processed
        html.Add($"<tr><td style='padding: 5px 15px 5px 0;'><strong>Projects processed:</strong></td>");
        html.Add($"<td style='padding: 5px;'>{_projectsProcessed}</td></tr>");
        
        if (_projectsProcessed > 1)
        {
            // Average wait time
            html.Add($"<tr><td style='padding: 5px 15px 5px 0;'><strong>Average wait between projects:</strong></td>");
            html.Add($"<td style='padding: 5px;'>{FormatTimeSpan(averageWaitTime)}</td></tr>");
        }
        
        // Pipeline efficiency
        html.Add($"<tr><td style='padding: 5px 15px 5px 0;'><strong>Pipeline efficiency:</strong></td>");
        html.Add($"<td style='padding: 5px;'>{efficiencyPercent:F1}% time spent downloading</td></tr>");
        
        html.Add("</table>");
        
        return string.Join("", html);
    }
    
    private string FormatTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan.TotalHours >= 1)
            return $"{(int)timeSpan.TotalHours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        if (timeSpan.TotalMinutes >= 1)
            return $"{(int)timeSpan.TotalMinutes:D2}:{timeSpan.Seconds:D2}";
        return $"{timeSpan.TotalSeconds:F1}s";
    }

    private BackupManifest? LoadPreviousBackupManifest()
    {
        if (Config.ForceFullDownload)
        {
            Logger.Info("=> Force full download enabled, skipping incremental backup optimization");
            return null;
        }

        try
        {
            // Get parent directory (two levels up since we already created the timestamped directory)
            // Config.BackupDirectory is now something like C:\Backup\2024-01-01_10-30
            // We need to look in C:\Backup for other backup directories
            var currentBackupDir = Config.BackupDirectory.TrimEnd('\\');
            var parentDir = Directory.GetParent(currentBackupDir)?.FullName;

            if (string.IsNullOrEmpty(parentDir))
            {
                Logger.Info("=> No parent directory found, performing full backup");
                return null;
            }

            // Get all backup directories (matching timestamp pattern)
            var backupDirectories = GetDirectories(parentDir)
                .Where(d => !d.FullName.Equals(currentBackupDir, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (backupDirectories.Count == 0)
            {
                Logger.Info("=> No previous backups found, performing full backup");
                return null;
            }

            // Sort by creation time descending to get the most recent
            var mostRecentBackup = backupDirectories
                .OrderByDescending(d => d.CreationTime)
                .FirstOrDefault();

            if (mostRecentBackup == null)
            {
                Logger.Info("=> No previous backups found, performing full backup");
                return null;
            }

            var manifestPath = Path.Combine(mostRecentBackup.FullName, "BackupManifest.json");
            if (!File.Exists(manifestPath))
            {
                Logger.Info($"=> Previous backup at {mostRecentBackup.Name} has no manifest, performing full backup");
                return null;
            }

            var manifest = BackupManifest.LoadFromFile(manifestPath);
            if (manifest != null)
            {
                // Validate the manifest by checking if at least some files exist
                var isValid = false;
                var filesToCheck = Math.Min(10, manifest.Files.Count); // Check up to 10 files
                var filesChecked = 0;

                foreach (var kvp in manifest.Files.Take(filesToCheck))
                {
                    // Convert forward slashes back to OS-specific separators for file path
                    var filePath = kvp.Key.Replace('/', Path.DirectorySeparatorChar);
                    var fullPath = Path.Combine(mostRecentBackup.FullName, filePath);

                    if (File.Exists(fullPath))
                    {
                        isValid = true;
                        break;
                    }

                    filesChecked++;
                }

                if (!isValid && filesChecked > 0)
                {
                    Logger.Warn("Previous backup manifest appears invalid (no files found), performing full backup");
                    return null;
                }

                Logger.Info($"=> Using previous backup from {mostRecentBackup.Name} for incremental sync");
                manifest.BackupDirectory = mostRecentBackup.FullName; // Update to actual directory path
            }

            return manifest;
        }
        catch (FileNotFoundException ex)
        {
            Logger.Warn($"Previous manifest file not found: {ex.Message}");
            return null;
        }
        catch (JsonException ex)
        {
            Logger.Warn($"Failed to parse previous manifest JSON: {ex.Message}");
            return null;
        }
        catch (UnauthorizedAccessException ex)
        {
            Logger.Warn($"Access denied to previous manifest: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Logger.Warn($"Unexpected error loading previous manifest: {ex.Message}");
            return null;
        }
    }

    private static string SanitizeProjectName(string projectName)
    {
        // Replace invalid path characters with underscore
        var invalidChars = Path.GetInvalidFileNameChars()
            .Concat(new[] { '/', '\\' }) // Also replace path separators
            .Distinct();

        var sanitized = projectName;
        foreach (var c in invalidChars) sanitized = sanitized.Replace(c, '_');

        return sanitized;
    }

    private void GenerateBackupManifest(List<ProjectBackup> projects)
    {
        try
        {
            var manifest = new BackupManifest
            {
                BackupDate = DateTime.Now,
                BackupDirectory = Config.BackupDirectory
            };

            foreach (var project in projects)
            foreach (var file in project.FilesRecursive)
            {
                // Use sanitized project name and normalize path separators for consistent manifest keys
                var sanitizedProjectName = SanitizeProjectName(project.Name);
                var relativePath = Path.Combine(sanitizedProjectName, file.GetPath()[1..])
                    .Replace('\\', '/');

                var entry = new FileManifestEntry
                {
                    FileId = file.FileId,
                    VersionNumber = file.VersionNumber,
                    LastModifiedTime = file.LastModifiedTime,
                    StorageSize = file.StorageSize,
                    Name = file.Name,
                    ProjectId = project.ProjectId
                };
                manifest.AddFile(relativePath, entry);
            }

            var manifestPath = Path.Combine(Config.BackupDirectory, "BackupManifest.json");
            manifest.SaveToFile(manifestPath);
            Logger.Info($"=> Backup manifest saved to {manifestPath}");
        }
        catch (Exception ex)
        {
            Logger.Warn($"Failed to generate backup manifest: {ex.Message}");
        }
    }
}