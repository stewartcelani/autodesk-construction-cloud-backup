using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using AutodeskConstructionCloud.ApiClient.Entities;
using Xunit;
using FluentAssertions;
using Library.Logger;
using File = AutodeskConstructionCloud.ApiClient.Entities.File;

// ReSharper disable AsyncVoidLambda

namespace AutodeskConstructionCloud.ApiClient.Tests;

public class ApiClientUnitTests
{
    [Fact]
    public void Building_WithoutOptions_Should_HaveAppropriateValues()
    {
        // Arrange
        const string clientId = "AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY";
        const string clientSecret = "wE3GFhuIsGJEi3d4";
        const string accountId = "f33e018a-d1f5-4ef3-ae67-606de6aeed87";

        // Act
        ApiClient sut = TwoLeggedApiClient
            .Configure()
            .WithClientId(clientId)
            .AndClientSecret(clientSecret)
            .ForAccount(accountId)
            .Create();

        // Assert
        sut.Should().BeOfType<ApiClient>();
        sut.Config.ClientId.Should().Be(clientId);
        sut.Config.ClientSecret.Should().Be(clientSecret);
        sut.Config.AccountId.Should().Be(accountId);
        sut.Config.Logger.Should().BeNull();
        sut.Config.RetryAttempts.Should().Be(4);
        sut.Config.InitialRetryInSeconds.Should().Be(2);
        sut.Config.HttpClient.BaseAddress.Should().BeNull();
        sut.Config.Logger.Should().BeNull();
    }

