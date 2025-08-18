using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Library.SecretsManager;
using Microsoft.Extensions.Configuration;
using Xunit;

// ReSharper disable AsyncVoidLambda


namespace ACC.ApiClient.IntegrationTests;

public class ApiClientIntegrationTests
{
    private readonly string _accountId;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly IConfiguration _configuration;

    public ApiClientIntegrationTests()
    {
        _configuration = new ConfigurationBuilder()
            .AddUserSecrets<ApiClientIntegrationTests>()
            .AddEnvironmentVariables()
            .Build();

        _clientId = _configuration["acc:clientid"] ?? SecretsManager.GetEnvironmentVariable("acc:clientid");
        _clientSecret = _configuration["acc:clientsecret"] ?? SecretsManager.GetEnvironmentVariable("acc:clientsecret");
        _accountId = _configuration["acc:accountid"] ?? SecretsManager.GetEnvironmentVariable("acc:accountid");
    }

    [Fact]
    public async Task GetAccessToken_InvalidClientId_Should_Throw_403Forbidden()
    {
        // Arrange
        const string clientId = "AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY";
        const string clientSecret = "wE3GFhuIsGJEi3d4";
        const string accountId = "f33e018a-d1f5-4ef3-ae67-606de6aeed87";
        var sut = TwoLeggedApiClient
            .Configure()
            .WithClientId(clientId)
            .AndClientSecret(clientSecret)
            .ForAccount(accountId)
            .WithOptions(options =>
            {
                options.RetryAttempts = 1;
                options.InitialRetryInSeconds = 1;
            })
            .Create();

        // Act
        Func<Task> act = async () => await sut.GetProjects();

        // Assert
        await act
            .Should()
            .ThrowAsync<HttpRequestException>()
            .Where(e => e.StatusCode == HttpStatusCode.Forbidden && e.Message.Contains("AUTH-001"));
    }

