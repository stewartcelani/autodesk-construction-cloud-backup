using AutodeskConstructionCloud.ApiClient;
using AutodeskConstructionCloud.ApiClient.Entities;
using Library.Logger;

namespace AutodeskConstructionCloud.Backup;

public class Backup
{
    public BackupConfiguration Config { get; }
    public ApiClient.ApiClient ApiClient { get; }

    public Backup(BackupConfiguration config)
    {
        Config = config;
        ApiClient = TwoLeggedApiClient
            .Configure()
            .WithClientId(config.ClientId)
            .AndClientSecret(config.ClientSecret)
            .ForAccount(config.AccountId)
            .WithOptions(options =>
            {
                options.Logger = new NLogLogger(new NLogLoggerConfiguration()
                {
                    LogLevel = Library.Logger.LogLevel.Info,
                    LogToConsole = true
                });
                options.DryRun = config.DryRun;
                options.HubId = config.HubId;
                options.RetryAttempts = config.RetryAttempts;
                options.InitialRetryInSeconds = config.InitialRetryInSeconds;
                options.MaxDegreeOfParallelism = config.MaxDegreeOfParallelism;
            })
            .Create();
    }


    public Task<List<Project>> FilterProjects(IEnumerable<Project> projects)
    {
        List<Project> filteredProjects = new();
        string[] projectsToBackup = Config.ProjectsToBackup.ConvertAll(x => x.ToLower()).ToArray();
        string[] projectsToExclude = Config.ProjectsToExclude.ConvertAll(x => x.ToLower()).ToArray();
        foreach (Project project in projects.Where(project => project.Name != "Sample Project"))
        {
            if (Config.ProjectsToExclude.Count > 0)
            {
                if (Config.ProjectsToExclude.Contains(project.Name.ToLower()))
                {
                    continue;
                }
            }
            
            if (Config.ProjectsToBackup.Count > 0)
            {
                if (projectsToExclude.Contains(project.Name.ToLower()))
                {
                    filteredProjects.Add(project);
                }
            }
            else
            {
                filteredProjects.Add(project);
            }
        }

        return Task.FromResult(filteredProjects);
    }
}