using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using ACC.ApiClient.Entities;
using FluentAssertions;
using Library.SecretsManager;
using Xunit;

// ReSharper disable AsyncVoidLambda


namespace ACC.ApiClient.IntegrationTests;

public class ApiClientIntegrationTests
{
    private readonly string _accountId;
    private readonly string _clientId;
    private readonly string _clientSecret;

    public ApiClientIntegrationTests()
    {
        _clientId = SecretsManager.GetEnvironmentVariable("acc:clientid");
        _clientSecret = SecretsManager.GetEnvironmentVariable("acc:clientsecret");
        _accountId = SecretsManager.GetEnvironmentVariable("acc:accountid");
    }

    [Fact]
    public async Task GetAccessToken_InvalidClientId_Should_Throw_403Forbidden()
    {
        // Arrange
        const string clientId = "AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY";
        const string clientSecret = "wE3GFhuIsGJEi3d4";
        const string accountId = "f33e018a-d1f5-4ef3-ae67-606de6aeed87";
        const string forbiddenResponse =
            @"{ ""developerMessage"":""The client_id specified does not have access to the api product"", ""moreInfo"": ""https://forge.autodesk.com/en/docs/oauth/v2/developers_guide/error_handling/"", ""errorCode"": ""AUTH-001""}";
        ApiClient sut = TwoLeggedApiClient
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
            .Where(e => e.StatusCode == HttpStatusCode.Forbidden && e.Message == forbiddenResponse);
    }

    [Fact]
    public async Task GetAccessToken_InvalidClientSecret_Should_Throw_401Unauthorized()
    {
        // Arrange
        string clientId = _clientId;
        const string clientSecret = "InvalidClientSecret";
        const string accountId = "IrrelevantToTest";
        const string unauthorizedResponse =
            @"{""developerMessage"":""The client_id (application key)/client_secret are not valid"",""errorCode"":""AUTH-003"",""more info"":""https://forge.autodesk.com/en/docs/oauth/v2/developers_guide/error_handling/""}";

        ApiClient sut = TwoLeggedApiClient
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
            .Where(e => e.StatusCode == HttpStatusCode.Unauthorized && e.Message == unauthorizedResponse);
    }

    [Fact]
    public async Task EnsureAccessToken_WithValidCredentials_Should_SetHttpAuthenticationHeader()
    {
        // Arrange
        ApiClient sut = TwoLeggedApiClient
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
        AuthenticationHeaderValue? initialAuthHeader = sut.Config.HttpClient.DefaultRequestHeaders.Authorization;

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
        ApiClient sut = TwoLeggedApiClient
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
        List<Project> projects = await sut.GetProjects();

        // Assert
        projects.Count.Should().BeGreaterOrEqualTo(1);
        projects.All(x => x.RootFolder == null).Should().BeTrue();
    }

    [Fact]
    public async Task GetProjects_getRootFolderContents_True_Should_ReturnOneOrMoreProjects()
    {
        // Arrange
        ApiClient sut = TwoLeggedApiClient
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
        List<Project> projects = await sut.GetProjects(true);

        // Assert
        projects.Count.Should().BeGreaterOrEqualTo(1);
        projects.All(x => x.RootFolder != null).Should().BeTrue();
    }

    [Fact]
    public async Task Project_DownloadContentsRecursively_Should_DownloadContentsRecursively()
    {
        // Arrange
        string projectId = SecretsManager.GetEnvironmentVariable(
            "acc:integrationtest:Project_DownloadContentsRecursively_Should_DownloadContentsRecursively:projectId");
        ApiClient apiClient = TwoLeggedApiClient
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
        string rootBackupDirectory = Path.GetTempPath();
        Project sut = await apiClient.GetProject(projectId);
        string rootProjectDirectory = Path.Combine(rootBackupDirectory, Guid.NewGuid().ToString(), sut.Name);
        await sut.GetContentsRecursively();

        // Act
        await sut.DownloadContentsRecursively(rootProjectDirectory);

        // Assert
        sut.FilesRecursive.All(x => x.Downloaded).Should().BeTrue();
        sut.FilesRecursive.Count(x => x.Downloaded).Should().BeGreaterOrEqualTo(1);
        sut.SubfoldersRecursive.All(x => x.Created).Should().BeTrue();
        sut.SubfoldersRecursive.Count(x => x.Created).Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task Folder_DownloadContentsRecursively_Should_DownloadContentsRecursively()
    {
        // Arrange
        string projectId = SecretsManager.GetEnvironmentVariable(
            "acc:integrationtest:Project_DownloadContentsRecursively_Should_DownloadContentsRecursively:projectId");
        ApiClient apiClient = TwoLeggedApiClient
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
        string rootBackupDirectory = Path.GetTempPath();
        Project project = await apiClient.GetProject(projectId);
        string rootProjectDirectory = Path.Combine(rootBackupDirectory, Guid.NewGuid().ToString(), project.Name);
        await project.GetContentsRecursively();
        Folder sut = project.RootFolder;

        // Act
        await sut.DownloadContentsRecursively(rootProjectDirectory);

        // Assert
        sut.FilesRecursive.All(x => x.Downloaded).Should().BeTrue();
        sut.FilesRecursive.Count(x => x.Downloaded).Should().BeGreaterOrEqualTo(1);
        sut.SubfoldersRecursive.All(x => x.Created).Should().BeTrue();
        sut.SubfoldersRecursive.Count(x => x.Created).Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task Folder_DownloadContents_Should_DownloadContents()
    {
        // Arrange
        string projectId = SecretsManager.GetEnvironmentVariable(
            "acc:integrationtest:Project_DownloadContentsRecursively_Should_DownloadContentsRecursively:projectId");
        string folderId =
            SecretsManager.GetEnvironmentVariable(
                "acc:integrationtest:Folder_DownloadContents_Should_DownloadContents:folderId");
        ApiClient apiClient = TwoLeggedApiClient
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
        string rootBackupDirectory = Path.GetTempPath();
        Folder sut = await apiClient.GetFolder(projectId, folderId, true);
        int sutFiles = sut.Files.Count;
        string downloadPath = Path.Combine(rootBackupDirectory, Guid.NewGuid().ToString(), sut.Name);

        // Act
        await sut.DownloadContents(downloadPath);

        // Assert
        sut.FilesRecursive.All(x => x.Downloaded).Should().BeTrue();
        sut.FilesRecursive.Count(x => x.Downloaded).Should().Be(sutFiles);
        sut.Created.Should().BeTrue();
    }
}