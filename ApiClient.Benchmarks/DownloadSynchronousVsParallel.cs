using ACC.ApiClient;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Library.Logger;
using Library.SecretsManager;

[SimpleJob(RunStrategy.Monitoring, launchCount: 1, warmupCount: 0, targetCount: 3)]
public class DownloadSynchronousVsParallel
{
    private readonly ApiClient _apiClient;

    private readonly string _projectId =
        SecretsManager.GetEnvironmentVariable("acc:benchmarks:DownloadSynchronousVsParallel:projectId");

    private readonly string _rootDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

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
                options.Logger = new NLogLogger(new NLogLoggerConfiguration
                {
                    LogLevel = LogLevel.Debug,
                    LogToConsole = true
                });
            })
            .Create();
    }

    [Benchmark]
    public async Task ParallelLoop()
    {
        var project = await _apiClient.GetProject(_projectId);
        await project.GetContentsRecursively();
        await project.DownloadContentsRecursively(_rootDirectory);
        Directory.Delete(_rootDirectory, true);
    }

    [Benchmark]
    public async Task SynchronousLoop()
    {
        var project = await _apiClient.GetProject(_projectId);
        await project.GetContentsRecursively();
        foreach (var file in project.FilesRecursive) 
            await _apiClient.DownloadFile(file, _rootDirectory);
        Directory.Delete(_rootDirectory, true);
    }
}