    [Fact]
    public void Building_WithoutOptions_Should_NotThrow()
    {
        // Arrange
        const string clientId = "AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY";
        const string clientSecret = "wE3GFhuIsGJEi3d4";
        const string accountId = "f33e018a-d1f5-4ef3-ae67-606de6aeed87";

        // Act
        Action act = () => TwoLeggedApiClient
            .Configure()
            .WithClientId(clientId)
            .AndClientSecret(clientSecret)
            .ForAccount(accountId)
            .Create();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Building_WithOptions_Should_HaveAppropriateValues()
    {
        // Arrange
        const string clientId = "AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY";
        const string clientSecret = "wE3GFhuIsGJEi3d4";
        const string accountId = "f33e018a-d1f5-4ef3-ae67-606de6aeed87";
        ILogger logger = new NLogLogger(new NLogLoggerConfiguration()
        {
            LogLevel = LogLevel.Fatal,
            LogToConsole = false
        });
        var httpClient = new HttpClient();
        var baseAddress = new Uri("https://www.test.com");
        httpClient.BaseAddress = baseAddress;

        // Act
        ApiClient sut = TwoLeggedApiClient
            .Configure()
            .WithClientId(clientId)
            .AndClientSecret(clientSecret)
            .ForAccount(accountId)
            .WithOptions(options =>
            {
                options.HttpClient = httpClient;
                options.Logger = logger;
                options.RetryAttempts = 20;
                options.InitialRetryInSeconds = 8;
            })
            .Create();

        // Assert
        sut.Should().BeOfType<ApiClient>();
        sut.Config.ClientId.Should().Be(clientId);
        sut.Config.ClientSecret.Should().Be(clientSecret);
        sut.Config.AccountId.Should().Be(accountId);
        sut.Config.HttpClient.BaseAddress.Should().Be(baseAddress);
        sut.Config.Logger.Should().BeOfType<NLogLogger>();
        sut.Config.Logger.Config.LogLevel.Should().Be(LogLevel.Fatal);
        sut.Config.Logger.Config.LogToConsole.Should().Be(false);
        sut.Config.RetryAttempts.Should().Be(20);
        sut.Config.InitialRetryInSeconds.Should().Be(8);
    }

    [Fact]
    public void Building_WithOptions_Should_NotThrow()
    {
        // Arrange
        const string clientId = "AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY";
        const string clientSecret = "wE3GFhuIsGJEi3d4";
        const string accountId = "f33e018a-d1f5-4ef3-ae67-606de6aeed87";
        ILogger logger = new NLogLogger(new NLogLoggerConfiguration()
        {
            LogLevel = LogLevel.Fatal,
            LogToConsole = false
        });
        var httpClient = new HttpClient();
        var baseAddress = new Uri("https://www.test.com");
        httpClient.BaseAddress = baseAddress;

        // Act
        Action act = () => TwoLeggedApiClient
            .Configure()
            .WithClientId(clientId)
            .AndClientSecret(clientSecret)
            .ForAccount(accountId)
            .WithOptions(options =>
            {
                options.HttpClient = httpClient;
                options.Logger = logger;
                options.RetryAttempts = 20;
                options.InitialRetryInSeconds = 8;
            })
            .Create();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public async Task GetAccessToken_InvalidClientId_Should_Throw_403Forbidden()
    {
        // Arrange
        const string clientId = "InvalidClientId";
        const string clientSecret = "wE3GFhuIsGJEi3d4";
        const string accountId = "f33e018a-d1f5-4ef3-ae67-606de6aeed87";
        const string forbiddenResponse =
            $@"{{ ""developerMessage"":""The client_id specified does not have access to the api product"", ""moreInfo"": ""https://forge.autodesk.com/en/docs/oauth/v2/developers_guide/error_handling/"", ""errorCode"": ""AUTH-001""}}";
        var messageHandlerMapping = new MockHttpMessageHandlerMapping()
        {
            RequestUri = new Uri("https://developer.api.autodesk.com/authentication/v1/authenticate"),
            Response = forbiddenResponse,
            StatusCode = HttpStatusCode.Forbidden
        };
        var messageHandler = new MockHttpMessageHandler(messageHandlerMapping);
        var httpClient = new HttpClient(messageHandler);

        ApiClient sut = TwoLeggedApiClient
            .Configure()
            .WithClientId(clientId)
            .AndClientSecret(clientSecret)
            .ForAccount(accountId)
            .WithOptions(options =>
            {
                options.RetryAttempts = 1;
                options.InitialRetryInSeconds = 1;
                options.HttpClient = httpClient;
            })
            .Create();

        // Act
        Func<Task> act = async () => await sut.EnsureAccessToken();

        // Assert
        await act
            .Should()
            .ThrowAsync<HttpRequestException>()
            .Where(e => e.StatusCode == HttpStatusCode.Forbidden && e.Message == forbiddenResponse);
        messageHandler.NumberOfCalls.Should().Be(2);
    }

    [Fact]
    public async Task EnsureAccessToken_WithValidCredentials_Should_SetHttpAuthenticationHeader()
    {
        // Arrange
        const string clientId = "AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY";
        const string clientSecret = "wE3GFhuIsGJEi3d4";
        const string accountId = "f33e018a-d1f5-4ef3-ae67-606de6aeed87";
        var messageHandlerMapping = new MockHttpMessageHandlerMapping()
        {
            RequestUri = new Uri("https://developer.api.autodesk.com/authentication/v1/authenticate"),
            Response = await new StreamReader(@"ExampleRestApiResponses\AuthenticateResponse.json").ReadToEndAsync(),
            StatusCode = HttpStatusCode.OK
        };
        var messageHandler = new MockHttpMessageHandler(messageHandlerMapping);
        var httpClient = new HttpClient(messageHandler);
        ApiClient sut = TwoLeggedApiClient
            .Configure()
            .WithClientId(clientId)
            .AndClientSecret(clientSecret)
            .ForAccount(accountId)
            .WithOptions(options =>
            {
                options.HttpClient = httpClient;
                options.RetryAttempts = 1;
                options.InitialRetryInSeconds = 1;
            })
            .Create();
        AuthenticationHeaderValue? initialAuthHeader = sut.Config.HttpClient.DefaultRequestHeaders.Authorization;

        // Act
        await sut.EnsureAccessToken();

        // Assert
        initialAuthHeader.Should().BeNull();
        sut.Config.HttpClient.DefaultRequestHeaders.Authorization.Should().NotBeNull();
        Assert.NotEqual(initialAuthHeader, sut.Config.HttpClient.DefaultRequestHeaders.Authorization);
        messageHandler.NumberOfCalls.Should().Be(1);
    }

    [Fact]
    public async Task GetAccessToken_InvalidClientSecret_Should_Throw_401Unauthorized()
    {
        // Arrange
        const string clientId = "AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY";
        const string clientSecret = "InvalidClientSecret";
        const string accountId = "f33e018a-d1f5-4ef3-ae67-606de6aeed87";
        const string unauthorizedResponse =
            $@"{{""developerMessage"":""The client_id (application key)/client_secret are not valid"",""errorCode"":""AUTH-003"",""more info"":""https://forge.autodesk.com/en/docs/oauth/v2/developers_guide/error_handling/""}}";
        var messageHandlerMapping = new MockHttpMessageHandlerMapping()
        {
            RequestUri = new Uri("https://developer.api.autodesk.com/authentication/v1/authenticate"),
            Response = unauthorizedResponse,
            StatusCode = HttpStatusCode.Unauthorized
        };
        var messageHandler = new MockHttpMessageHandler(messageHandlerMapping);
        var httpClient = new HttpClient(messageHandler);

        ApiClient sut = TwoLeggedApiClient
            .Configure()
            .WithClientId(clientId)
            .AndClientSecret(clientSecret)
            .ForAccount(accountId)
            .WithOptions(options =>
            {
                options.RetryAttempts = 1;
                options.InitialRetryInSeconds = 1;
                options.HttpClient = httpClient;
            })
            .Create();

        // Act
        Func<Task> act = async () => await sut.EnsureAccessToken();

        // Assert
        await act
            .Should()
            .ThrowAsync<HttpRequestException>()
            .Where(e => e.StatusCode == HttpStatusCode.Unauthorized && e.Message == unauthorizedResponse);
        messageHandler.NumberOfCalls.Should().Be(2);
    }

    [Fact]
    public async Task GetAccessToken_InternalServerError_Should_Throw_GenericHttpRequestException()
    {
        // Arrange
        const string clientId = "AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY";
        const string clientSecret = "InvalidClientSecret";
        const string accountId = "f33e018a-d1f5-4ef3-ae67-606de6aeed87";
        var messageHandlerMapping = new MockHttpMessageHandlerMapping()
        {
            RequestUri = new Uri("https://developer.api.autodesk.com/authentication/v1/authenticate"),
            Response = string.Empty,
            StatusCode = HttpStatusCode.InternalServerError
        };
        var messageHandler = new MockHttpMessageHandler(messageHandlerMapping);
        var httpClient = new HttpClient(messageHandler);

        ApiClient sut = TwoLeggedApiClient
            .Configure()
            .WithClientId(clientId)
            .AndClientSecret(clientSecret)
            .ForAccount(accountId)
            .WithOptions(options =>
            {
                options.RetryAttempts = 1;
                options.InitialRetryInSeconds = 1;
                options.HttpClient = httpClient;
            })
            .Create();

        // Act
        Func<Task> act = async () => await sut.EnsureAccessToken();

        // Assert
        await act
            .Should()
            .ThrowAsync<HttpRequestException>()
            .Where(e => e.StatusCode == HttpStatusCode.InternalServerError);
        messageHandler.NumberOfCalls.Should().Be(2);
    }

    [Fact]
    public async Task GetProjects_Should_ReturnOneProject()
    {
        // Arrange
        const string clientId = "AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY";
        const string clientSecret = "wE3GFhuIsGJEi3d4";
        const string accountId = "48a4d1eb-a370-42fe-89c9-4dd9e2ad9d41";
        var messageHandlerMappings = new List<MockHttpMessageHandlerMapping>
        {
            new()
            {
                RequestUri = new Uri("https://developer.api.autodesk.com/authentication/v1/authenticate"),
                Response =
                    await new StreamReader(@"ExampleRestApiResponses\AuthenticateResponse.json").ReadToEndAsync(),
                StatusCode = HttpStatusCode.OK
            },
            new()
            {
                RequestUri = new Uri($"https://developer.api.autodesk.com/project/v1/hubs/b.{accountId}/projects"),
                Response = await new StreamReader(@"ExampleRestApiResponses\ProjectsResponse.json").ReadToEndAsync(),
                StatusCode = HttpStatusCode.OK
            }
        };
        var messageHandler = new MockHttpMessageHandler(messageHandlerMappings);
        var httpClient = new HttpClient(messageHandler);
        ApiClient sut = TwoLeggedApiClient
            .Configure()
            .WithClientId(clientId)
            .AndClientSecret(clientSecret)
            .ForAccount(accountId)
            .WithOptions(options =>
            {
                options.HttpClient = httpClient;
                options.RetryAttempts = 1;
                options.InitialRetryInSeconds = 1;
            })
            .Create();

        // Act
        List<Project> projects = await sut.GetProjects();

        // Assert
        projects.Count.Should().Be(1);
        messageHandler.NumberOfCalls.Should().Be(2);
    }

    [Fact]
    public async Task GetProjects_InternalServerError_Should_Throw_GenericHttpRequestException()
    {
        // Arrange
        const string clientId = "AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY";
        const string clientSecret = "wE3GFhuIsGJEi3d4";
        const string accountId = "48a4d1eb-a370-42fe-89c9-4dd9e2ad9d41";
        var messageHandlerMappings = new List<MockHttpMessageHandlerMapping>
        {
            new()
            {
                RequestUri = new Uri("https://developer.api.autodesk.com/authentication/v1/authenticate"),
                Response =
                    await new StreamReader(@"ExampleRestApiResponses\AuthenticateResponse.json").ReadToEndAsync(),
                StatusCode = HttpStatusCode.OK
            },
            new()
            {
                RequestUri = new Uri($"https://developer.api.autodesk.com/project/v1/hubs/b.{accountId}/projects"),
                Response = string.Empty,
                StatusCode = HttpStatusCode.InternalServerError
            }
        };
        var messageHandler = new MockHttpMessageHandler(messageHandlerMappings);
        var httpClient = new HttpClient(messageHandler);
        ApiClient sut = TwoLeggedApiClient
            .Configure()
            .WithClientId(clientId)
            .AndClientSecret(clientSecret)
            .ForAccount(accountId)
            .WithOptions(options =>
            {
                options.HttpClient = httpClient;
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
            .Where(e => e.StatusCode == HttpStatusCode.InternalServerError);
        messageHandler.NumberOfCalls.Should().Be(3);
    }

    [Fact]
    public async Task GetProjects_WithPagination_Should_ReturnAppropriateProjects()
    {
        // Arrange
        const string clientId = "AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY";
        const string clientSecret = "wE3GFhuIsGJEi3d4";
        const string accountId = "48a4d1eb-a370-42fe-89c9-4dd9e2ad9d41";
        var messageHandlerMappings = new List<MockHttpMessageHandlerMapping>
        {
            new()
            {
                RequestUri = new Uri("https://developer.api.autodesk.com/authentication/v1/authenticate"),
                Response =
                    await new StreamReader(@"ExampleRestApiResponses\AuthenticateResponse.json").ReadToEndAsync(),
                StatusCode = HttpStatusCode.OK
            },
            new()
            {
                RequestUri = new Uri($"https://developer.api.autodesk.com/project/v1/hubs/b.{accountId}/projects"),
                Response = await new StreamReader(@"ExampleRestApiResponses\ProjectsResponseWithPagination.json")
                    .ReadToEndAsync(),
                StatusCode = HttpStatusCode.OK
            },
            new()
            {
                RequestUri =
                    new Uri(
                        $"https://developer.api.autodesk.com/project/v1/hubs/b.{accountId}/projects?page%5Bnumber%5D=2&page%5Blimit%5D=2"),
                Response = await new StreamReader(@"ExampleRestApiResponses\ProjectsResponse.json").ReadToEndAsync(),
                StatusCode = HttpStatusCode.OK
            }
        };
        var messageHandler = new MockHttpMessageHandler(messageHandlerMappings);
        var httpClient = new HttpClient(messageHandler);
        ApiClient sut = TwoLeggedApiClient
            .Configure()
            .WithClientId(clientId)
            .AndClientSecret(clientSecret)
            .ForAccount(accountId)
            .WithOptions(options =>
            {
                options.HttpClient = httpClient;
                options.RetryAttempts = 1;
                options.InitialRetryInSeconds = 1;
            })
            .Create();

        // Act
        List<Project> projects = await sut.GetProjects();

        // Assert
        projects.Count.Should().Be(2);
        messageHandler.NumberOfCalls.Should().Be(3);
    }

    [Fact]
    public async Task GetFolderContents_Should_ReturnFolderContents()
    {
        // Arrange
        const string clientId = "AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY";
        const string clientSecret = "wE3GFhuIsGJEi3d4";
        const string accountId = "48a4d1eb-a370-42fe-89c9-4dd9e2ad9d41";
        const string projectId = "0577ff54-1967-4c9b-80d4-eb649bd0774d";
        const string folderId = "urn:adsk.wipprod:fs.folder:co.tW57hcgcQxXkBrMu38Az1Z";
        string folderContentsEndpoint =
            $"https://developer.api.autodesk.com/data/v1/projects/b.{projectId}/folders/{folderId}/contents";
        var messageHandlerMappings = new List<MockHttpMessageHandlerMapping>
        {
            new()
            {
                RequestUri = new Uri("https://developer.api.autodesk.com/authentication/v1/authenticate"),
                Response =
                    await new StreamReader(@"ExampleRestApiResponses\AuthenticateResponse.json").ReadToEndAsync(),
                StatusCode = HttpStatusCode.OK
            },
            new()
            {
                RequestUri = new Uri(folderContentsEndpoint),
                Response =
                    await new StreamReader(@"ExampleRestApiResponses\FolderContentsResponse.json").ReadToEndAsync(),
                StatusCode = HttpStatusCode.OK
            }
        };
        var messageHandler = new MockHttpMessageHandler(messageHandlerMappings);
        var httpClient = new HttpClient(messageHandler);
        ApiClient sut = TwoLeggedApiClient
            .Configure()
            .WithClientId(clientId)
            .AndClientSecret(clientSecret)
            .ForAccount(accountId)
            .WithOptions(options =>
            {
                options.HttpClient = httpClient;
                options.RetryAttempts = 1;
                options.InitialRetryInSeconds = 1;
            })
            .Create();

        // Act
        (List<Folder> folders, List<File> files) = await sut.GetFolderContents(projectId, folderId);

        // Assert
        folders.Count.Should().Be(1);
        files.Count.Should().Be(1);
        messageHandler.NumberOfCalls.Should().Be(2);
    }
    
    [Fact]
    public async Task GetFolderContentsFor_Should_PopulateFolderWithContents()
    {
        // Arrange
        const string clientId = "AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY";
        const string clientSecret = "wE3GFhuIsGJEi3d4";
        const string accountId = "48a4d1eb-a370-42fe-89c9-4dd9e2ad9d41";
        const string projectId = "0577ff54-1967-4c9b-80d4-eb649bd0774d";
        const string parentFolderId = "urn:adsk.wipprod:fs.folder:co.DerBocbkXrcYsz43uJLTkW";
        const string folderId = "urn:adsk.wipprod:fs.folder:co.tW57hcgcQxXkBrMu38Az1Z";
        string folderEndpoint =
            $"https://developer.api.autodesk.com/data/v1/projects/b.{projectId}/folders/{folderId}";
        string folderContentsEndpoint =
            $"https://developer.api.autodesk.com/data/v1/projects/b.{projectId}/folders/{folderId}/contents";
        var messageHandlerMappings = new List<MockHttpMessageHandlerMapping>
        {
            new()
            {
                RequestUri = new Uri("https://developer.api.autodesk.com/authentication/v1/authenticate"),
                Response =
                    await new StreamReader(@"ExampleRestApiResponses\AuthenticateResponse.json").ReadToEndAsync(),
                StatusCode = HttpStatusCode.OK
            },
            new()
            {
                RequestUri = new Uri(folderEndpoint),
                Response =
                    await new StreamReader(@"ExampleRestApiResponses\FolderResponse.json").ReadToEndAsync(),
                StatusCode = HttpStatusCode.OK
            },
            new()
            {
                RequestUri = new Uri(folderContentsEndpoint),
                Response =
                    await new StreamReader(@"ExampleRestApiResponses\FolderContentsResponse.json").ReadToEndAsync(),
                StatusCode = HttpStatusCode.OK
            }
        };
        var messageHandler = new MockHttpMessageHandler(messageHandlerMappings);
        var httpClient = new HttpClient(messageHandler);
        ApiClient sut = TwoLeggedApiClient
            .Configure()
            .WithClientId(clientId)
            .AndClientSecret(clientSecret)
            .ForAccount(accountId)
            .WithOptions(options =>
            {
                options.HttpClient = httpClient;
                options.RetryAttempts = 1;
                options.InitialRetryInSeconds = 1;
            })
            .Create();
        Folder folder = await sut.GetFolder(projectId, folderId);
        int initialFolderCount = folder.Folders.Count;
        int initialFilesCount = folder.Files.Count;

        // Act
        await sut.GetFolderContentsFor(folder);

        // Assert
        folder.ProjectId.Should().Be(projectId);
        folder.FolderId.Should().Be(folderId);
        folder.ParentFolderId.Should().Be(parentFolderId);
        folder.IsRootFolder.Should().BeFalse();
        initialFilesCount.Should().Be(0);
        folder.Files.Count.Should().Be(1);
        initialFolderCount.Should().Be(0);
        folder.Folders.Count.Should().Be(1);
        folder.Files.All(x => x.FolderId == folderId).Should().BeTrue();
        folder.Files.All(x => x.ProjectId == projectId).Should().BeTrue();
        folder.Folders.All(x => x.ParentFolderId == folderId).Should().BeTrue();
        folder.Folders.All(x => x.ProjectId == projectId).Should().BeTrue();
        messageHandler.NumberOfCalls.Should().Be(3);
    }

    [Fact]
    public async Task GetFolder_Should_ReturnFolder()
    {
        // Arrange
        const string clientId = "AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY";
        const string clientSecret = "wE3GFhuIsGJEi3d4";
        const string accountId = "48a4d1eb-a370-42fe-89c9-4dd9e2ad9d41";
        const string projectId = "0577ff54-1967-4c9b-80d4-eb649bd0774d";
        const string parentFolderId = "urn:adsk.wipprod:fs.folder:co.DerBocbkXrcYsz43uJLTkW";
        const string folderId = "urn:adsk.wipprod:fs.folder:co.tW57hcgcQxXkBrMu38Az1Z";
        string folderEndpoint =
            $"https://developer.api.autodesk.com/data/v1/projects/b.{projectId}/folders/{folderId}";
        var messageHandlerMappings = new List<MockHttpMessageHandlerMapping>
        {
            new()
            {
                RequestUri = new Uri("https://developer.api.autodesk.com/authentication/v1/authenticate"),
                Response =
                    await new StreamReader(@"ExampleRestApiResponses\AuthenticateResponse.json").ReadToEndAsync(),
                StatusCode = HttpStatusCode.OK
            },
            new()
            {
                RequestUri = new Uri(folderEndpoint),
                Response = await new StreamReader(@"ExampleRestApiResponses\FolderResponse.json").ReadToEndAsync(),
                StatusCode = HttpStatusCode.OK
            }
        };
        var messageHandler = new MockHttpMessageHandler(messageHandlerMappings);
        var httpClient = new HttpClient(messageHandler);
        ApiClient sut = TwoLeggedApiClient
            .Configure()
            .WithClientId(clientId)
            .AndClientSecret(clientSecret)
            .ForAccount(accountId)
            .WithOptions(options =>
            {
                options.HttpClient = httpClient;
                options.RetryAttempts = 1;
                options.InitialRetryInSeconds = 1;
            })
            .Create();

        // Act
        Folder folder = await sut.GetFolder(projectId, folderId);

        // Assert
        folder.ProjectId.Should().Be(projectId);
        folder.FolderId.Should().Be(folderId);
        folder.ParentFolderId.Should().Be(parentFolderId);
        folder.IsRootFolder.Should().BeFalse();
        folder.Files.Count.Should().Be(0);
        messageHandler.NumberOfCalls.Should().Be(2);
    }

    [Fact]
    public async Task GetFolder_RootFolder_Should_ReturnFolder()
    {
        // Arrange
        const string clientId = "AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY";
        const string clientSecret = "wE3GFhuIsGJEi3d4";
        const string accountId = "48a4d1eb-a370-42fe-89c9-4dd9e2ad9d41";
        const string projectId = "0577ff54-1967-4c9b-80d4-eb649bd0774d";
        const string folderId = "urn:adsk.wipprod:fs.folder:co.DerBocbkXrcYsz43uJLTkW";
        string folderEndpoint =
            $"https://developer.api.autodesk.com/data/v1/projects/b.{projectId}/folders/{folderId}";
        var messageHandlerMappings = new List<MockHttpMessageHandlerMapping>
        {
            new()
            {
                RequestUri = new Uri("https://developer.api.autodesk.com/authentication/v1/authenticate"),
                Response =
                    await new StreamReader(@"ExampleRestApiResponses\AuthenticateResponse.json").ReadToEndAsync(),
                StatusCode = HttpStatusCode.OK
            },
            new()
            {
                RequestUri = new Uri(folderEndpoint),
                Response = await new StreamReader(@"ExampleRestApiResponses\FolderResponseRootFolder.json")
                    .ReadToEndAsync(),
                StatusCode = HttpStatusCode.OK
            }
        };
        var messageHandler = new MockHttpMessageHandler(messageHandlerMappings);
        var httpClient = new HttpClient(messageHandler);
        ApiClient sut = TwoLeggedApiClient
            .Configure()
            .WithClientId(clientId)
            .AndClientSecret(clientSecret)
            .ForAccount(accountId)
            .WithOptions(options =>
            {
                options.HttpClient = httpClient;
                options.RetryAttempts = 1;
                options.InitialRetryInSeconds = 1;
            })
            .Create();

        // Act
        Folder rootFolder = await sut.GetFolder(projectId, folderId);

        // Assert
        rootFolder.ProjectId.Should().Be(projectId);
        rootFolder.FolderId.Should().Be(folderId);
        rootFolder.IsRootFolder.Should().BeTrue();
        rootFolder.Files.Count.Should().Be(0);
        messageHandler.NumberOfCalls.Should().Be(2);
    }

    [Fact]
    public async Task GetFolderContents_WithPagination_Should_ReturnFolderContents()
    {
        // Arrange
        const string clientId = "AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY";
        const string clientSecret = "wE3GFhuIsGJEi3d4";
        const string accountId = "48a4d1eb-a370-42fe-89c9-4dd9e2ad9d41";
        const string projectId = "0577ff54-1967-4c9b-80d4-eb649bd0774d";
        const string folderId = "urn:adsk.wipprod:fs.folder:co.tW57hcgcQxXkBrMu38Az1Z";
        string folderContentsEndpoint =
            $"https://developer.api.autodesk.com/data/v1/projects/b.{projectId}/folders/{folderId}/contents";
        var messageHandlerMappings = new List<MockHttpMessageHandlerMapping>
        {
            new()
            {
                RequestUri = new Uri("https://developer.api.autodesk.com/authentication/v1/authenticate"),
                Response =
                    await new StreamReader(@"ExampleRestApiResponses\AuthenticateResponse.json").ReadToEndAsync(),
                StatusCode = HttpStatusCode.OK
            },
            new()
            {
                RequestUri = new Uri(folderContentsEndpoint),
                Response = await new StreamReader(@"ExampleRestApiResponses\FolderContentsResponseWithPagination.json")
                    .ReadToEndAsync(),
                StatusCode = HttpStatusCode.OK
            },
            new()
            {
                RequestUri = new Uri($"{folderContentsEndpoint}?page%5Bnumber%5D=3"),
                Response =
                    await new StreamReader(@"ExampleRestApiResponses\FolderContentsResponse.json").ReadToEndAsync(),
                StatusCode = HttpStatusCode.OK
            }
        };
        var messageHandler = new MockHttpMessageHandler(messageHandlerMappings);
        var httpClient = new HttpClient(messageHandler);
        ApiClient sut = TwoLeggedApiClient
            .Configure()
            .WithClientId(clientId)
            .AndClientSecret(clientSecret)
            .ForAccount(accountId)
            .WithOptions(options =>
            {
                options.HttpClient = httpClient;
                options.RetryAttempts = 1;
                options.InitialRetryInSeconds = 1;
            })
            .Create();

        // Act
        (List<Folder> folders, List<File> files) = await sut.GetFolderContents(projectId, folderId);

        // Assert
        folders.Count.Should().Be(2);
        files.Count.Should().Be(2);
        messageHandler.NumberOfCalls.Should().Be(3);
    }

    [Fact]
    public async Task GetFolderWithContents_Should_ReturnFolderWithContents()
    {
        // Arrange
        const string clientId = "AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY";
        const string clientSecret = "wE3GFhuIsGJEi3d4";
        const string accountId = "48a4d1eb-a370-42fe-89c9-4dd9e2ad9d41";
        const string projectId = "0577ff54-1967-4c9b-80d4-eb649bd0774d";
        const string folderId = "urn:adsk.wipprod:fs.folder:co.tW57hcgcQxXkBrMu38Az1Z";
        const string parentFolderId = "urn:adsk.wipprod:fs.folder:co.DerBocbkXrcYsz43uJLTkW";
        var folderEndpoint =
            $"https://developer.api.autodesk.com/data/v1/projects/b.{projectId}/folders/{folderId}";
        var folderContentsEndpoint = $"{folderEndpoint}/contents";
        var messageHandlerMappings = new List<MockHttpMessageHandlerMapping>
        {
            new()
            {
                RequestUri = new Uri("https://developer.api.autodesk.com/authentication/v1/authenticate"),
                Response =
                    await new StreamReader(@"ExampleRestApiResponses\AuthenticateResponse.json").ReadToEndAsync(),
                StatusCode = HttpStatusCode.OK
            },
            new()
            {
                RequestUri = new Uri(folderEndpoint),
                Response = await new StreamReader(@"ExampleRestApiResponses\FolderResponse.json").ReadToEndAsync(),
                StatusCode = HttpStatusCode.OK
            },
            new()
            {
                RequestUri = new Uri(folderContentsEndpoint),
                Response =
                    await new StreamReader(@"ExampleRestApiResponses\FolderContentsResponse.json").ReadToEndAsync(),
                StatusCode = HttpStatusCode.OK
            }
        };
        var messageHandler = new MockHttpMessageHandler(messageHandlerMappings);
        var httpClient = new HttpClient(messageHandler);
        ApiClient sut = TwoLeggedApiClient
            .Configure()
            .WithClientId(clientId)
            .AndClientSecret(clientSecret)
            .ForAccount(accountId)
            .WithOptions(options =>
            {
                options.HttpClient = httpClient;
                options.RetryAttempts = 1;
                options.InitialRetryInSeconds = 1;
            })
            .Create();

        // Act
        Folder folder = await sut.GetFolderWithContents(projectId, folderId);

        // Assert
        folder.ProjectId.Should().Be(projectId);
        folder.FolderId.Should().Be(folderId);
        folder.ParentFolderId.Should().Be(parentFolderId);
        folder.IsRootFolder.Should().BeFalse();
        folder.Files.Count.Should().Be(1);
        folder.Files.All(x => x.FolderId == folderId).Should().BeTrue();
        folder.Files.All(x => x.ProjectId == projectId).Should().BeTrue();
        folder.Folders.Count.Should().Be(1);
        folder.Folders.All(x => x.ParentFolderId == folderId).Should().BeTrue();
        folder.Folders.All(x => x.ProjectId == projectId).Should().BeTrue();
        messageHandler.NumberOfCalls.Should().Be(3);
    }

    [Fact]
    public async Task GetFolderWithContents_WithPagination_Should_ReturnFolderWithContents()
    {
        // Arrange
        const string clientId = "AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY";
        const string clientSecret = "wE3GFhuIsGJEi3d4";
        const string accountId = "48a4d1eb-a370-42fe-89c9-4dd9e2ad9d41";
        const string projectId = "0577ff54-1967-4c9b-80d4-eb649bd0774d";
        const string folderId = "urn:adsk.wipprod:fs.folder:co.tW57hcgcQxXkBrMu38Az1Z";
        const string parentFolderId = "urn:adsk.wipprod:fs.folder:co.DerBocbkXrcYsz43uJLTkW";
        var folderEndpoint =
            $"https://developer.api.autodesk.com/data/v1/projects/b.{projectId}/folders/{folderId}";
        var folderContentsEndpoint = $"{folderEndpoint}/contents";
        var messageHandlerMappings = new List<MockHttpMessageHandlerMapping>
        {
            new()
            {
                RequestUri = new Uri("https://developer.api.autodesk.com/authentication/v1/authenticate"),
                Response =
                    await new StreamReader(@"ExampleRestApiResponses\AuthenticateResponse.json").ReadToEndAsync(),
                StatusCode = HttpStatusCode.OK
            },
            new()
            {
                RequestUri = new Uri(folderEndpoint),
                Response = await new StreamReader(@"ExampleRestApiResponses\FolderResponse.json").ReadToEndAsync(),
                StatusCode = HttpStatusCode.OK
            },
            new()
            {
                RequestUri = new Uri(folderContentsEndpoint),
                Response = await new StreamReader(@"ExampleRestApiResponses\FolderContentsResponseWithPagination.json")
                    .ReadToEndAsync(),
                StatusCode = HttpStatusCode.OK
            },
            new()
            {
                RequestUri = new Uri($"{folderContentsEndpoint}?page%5Bnumber%5D=3"),
                Response =
                    await new StreamReader(@"ExampleRestApiResponses\FolderContentsResponse.json").ReadToEndAsync(),
                StatusCode = HttpStatusCode.OK
            }
        };
        var messageHandler = new MockHttpMessageHandler(messageHandlerMappings);
        var httpClient = new HttpClient(messageHandler);
        ApiClient sut = TwoLeggedApiClient
            .Configure()
            .WithClientId(clientId)
            .AndClientSecret(clientSecret)
            .ForAccount(accountId)
            .WithOptions(options =>
            {
                options.HttpClient = httpClient;
                options.RetryAttempts = 1;
                options.InitialRetryInSeconds = 1;
            })
            .Create();

        // Act
        Folder folder = await sut.GetFolderWithContents(projectId, folderId);

        // Assert
        folder.ProjectId.Should().Be(projectId);
        folder.FolderId.Should().Be(folderId);
        folder.ParentFolderId.Should().Be(parentFolderId);
        folder.IsRootFolder.Should().BeFalse();
        folder.Files.Count.Should().Be(2);
        folder.Files.All(x => x.FolderId == folderId).Should().BeTrue();
        folder.Files.All(x => x.ProjectId == projectId).Should().BeTrue();
        folder.Folders.Count.Should().Be(2);
        folder.Folders.All(x => x.ParentFolderId == folderId).Should().BeTrue();
        folder.Folders.All(x => x.ProjectId == projectId).Should().BeTrue();
        messageHandler.NumberOfCalls.Should().Be(4);
    }
}