    [Fact]
    public async Task GetAccessToken_InvalidClientSecret_Should_Throw_401Unauthorized()
    {
        // Arrange
        var clientId = _clientId;
        const string clientSecret = "InvalidClientSecret";
        const string accountId = "IrrelevantToTest";

        var sut = TwoLeggedApiClient
            .Configure()
            .WithClientId(clientId)
            .AndClientSecret(clientSecret)
            .ForAccount(accountId)
            .WithOptions(options =>
            {
                options.RetryAttempts = 1;
                options.InitialRetryInSeconds = 1;
            })
            .Create();

        // Act
        Func<Task> act = async () => await sut.GetProjects();

        // Assert
        await act
            .Should()
            .ThrowAsync<HttpRequestException>()
            .Where(e => e.StatusCode == HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task EnsureAccessToken_WithValidCredentials_Should_SetHttpAuthenticationHeader()
    {
        // Arrange
        var sut = TwoLeggedApiClient
            .Configure()
            .WithClientId(_clientId)
            .AndClientSecret(_clientSecret)
            .ForAccount(_accountId)
            .WithOptions(options =>
            {
                options.RetryAttempts = 1;
                options.InitialRetryInSeconds = 1;
            })
            .Create();
        var initialAuthHeader = sut.Config.HttpClient.DefaultRequestHeaders.Authorization;

        // Act
        await sut.GetProjects();

        // Assert
        initialAuthHeader.Should().BeNull();
        sut.Config.HttpClient.DefaultRequestHeaders.Authorization.Should().NotBeNull();
        Assert.NotEqual(initialAuthHeader, sut.Config.HttpClient.DefaultRequestHeaders.Authorization);
    }

    [Fact]
    public async Task GetProjects_Should_ReturnOneOrMoreProjects()
    {
        // Arrange
        var sut = TwoLeggedApiClient
            .Configure()
            .WithClientId(_clientId)
            .AndClientSecret(_clientSecret)
            .ForAccount(_accountId)
            .WithOptions(options =>
            {
                options.RetryAttempts = 1;
                options.InitialRetryInSeconds = 1;
            })
            .Create();

        // Act
        var projects = await sut.GetProjects();

        // Assert
        projects.Count.Should().BeGreaterThanOrEqualTo(1);
        projects.All(x => x.RootFolder == null).Should().BeTrue();
    }

    [Fact]
    public async Task GetProjects_getRootFolderContents_True_Should_ReturnOneOrMoreProjects()
    {
        // Arrange
        var sut = TwoLeggedApiClient
            .Configure()
            .WithClientId(_clientId)
            .AndClientSecret(_clientSecret)
            .ForAccount(_accountId)
            .WithOptions(options =>
            {
                options.RetryAttempts = 1;
                options.InitialRetryInSeconds = 1;
            })
            .Create();

        // Act
        var projects = await sut.GetProjects(true);

        // Assert
        projects.Count.Should().BeGreaterThanOrEqualTo(1);
        projects.All(x => x.RootFolder != null).Should().BeTrue();
    }

    [Fact(Timeout = 300000)] // 5 minute timeout
    public async Task Project_DownloadContentsRecursively_Should_DownloadContentsRecursively()
    {
        // Arrange
        var projectId = _configuration["acc:integrationtest:projectId"];
        if (string.IsNullOrEmpty(projectId))
            throw new InvalidOperationException("Project ID not configured in user secrets");
        var apiClient = TwoLeggedApiClient
            .Configure()
            .WithClientId(_clientId)
            .AndClientSecret(_clientSecret)
            .ForAccount(_accountId)
            .WithOptions(options =>
            {
                options.RetryAttempts = 12;
                options.InitialRetryInSeconds = 2;
            })
            .Create();
        var rootBackupDirectory = Path.GetTempPath();
        var sut = await apiClient.GetProject(projectId);
        var rootProjectDirectory = Path.Combine(rootBackupDirectory, Guid.NewGuid().ToString(), sut.Name);
        await sut.GetContentsRecursively();

        // Act
        await sut.DownloadContentsRecursively(rootProjectDirectory);

        // Assert
        sut.FilesRecursive.All(x => x.Downloaded).Should().BeTrue();
        sut.FilesRecursive.Count(x => x.Downloaded).Should().BeGreaterThanOrEqualTo(1);
        sut.SubfoldersRecursive.All(x => x.Created).Should().BeTrue();
        sut.SubfoldersRecursive.Count(x => x.Created).Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact(Timeout = 300000)] // 5 minute timeout
    public async Task Folder_DownloadContentsRecursively_Should_DownloadContentsRecursively()
    {
        // Arrange
        var projectId = _configuration["acc:integrationtest:projectId"];
        if (string.IsNullOrEmpty(projectId))
            throw new InvalidOperationException("Project ID not configured in user secrets");
        var apiClient = TwoLeggedApiClient
            .Configure()
            .WithClientId(_clientId)
            .AndClientSecret(_clientSecret)
            .ForAccount(_accountId)
            .WithOptions(options =>
            {
                options.RetryAttempts = 12;
                options.InitialRetryInSeconds = 2;
            })
            .Create();
        var rootBackupDirectory = Path.GetTempPath();
        var project = await apiClient.GetProject(projectId);
        var rootProjectDirectory = Path.Combine(rootBackupDirectory, Guid.NewGuid().ToString(), project.Name);
        await project.GetContentsRecursively();
        var sut = project.RootFolder;

        // Act
        await sut.DownloadContentsRecursively(rootProjectDirectory);

        // Assert
        sut.FilesRecursive.All(x => x.Downloaded).Should().BeTrue();
        sut.FilesRecursive.Count(x => x.Downloaded).Should().BeGreaterThanOrEqualTo(1);
        sut.SubfoldersRecursive.All(x => x.Created).Should().BeTrue();
        sut.SubfoldersRecursive.Count(x => x.Created).Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact(Timeout = 300000)] // 5 minute timeout
    public async Task Folder_DownloadContents_Should_DownloadContents()
    {
        // Arrange
        var projectId = _configuration["acc:integrationtest:projectId"];
        if (string.IsNullOrEmpty(projectId))
            throw new InvalidOperationException("Project ID not configured in user secrets");

        var apiClient = TwoLeggedApiClient
            .Configure()
            .WithClientId(_clientId)
            .AndClientSecret(_clientSecret)
            .ForAccount(_accountId)
            .WithOptions(options =>
            {
                options.RetryAttempts = 12;
                options.InitialRetryInSeconds = 2;
            })
            .Create();

        // Get the project and use its root folder
        var project = await apiClient.GetProject(projectId);
        await project.GetRootFolder();
        var sut = project.RootFolder!;

        var rootBackupDirectory = Path.GetTempPath();
        var sutFiles = sut.Files.Count;
        var downloadPath = Path.Combine(rootBackupDirectory, Guid.NewGuid().ToString(), sut.Name);

        // Act
        await sut.DownloadContents(downloadPath);

        // Assert
        sut.FilesRecursive.All(x => x.Downloaded).Should().BeTrue();
        sut.FilesRecursive.Count(x => x.Downloaded).Should().Be(sutFiles);
        sut.Created.Should().BeTrue();
    }
}