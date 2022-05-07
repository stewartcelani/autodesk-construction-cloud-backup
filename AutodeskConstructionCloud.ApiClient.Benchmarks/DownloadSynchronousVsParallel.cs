using AutodeskConstructionCloud.ApiClient;
using AutodeskConstructionCloud.ApiClient.Entities;
using BenchmarkDotNet.Attributes;
using Library.Logger;
using Library.SecretsManager;
using File = AutodeskConstructionCloud.ApiClient.Entities.File;

[SimpleJob(launchCount: 1, warmupCount: 1, targetCount: 3)]
[MemoryDiagnoser()]
public class DownloadSynchronousVsParallel
{
    private readonly ApiClient _apiClient;
    private readonly string _rootDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    private readonly string _projectId =
        SecretsManager.GetEnvironmentVariable("acc:benchmarks:DownloadSynchronousVsParallel:projectId");

    public DownloadSynchronousVsParallel()
    {
        string clientId = SecretsManager.GetEnvironmentVariable("acc:clientid");
        string clientSecret = SecretsManager.GetEnvironmentVariable("acc:clientsecret");
        string accountId = SecretsManager.GetEnvironmentVariable("acc:accountid");
        
        _apiClient = TwoLeggedApiClient
            .Configure()
            .WithClientId(clientId)
            .AndClientSecret(clientSecret)
            .ForAccount(accountId)
            .WithOptions(options =>
            {
                options.Logger = new NLogLogger(new NLogLoggerConfiguration()
                {
                    LogLevel = LogLevel.Debug,
                    LogToConsole = true
                });
                options.RetryAttempts = 12;
                options.InitialRetryInSeconds = 2;
            })
            .Create();
    }

    [Benchmark]
    public async Task ParallelLoop()
    {
        Project project = await _apiClient.GetProject(_projectId);
        await project.GetContentsRecursively();
        await project.DownloadContentsRecursively(_rootDirectory);
    }
    
    [Benchmark]
    public async Task SynchronousLoop()
    {
        Project project = await _apiClient.GetProject(_projectId);
        await project.GetContentsRecursively();
        foreach (File file in project.FilesRecursive)
        {
            await _apiClient.DownloadFile(file, _rootDirectory);
        }
    }
}