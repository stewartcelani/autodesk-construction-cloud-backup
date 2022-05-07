using System.Net;
using System.Net.Http.Headers;
using AutodeskConstructionCloud.ApiClient;
using AutodeskConstructionCloud.ApiClient.Entities;
using AutodeskConstructionCloud.ApiClient.Tests;
using Castle.Components.DictionaryAdapter;
using FluentAssertions;
using Library.Logger;
using Library.SecretsManager;
using NLog;
using NSubstitute.Exceptions;
using LogLevel = Library.Logger.LogLevel;
using Library.Extensions;
using NSubstitute;
using File = AutodeskConstructionCloud.ApiClient.Entities.File;

//var clientId = "vpy4ARkUEXQ4lL4dmTneGwmb1tZ3i8Jk";
//var clientSecret = "k0G1TXacD0Ud88FE";
//var accountId = "a867403b-f199-44fa-bdd6-a5dac350a541";

string clientId = SecretsManager.GetEnvironmentVariableOrDefaultTo("acc:clientid", "InvalidClientId");
string clientSecret = SecretsManager.GetEnvironmentVariableOrDefaultTo("acc:clientsecret", "InvalidClientSecret");
string accountId = SecretsManager.GetEnvironmentVariableOrDefaultTo("acc:accountid", "InvalidAccountId");

ApiClient apiClient = TwoLeggedApiClient
    .Configure()
    .WithClientId(clientId)
    .AndClientSecret(clientSecret)
    .ForAccount(accountId)
    .WithOptions(options =>
    {
        options.Logger = new NLogLogger(new NLogLoggerConfiguration()
        {
            LogLevel = LogLevel.Trace,
            LogToConsole = true
        });
        options.RetryAttempts = 12;
        options.InitialRetryInSeconds = 2;
    })
    .Create();

List<Project> allProjects = await apiClient.GetProjects();
Project pilotProject = allProjects.First(x => x.Name == "Sydney West 132kvV Steelwork");

Console.ReadLine();

string rootBackupDirectory = Path.GetTempPath();
Project sampleProject = await apiClient.GetProject("b.62185181-412c-4c01-b45c-6fcd429e58b2");
string rootProjectDirectory = Path.Combine(rootBackupDirectory, Guid.NewGuid().ToString(), sampleProject.Name);

await sampleProject.GetContentsRecursively();
await sampleProject.DownloadContentsRecursively(rootProjectDirectory);

bool areAllFoldersCreated = sampleProject.SubfoldersRecursive.All(x => x.Created);
bool areAllFilesDownloads = sampleProject.FilesRecursive.All(x => x.Downloaded);

Console.ReadLine();

/*
Project sampleProject2 = await apiClient.GetProject("b.62185181-412c-4c01-b45c-6fcd429e58b2");
await sampleProject2.GetRootFolder();

string rootProjectDirectory2 = Path.Combine(rootBackupDirectory, Guid.NewGuid().ToString(), sampleProject2.Name);
await sampleProject2.RootFolder.DownloadContents(rootProjectDirectory2);

bool isRootFolderCreated = sampleProject2.RootFolder.Created;
bool areAllFilesDownloaded2 = sampleProject2.RootFolder.Files.All(x => x.Downloaded);

Console.ReadLine();

Project sampleProject3 = await apiClient.GetProject("b.62185181-412c-4c01-b45c-6fcd429e58b2");
await sampleProject3.GetContentsRecursively();

string rootProjectDirectory3 = Path.Combine(rootBackupDirectory, Guid.NewGuid().ToString(), sampleProject3.Name);
await sampleProject3.RootFolder.DownloadContentsRecursively(rootProjectDirectory3);

Console.ReadLine();
*/



/*
Folder rootFolder = FakeData.GetFakeFolder(apiClient);
rootFolder.Subfolders.Add(FakeData.GetFakeFolder(apiClient, rootFolder));
rootFolder.Subfolders[0].Subfolders.Add(FakeData.GetFakeFolder(apiClient, rootFolder.Subfolders[0]));

string path = rootFolder.Subfolders[0].Subfolders[0].GetPath(rootFolder.FolderId);
Console.ReadLine();
*/




















/*
const string clientId = "AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY";
const string clientSecret = "wE3GFhuIsGJEi3d4";
const string accountId = "48a4d1eb-a370-42fe-89c9-4dd9e2ad9d41";

ApiClient sut = TwoLeggedApiClient
    .Configure()
    .WithClientId(clientId)
    .AndClientSecret(clientSecret)
    .ForAccount(accountId)
    .WithOptions(options =>
    {
        options.Logger = new NLogLogger(new NLogLoggerConfiguration()
        {
            LogLevel = LogLevel.Trace,
            LogToConsole = true
        });
        options.RetryAttempts = 5;
        options.InitialRetryInSeconds = 2;
    })
    .Create();

// Act

await sut.EnsureAccessToken();
/*










/*
string clientId = SecretsManager.GetEnvironmentVariableOrDefaultTo("acc:clientid", "InvalidClientId");
string clientSecret = SecretsManager.GetEnvironmentVariableOrDefaultTo("acc:clientsecret", "InvalidClientSecret");
string accountId = SecretsManager.GetEnvironmentVariableOrDefaultTo("acc:accountid", "InvalidAccountId");

ApiClient client = TwoLeggedApiClient
    .Configure()
    .WithClientId(clientId)
    .AndClientSecret(clientSecret)
    .ForAccount(accountId)
    .WithOptions(options =>
    {
        options.Logger = new NLogLogger(new NLogLoggerConfiguration()
        {
            LogLevel = LogLevel.Trace,
            LogToConsole = true
        });
        options.RetryAttempts = 1;
        options.InitialRetryInSeconds = 2;
    })
    .Create();

List<Project> projects = await client.GetProjects();



Console.ReadLine();
*/









/*
ApiClient api1 = TwoLeggedApiClient
    .Configure()
    .WithClientId("AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY")
    .AndClientSecret("wE3GFhuIsGJEi3d4")
    .ForAccount("f33e018a-d1f5-4ef3-ae67-606de6aeed87")
    .WithOptions(options => 
    {
        options.RetryAttempts = 4;
        options.InitialRetryInSeconds = 15;
        options.Logger = new NLogLogger(); // Logger is null (no logging) by default
    })
    .Create();

Console.WriteLine(api1.Config.ClientId); // AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY
Console.WriteLine(api1.Config.RetryAttempts); // 4
Console.WriteLine(api1.Config.InitialRetryInSeconds); // 15
Console.WriteLine();


ApiClient api2 = TwoLeggedApiClient
    .Configure()
    .WithClientId("AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY")
    .AndClientSecret("wE3GFhuIsGJEi3d4")
    .ForAccount("f33e018a-d1f5-4ef3-ae67-606de6aeed87")
    .Create(); // .WithOptions can be skipped and default values are provided

ApiClient api3 = TwoLeggedApiClient
    .Configure()
    .WithClientId("AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY")
    .AndClientSecret("wE3GFhuIsGJEi3d4")
    .ForAccount("f33e018a-d1f5-4ef3-ae67-606de6aeed87")
    .WithOptions(options =>
    {
        options.InitialRetryInSeconds = 2;
        options.RetryAttempts = 2;
        options.Logger = new NLogLogger(new NLogLoggerConfiguration()
        {
            LogLevel = LogLevel.Trace,
            LogToConsole = true
        });
    })
    .Create();

Console.WriteLine(api3.Config.InitialRetryInSeconds);
Console.WriteLine(api3.Config.RetryAttempts);

await api3.GetAllProjects();
*/