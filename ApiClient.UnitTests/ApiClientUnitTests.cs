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
using Library.Logger;
using Library.Testing;
using Xunit;
using File = ACC.ApiClient.Entities.File;

// ReSharper disable AsyncVoidLambda

namespace ACC.ApiClient.UnitTests;

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
        sut.Config.MaxDegreeOfParallelism.Should().Be(8);
        sut.Config.RetryAttempts.Should().Be(15);
        sut.Config.InitialRetryInSeconds.Should().Be(2);
        sut.Config.HttpClient.BaseAddress.Should().BeNull();
        sut.Config.DryRun.Should().BeFalse();
        sut.Config.Logger.Should().BeNull();
    }

    [Fact]
    public void Building_WithOptions_Should_HaveAppropriateValues()
    {
        // Arrange
        const string clientId = "AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY";
        const string clientSecret = "wE3GFhuIsGJEi3d4";
        const string accountId = "f33e018a-d1f5-4ef3-ae67-606de6aeed87";
        ILogger logger = new NLogLogger(new NLogLoggerConfiguration
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
                options.HubId = accountId;
                options.HttpClient = httpClient;
                options.Logger = logger;
                options.RetryAttempts = 20;
                options.InitialRetryInSeconds = 8;
                options.MaxDegreeOfParallelism = 4;
                options.DryRun = true;
            })
            .Create();

        // Assert
        sut.Should().BeOfType<ApiClient>();
        sut.Config.ClientId.Should().Be(clientId);
        sut.Config.ClientSecret.Should().Be(clientSecret);
        sut.Config.AccountId.Should().Be(accountId);
        sut.Config.HubId.Should().Be(accountId);
        sut.Config.HttpClient.BaseAddress.Should().Be(baseAddress);
        sut.Config.Logger.Should().BeOfType<NLogLogger>();
        sut.Config.Logger.Config.LogLevel.Should().Be(LogLevel.Fatal);
        sut.Config.Logger.Config.LogToConsole.Should().Be(false);
        sut.Config.RetryAttempts.Should().Be(20);
        sut.Config.DryRun.Should().BeTrue();
        sut.Config.InitialRetryInSeconds.Should().Be(8);
        sut.Config.MaxDegreeOfParallelism.Should().Be(4);
    }

    [Fact]
    public async Task GetAccessToken_InvalidClientId_Should_Throw_403Forbidden()
    {
        // Arrange
        const string clientId = "InvalidClientId";
        const string clientSecret = "wE3GFhuIsGJEi3d4";
        const string accountId = "f33e018a-d1f5-4ef3-ae67-606de6aeed87";
        const string forbiddenResponse =
            @"{ ""developerMessage"":""The client_id specified does not have access to the api product"", ""moreInfo"": ""https://forge.autodesk.com/en/docs/oauth/v2/developers_guide/error_handling/"", ""errorCode"": ""AUTH-001""}";
        var messageHandlerMapping = new MockHttpMessageHandlerMapping
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
        Func<Task> act = async () => await sut.GetProjects();

        // Assert
        await act
            .Should()
            .ThrowAsync<HttpRequestException>()
            .Where(e => e.StatusCode == HttpStatusCode.Forbidden && e.Message == forbiddenResponse);
        messageHandler.NumberOfCalls.Should().Be(4);
    }

    [Fact]
    public async Task EnsureAccessToken_WithValidCredentials_Should_SetHttpAuthenticationHeader()
    {
        // Arrange
        const string clientId = "AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY";
        const string clientSecret = "wE3GFhuIsGJEi3d4";
        const string accountId = "f33e018a-d1f5-4ef3-ae67-606de6aeed87";
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
        AuthenticationHeaderValue? initialAuthHeader = sut.Config.HttpClient.DefaultRequestHeaders.Authorization;

        // Act
        await sut.GetProjects();

        // Assert
        initialAuthHeader.Should().BeNull();
        sut.Config.HttpClient.DefaultRequestHeaders.Authorization.Should().NotBeNull();
        Assert.NotEqual(initialAuthHeader, sut.Config.HttpClient.DefaultRequestHeaders.Authorization);
        messageHandler.NumberOfCalls.Should().Be(2);
    }

    [Fact]
    public async Task GetAccessToken_InvalidClientSecret_Should_Throw_401Unauthorized()
    {
        // Arrange
        const string clientId = "AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY";
        const string clientSecret = "InvalidClientSecret";
        const string accountId = "f33e018a-d1f5-4ef3-ae67-606de6aeed87";
        const string unauthorizedResponse =
            @"{""developerMessage"":""The client_id (application key)/client_secret are not valid"",""errorCode"":""AUTH-003"",""more info"":""https://forge.autodesk.com/en/docs/oauth/v2/developers_guide/error_handling/""}";
        var messageHandlerMapping = new MockHttpMessageHandlerMapping
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
        Func<Task> act = async () => await sut.GetProjects();

        // Assert
        await act
            .Should()
            .ThrowAsync<HttpRequestException>()
            .Where(e => e.StatusCode == HttpStatusCode.Unauthorized && e.Message == unauthorizedResponse);
        messageHandler.NumberOfCalls.Should().Be(4);
    }

    [Fact]
    public async Task GetAccessToken_InternalServerError_Should_Throw_GenericHttpRequestException()
    {
        // Arrange
        const string clientId = "AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY";
        const string clientSecret = "InvalidClientSecret";
        const string accountId = "f33e018a-d1f5-4ef3-ae67-606de6aeed87";
        var messageHandlerMapping = new MockHttpMessageHandlerMapping
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
        Func<Task> act = async () => await sut.GetProjects();

        // Assert
        await act
            .Should()
            .ThrowAsync<HttpRequestException>()
            .Where(e => e.StatusCode == HttpStatusCode.InternalServerError);
        messageHandler.NumberOfCalls.Should().Be(4);
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
    public async Task GetFolderContents_Should_PopulateFolderWithContents()
    {
        // Arrange
        const string clientId = "AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY";
        const string clientSecret = "wE3GFhuIsGJEi3d4";
        const string accountId = "48a4d1eb-a370-42fe-89c9-4dd9e2ad9d41";
        const string projectId = "b.0577ff54-1967-4c9b-80d4-eb649bd0774d";
        const string parentFolderId = "urn:adsk.wipprod:fs.folder:co.DerBocbkXrcYsz43uJLTkW";
        const string folderId = "urn:adsk.wipprod:fs.folder:co.tW57hcgcQxXkBrMu38Az1Z";
        var folderEndpoint =
            $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/folders/{folderId}";
        var folderContentsEndpoint =
            $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/folders/{folderId}/contents";
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
        int initialFolderCount = folder.Subfolders.Count;
        int initialFilesCount = folder.Files.Count;

        // Act
        await sut.GetFolderContents(folder);

        // Assert
        folder.ProjectId.Should().Be(projectId);
        folder.FolderId.Should().Be(folderId);
        folder.ParentFolderId.Should().Be(parentFolderId);
        folder.IsRootFolder.Should().BeFalse();
        initialFilesCount.Should().Be(0);
        folder.Files.Count.Should().Be(1);
        initialFolderCount.Should().Be(0);
        folder.Subfolders.Count.Should().Be(1);
        folder.Files.All(x => x.ParentFolder.FolderId == folderId).Should().BeTrue();
        folder.Files.All(x => x.ProjectId == projectId).Should().BeTrue();
        folder.Subfolders.All(x => x.ParentFolderId == folderId).Should().BeTrue();
        folder.Subfolders.All(x => x.ProjectId == projectId).Should().BeTrue();
        messageHandler.NumberOfCalls.Should().Be(3);
    }

    [Fact]
    public async Task GetFolder_Should_ReturnFolder()
    {
        // Arrange
        const string clientId = "AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY";
        const string clientSecret = "wE3GFhuIsGJEi3d4";
        const string accountId = "48a4d1eb-a370-42fe-89c9-4dd9e2ad9d41";
        const string projectId = "b.0577ff54-1967-4c9b-80d4-eb649bd0774d";
        const string parentFolderId = "urn:adsk.wipprod:fs.folder:co.DerBocbkXrcYsz43uJLTkW";
        const string folderId = "urn:adsk.wipprod:fs.folder:co.tW57hcgcQxXkBrMu38Az1Z";
        var folderEndpoint =
            $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/folders/{folderId}";
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
        const string projectId = "b.0577ff54-1967-4c9b-80d4-eb649bd0774d";
        const string folderId = "urn:adsk.wipprod:fs.folder:co.DerBocbkXrcYsz43uJLTkW";
        var folderEndpoint =
            $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/folders/{folderId}";
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
        const string projectId = "b.0577ff54-1967-4c9b-80d4-eb649bd0774d";
        const string folderId = "urn:adsk.wipprod:fs.folder:co.tW57hcgcQxXkBrMu38Az1Z";
        var folderEndpoint =
            $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/folders/{folderId}";
        var folderContentsEndpoint =
            $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/folders/{folderId}/contents";
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
        Folder folder = await sut.GetFolder(projectId, folderId);

        // Act
        await sut.GetFolderContents(folder);

        // Assert
        folder.Subfolders.Count.Should().Be(2);
        folder.Files.Count.Should().Be(2);
        messageHandler.NumberOfCalls.Should().Be(4);
    }


    [Fact]
    public async Task GetFolderWithContents_Should_ReturnFolderWithContents()
    {
        // Arrange
        const string clientId = "AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY";
        const string clientSecret = "wE3GFhuIsGJEi3d4";
        const string accountId = "48a4d1eb-a370-42fe-89c9-4dd9e2ad9d41";
        const string projectId = "b.0577ff54-1967-4c9b-80d4-eb649bd0774d";
        const string folderId = "urn:adsk.wipprod:fs.folder:co.tW57hcgcQxXkBrMu38Az1Z";
        const string parentFolderId = "urn:adsk.wipprod:fs.folder:co.DerBocbkXrcYsz43uJLTkW";
        var folderEndpoint =
            $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/folders/{folderId}";
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
        Folder folder = await sut.GetFolder(projectId, folderId, true);

        // Assert
        folder.ProjectId.Should().Be(projectId);
        folder.FolderId.Should().Be(folderId);
        folder.ParentFolderId.Should().Be(parentFolderId);
        folder.IsRootFolder.Should().BeFalse();
        folder.Files.Count.Should().Be(1);
        folder.Files.All(x => x.ParentFolder.FolderId == folderId).Should().BeTrue();
        folder.Files.All(x => x.ProjectId == projectId).Should().BeTrue();
        folder.Subfolders.Count.Should().Be(1);
        folder.Subfolders.All(x => x.ParentFolderId == folderId).Should().BeTrue();
        folder.Subfolders.All(x => x.ProjectId == projectId).Should().BeTrue();
        messageHandler.NumberOfCalls.Should().Be(3);
    }

    [Fact]
    public async Task GetFolderWithContents_WithPagination_Should_ReturnFolderWithContents()
    {
        // Arrange
        const string clientId = "AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY";
        const string clientSecret = "wE3GFhuIsGJEi3d4";
        const string accountId = "48a4d1eb-a370-42fe-89c9-4dd9e2ad9d41";
        const string projectId = "b.0577ff54-1967-4c9b-80d4-eb649bd0774d";
        const string folderId = "urn:adsk.wipprod:fs.folder:co.tW57hcgcQxXkBrMu38Az1Z";
        const string parentFolderId = "urn:adsk.wipprod:fs.folder:co.DerBocbkXrcYsz43uJLTkW";
        var folderEndpoint =
            $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/folders/{folderId}";
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
        Folder folder = await sut.GetFolder(projectId, folderId, true);

        // Assert
        folder.ProjectId.Should().Be(projectId);
        folder.FolderId.Should().Be(folderId);
        folder.ParentFolderId.Should().Be(parentFolderId);
        folder.IsRootFolder.Should().BeFalse();
        folder.Files.Count.Should().Be(2);
        folder.Files.All(x => x.ParentFolder.FolderId == folderId).Should().BeTrue();
        folder.Files.All(x => x.ProjectId == projectId).Should().BeTrue();
        folder.Subfolders.Count.Should().Be(2);
        folder.Subfolders.All(x => x.ParentFolderId == folderId).Should().BeTrue();
        folder.Subfolders.All(x => x.ProjectId == projectId).Should().BeTrue();
        messageHandler.NumberOfCalls.Should().Be(4);
    }

    [Fact]
    public async Task GetProject_Should_ReturnProject()
    {
        // Arrange
        const string clientId = "AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY";
        const string clientSecret = "wE3GFhuIsGJEi3d4";
        const string accountId = "a867403b-f199-44fa-bdd6-a5dac350a541";
        const string projectId = "b.62185181-412c-4c01-b45c-6fcd429e58b2";
        const string projectName = "Sample Project - Seaport Civic Center";
        const string rootFolderId = "urn:adsk.wipprod:fs.folder:co.z5ncFrXRQiytzcYZrCj9-w";
        var projectEndpoint = $"https://developer.api.autodesk.com/project/v1/hubs/b.{accountId}/projects/{projectId}";
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
                RequestUri = new Uri(projectEndpoint),
                Response = await new StreamReader(@"ExampleRestApiResponses\ProjectResponse.json").ReadToEndAsync(),
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
        Project project = await sut.GetProject(projectId);

        // Assert
        project.ProjectId.Should().Be(projectId);
        project.Name.Should().Be(projectName);
        project.RootFolderId.Should().Be(rootFolderId);
        project.AccountId.Should().Be(accountId);
        project.RootFolder.Should().BeNull();
        messageHandler.NumberOfCalls.Should().Be(2);
    }

    [Fact]
    public async Task GetProject_getRootFolderContents_true_Should_ReturnProjectWithRootFolder()
    {
        // Arrange
        const string clientId = "AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY";
        const string clientSecret = "wE3GFhuIsGJEi3d4";
        const string accountId = "a867403b-f199-44fa-bdd6-a5dac350a541";
        const string projectId = "b.62185181-412c-4c01-b45c-6fcd429e58b2";
        const string projectName = "Sample Project - Seaport Civic Center";
        const string rootFolderId = "urn:adsk.wipprod:fs.folder:co.z5ncFrXRQiytzcYZrCj9-w";
        var projectEndpoint = $"https://developer.api.autodesk.com/project/v1/hubs/b.{accountId}/projects/{projectId}";
        var folderEndpoint =
            $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/folders/{rootFolderId}";
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
                RequestUri = new Uri(projectEndpoint),
                Response = await new StreamReader(@"ExampleRestApiResponses\ProjectResponse.json").ReadToEndAsync(),
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
        Project project = await sut.GetProject(projectId, true);

        // Assert
        project.ProjectId.Should().Be(projectId);
        project.Name.Should().Be(projectName);
        project.RootFolderId.Should().Be(rootFolderId);
        project.AccountId.Should().Be(accountId);
        project.RootFolder.Should().BeOfType<Folder>();
        messageHandler.NumberOfCalls.Should().Be(3);
    }

    [Fact]
    public async Task Folder_RecursiveEnumerators_Should_EnumerateContentsAppropriately()
    {
        // Arrange
        const string clientId = "AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY";
        const string clientSecret = "wE3GFhuIsGJEi3d4";
        const string accountId = "48a4d1eb-a370-42fe-89c9-4dd9e2ad9d41";
        const string projectId = "b.0577ff54-1967-4c9b-80d4-eb649bd0774d";
        const string folderId = "urn:adsk.wipprod:fs.folder:co.DerBocbkXrcYsz43uJLTkW";
        var folderEndpoint =
            $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/folders/{folderId}";
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
        Folder rootFolder = await sut.GetFolder(projectId, folderId);
        rootFolder.Files.Add(FakeData.GetFakeFile());
        rootFolder.Subfolders.Add(FakeData.GetFakeFolder(sut));
        rootFolder.Subfolders[0].Files.Add(FakeData.GetFakeFile());
        rootFolder.Subfolders[0].Subfolders.Add(FakeData.GetFakeFolder(sut));
        rootFolder.Subfolders[0].Subfolders[0].Files.Add(FakeData.GetFakeFile());
        rootFolder.Subfolders[0].Subfolders[0].Subfolders.AddRange(FakeData.GetFakeFolders(8, sut));
        foreach (Folder subfolder in rootFolder.Subfolders[0].Subfolders[0].Subfolders)
            subfolder.Files.Add(FakeData.GetFakeFile());

        // Act
        int subfolderCount = rootFolder.SubfoldersRecursive.Count();
        int filesCount = rootFolder.FilesRecursive.Count();

        // Assert
        subfolderCount.Should().Be(10);
        filesCount.Should().Be(11);
        messageHandler.NumberOfCalls.Should().Be(2);
    }

    [Fact]
    public async Task GetFolderContentsRecursively_Should_RecursivelyReturnFolderContents()
    {
        // Arrange
        const string clientId = "AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY";
        const string clientSecret = "wE3GFhuIsGJEi3d4";
        const string accountId = "48a4d1eb-a370-42fe-89c9-4dd9e2ad9d41";
        const string projectId = "b.62185181-412c-4c01-b45c-6fcd429e58b2";
        const string folderId = "urn:adsk.wipprod:fs.folder:co.9x1ON-8QSve2cJA6lAiegQ";
        var folderEndpoint = $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/folders/{folderId}";
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
                Response = await new StreamReader(@"ExampleRestApiResponses\Recursion_GetFolder_1.json")
                    .ReadToEndAsync(),
                StatusCode = HttpStatusCode.OK
            },
            new()
            {
                RequestUri = new Uri($"{folderEndpoint}/contents"),
                Response = await new StreamReader(@"ExampleRestApiResponses\Recursion_GetFolderContents_2.json")
                    .ReadToEndAsync(),
                StatusCode = HttpStatusCode.OK
            },
            new()
            {
                RequestUri =
                    new Uri(
                        $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/folders/urn:adsk.wipprod:fs.folder:co.-Cj9GOznROaMrLBtjvWzdg/contents"),
                Response = await new StreamReader(@"ExampleRestApiResponses\Recursion_GetFolderContents_3.json")
                    .ReadToEndAsync(),
                StatusCode = HttpStatusCode.OK
            },
            new()
            {
                RequestUri =
                    new Uri(
                        $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/folders/urn:adsk.wipprod:fs.folder:co.qh2HWoteQ9iymYm7DjbYvg/contents"),
                Response = await new StreamReader(@"ExampleRestApiResponses\Recursion_GetFolderContents_4.json")
                    .ReadToEndAsync(),
                StatusCode = HttpStatusCode.OK
            },
            new()
            {
                RequestUri =
                    new Uri(
                        $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/folders/urn:adsk.wipprod:fs.folder:co.zLe2AszxQjeGgdiSCf49Ug/contents"),
                Response = await new StreamReader(@"ExampleRestApiResponses\Recursion_GetFolderContents_5.json")
                    .ReadToEndAsync(),
                StatusCode = HttpStatusCode.OK
            },
            new()
            {
                RequestUri =
                    new Uri(
                        $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/folders/urn:adsk.wipprod:fs.folder:co.R86FfZRmS5eB0_cn1TIRUg/contents"),
                Response = await new StreamReader(@"ExampleRestApiResponses\Recursion_GetFolderContents_6.json")
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
        Folder folder = await sut.GetFolder(projectId, folderId);

        // Act
        await folder.GetContentsRecursively();

        // Assert
        folder.SubfoldersRecursive.Count().Should().Be(4);
        folder.FilesRecursive.Count().Should().Be(12);
        messageHandler.NumberOfCalls.Should().Be(7);
    }

    [Fact]
    public async Task GetFolder_GetContents_Should_MapFolderPropertiesAppropriately()
    {
        // Arrange
        const string clientId = "AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY";
        const string clientSecret = "wE3GFhuIsGJEi3d4";
        const string accountId = "48a4d1eb-a370-42fe-89c9-4dd9e2ad9d41";
        const string projectId = "b.62185181-412c-4c01-b45c-6fcd429e58b2";
        const string folderId = "urn:adsk.wipprod:fs.folder:co.9x1ON-8QSve2cJA6lAiegQ";
        const string folderEndpoint =
            $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/folders/{folderId}";
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
                Response = await new StreamReader(@"ExampleRestApiResponses\Recursion_GetFolder_1.json")
                    .ReadToEndAsync(),
                StatusCode = HttpStatusCode.OK
            },
            new()
            {
                RequestUri = new Uri($"{folderEndpoint}/contents"),
                Response = await new StreamReader(@"ExampleRestApiResponses\Recursion_GetFolderContents_2.json")
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
        Folder folder = await sut.GetFolder(projectId, folderId);

        // Act
        await folder.GetContents();

        // Assert
        messageHandler.NumberOfCalls.Should().Be(3);
        folder.Should().BeOfType<Folder>();
        folder.CreateTime.Should().BeBefore(DateTime.Now);
        folder.CreateUserId.Should().Be("58L2KZX7KE8FP5A4");
        folder.Created.Should().Be(false);
        folder.DirectoryInfo.Should().BeNull();
        folder.DisplayName.Should().Be("Recursion");
        folder.Files.Count.Should().Be(2);
        folder.FilesRecursive.Count().Should().Be(2);
        folder.FolderId.Should().Be(folderId);
        folder.Hidden.Should().BeFalse();
        folder.IsEmpty.Should().BeFalse();
        folder.IsNotEmpty.Should().BeTrue();
        folder.IsRootFolder.Should().BeFalse();
        folder.LastModifiedTime.Should().BeBefore(DateTime.Now);
        folder.LastModifiedTimeRollup.Should().BeBefore(DateTime.Now);
        folder.LastModifiedUserId.Should().Be("58L2KZX7KE8FP5A4");
        folder.LastModifiedUserName.Should().Be("Stewart Celani");
        folder.Name.Should().Be("Recursion");
        folder.ObjectCount.Should().Be(4);
        folder.ParentFolder.Should().BeNull();
        folder.ParentFolderId.Should().Be("urn:adsk.wipprod:fs.folder:co.5WdH8YwrSgWYSp9dA3E8Nw");
        folder.ProjectId.Should().Be("b.62185181-412c-4c01-b45c-6fcd429e58b2");
        folder.Subfolders.Count.Should().Be(2);
        folder.SubfoldersRecursive.Count().Should().Be(2);
        folder.Type.Should().Be("folders");
    }

    [Fact]
    public async Task GetFolder_GetContents_Should_MapSubfolderPropertiesAppropriately()
    {
        // Arrange
        const string clientId = "AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY";
        const string clientSecret = "wE3GFhuIsGJEi3d4";
        const string accountId = "48a4d1eb-a370-42fe-89c9-4dd9e2ad9d41";
        const string projectId = "b.62185181-412c-4c01-b45c-6fcd429e58b2";
        const string folderId = "urn:adsk.wipprod:fs.folder:co.9x1ON-8QSve2cJA6lAiegQ";
        const string folderEndpoint =
            $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/folders/{folderId}";
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
                Response = await new StreamReader(@"ExampleRestApiResponses\Recursion_GetFolder_1.json")
                    .ReadToEndAsync(),
                StatusCode = HttpStatusCode.OK
            },
            new()
            {
                RequestUri = new Uri($"{folderEndpoint}/contents"),
                Response = await new StreamReader(@"ExampleRestApiResponses\Recursion_GetFolderContents_2.json")
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
        Folder folder = await sut.GetFolder(projectId, folderId);

        // Act
        await folder.GetContents();
        Folder subfolder = folder.Subfolders[0];

        // Assert
        messageHandler.NumberOfCalls.Should().Be(3);
        subfolder.Should().BeOfType<Folder>();
        subfolder.CreateTime.Should().BeBefore(DateTime.Now);
        subfolder.CreateUserId.Should().Be("58L2KZX7KE8FP5A4");
        subfolder.Created.Should().Be(false);
        subfolder.DirectoryInfo.Should().BeNull();
        subfolder.DisplayName.Should().Be("1");
        subfolder.Files.Count.Should().Be(0);
        subfolder.FilesRecursive.Count().Should().Be(0);
        subfolder.FolderId.Should().Be("urn:adsk.wipprod:fs.folder:co.-Cj9GOznROaMrLBtjvWzdg");
        subfolder.Hidden.Should().BeFalse();
        subfolder.IsEmpty.Should().BeTrue();
        subfolder.IsNotEmpty.Should().BeFalse();
        subfolder.IsRootFolder.Should().BeFalse();
        subfolder.LastModifiedTime.Should().BeBefore(DateTime.Now);
        subfolder.LastModifiedTimeRollup.Should().BeBefore(DateTime.Now);
        subfolder.LastModifiedUserId.Should().Be("58L2KZX7KE8FP5A4");
        subfolder.LastModifiedUserName.Should().Be("Stewart Celani");
        subfolder.Name.Should().Be("1");
        subfolder.ObjectCount.Should().Be(7);
        subfolder.ParentFolder.Should().Be(folder);
        subfolder.ParentFolderId.Should().Be(folder.FolderId);
        subfolder.ProjectId.Should().Be("b.62185181-412c-4c01-b45c-6fcd429e58b2");
        subfolder.Subfolders.Count.Should().Be(0);
        subfolder.SubfoldersRecursive.Count().Should().Be(0);
        subfolder.Type.Should().Be("folders");
    }

    [Fact]
    public async Task GetFolder_GetContents_Should_MapFilePropertiesAppropriately()
    {
        // Arrange
        const string clientId = "AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY";
        const string clientSecret = "wE3GFhuIsGJEi3d4";
        const string accountId = "48a4d1eb-a370-42fe-89c9-4dd9e2ad9d41";
        const string projectId = "b.62185181-412c-4c01-b45c-6fcd429e58b2";
        const string folderId = "urn:adsk.wipprod:fs.folder:co.9x1ON-8QSve2cJA6lAiegQ";
        const string folderEndpoint =
            $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/folders/{folderId}";
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
                Response = await new StreamReader(@"ExampleRestApiResponses\Recursion_GetFolder_1.json")
                    .ReadToEndAsync(),
                StatusCode = HttpStatusCode.OK
            },
            new()
            {
                RequestUri = new Uri($"{folderEndpoint}/contents"),
                Response = await new StreamReader(@"ExampleRestApiResponses\Recursion_GetFolderContents_2.json")
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
        Folder folder = await sut.GetFolder(projectId, folderId);

        // Act
        await folder.GetContents();
        File file = folder.Files[0];

        // Assert
        messageHandler.NumberOfCalls.Should().Be(3);
        file.Should().BeOfType<File>();
        file.CreateTime.Should().BeBefore(DateTime.Now);
        file.CreateUserId.Should().Be("5E4FQ5CA6P9L");
        file.CreateUserName.Should().Be("ACC Sample Project");
        file.DisplayName.Should().Be("A001 - ARCHITECTURAL - GRAPHIC SYMBOLS & ABBREVIATIONS.pdf");
        file.DownloadAttempts.Should().Be(0);
        file.DownloadUrl.Should()
            .Be(
                "https://developer.api.autodesk.com/oss/v2/buckets/wip.dm.prod/objects/cc5f57d7-f3de-4343-a427-af591339746c.pdf?scopes=b360project.62185181-412c-4c01-b45c-6fcd429e58b2,O2tenant.27638224");
        file.Downloaded.Should().BeFalse();
        file.FileId.Should().Be("urn:adsk.wipprod:fs.file:vf.HJ4s0IyyXg-6_eYcUmDV8Q?version=1");
        file.FileInfo.Should().BeNull();
        file.FileType.Should().Be("pdf");
        file.Hidden.Should().BeFalse();
        file.LastModifiedTime.Should().BeBefore(DateTime.Now);
        file.LastModifiedUserId.Should().Be("5E4FQ5CA6P9L");
        file.LastModifiedUserName.Should().Be("ACC Sample Project");
        file.Name.Should().Be("A001 - ARCHITECTURAL - GRAPHIC SYMBOLS & ABBREVIATIONS.pdf");
        file.ParentFolder.Should().Be(folder);
        file.ProjectId.Should().Be("b.62185181-412c-4c01-b45c-6fcd429e58b2");
        file.Reserved.Should().BeFalse();
        file.ReservedTime.Should().BeBefore(DateTime.Now);
        file.ReservedUserId.Should().BeNull();
        file.ReservedUserName.Should().BeNull();
        file.StorageSize.Should().Be(78750);
        file.ApiReportedStorageSizeInMb.Should().Be(0.08m);
        file.Type.Should().Be("versions");
        file.VersionNumber.Should().Be(1);
    }

    [Fact]
    public async Task Folder_GetPath_Should_ReturnAppropriatePath()
    {
        // Arrange
        const string clientId = "AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY";
        const string clientSecret = "wE3GFhuIsGJEi3d4";
        const string accountId = "48a4d1eb-a370-42fe-89c9-4dd9e2ad9d41";
        const string projectId = "b.62185181-412c-4c01-b45c-6fcd429e58b2";
        const string folderId = "urn:adsk.wipprod:fs.folder:co.9x1ON-8QSve2cJA6lAiegQ";
        const string folderEndpoint =
            $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/folders/{folderId}";
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
                Response = await new StreamReader(@"ExampleRestApiResponses\Recursion_GetFolder_1.json")
                    .ReadToEndAsync(),
                StatusCode = HttpStatusCode.OK
            },
            new()
            {
                RequestUri = new Uri($"{folderEndpoint}/contents"),
                Response = await new StreamReader(@"ExampleRestApiResponses\Recursion_GetFolderContents_2.json")
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
        Folder folder = await sut.GetFolder(projectId, folderId);
        await folder.GetContents();
        Folder subfolder = folder.Subfolders[0];

        // Act
        string folderPath = folder.GetPath();
        string subfolderPath = subfolder.GetPath();

        // Assert
        messageHandler.NumberOfCalls.Should().Be(3);
        folderPath.Should().Be(@"\Recursion");
        subfolderPath.Should().Be(@"\Recursion\1");
    }

    [Fact]
    public async Task File_GetPath_Should_ReturnAppropriatePath()
    {
        // Arrange
        const string clientId = "AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY";
        const string clientSecret = "wE3GFhuIsGJEi3d4";
        const string accountId = "48a4d1eb-a370-42fe-89c9-4dd9e2ad9d41";
        const string projectId = "b.62185181-412c-4c01-b45c-6fcd429e58b2";
        const string folderId = "urn:adsk.wipprod:fs.folder:co.9x1ON-8QSve2cJA6lAiegQ";
        const string folderEndpoint =
            $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/folders/{folderId}";
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
                Response = await new StreamReader(@"ExampleRestApiResponses\Recursion_GetFolder_1.json")
                    .ReadToEndAsync(),
                StatusCode = HttpStatusCode.OK
            },
            new()
            {
                RequestUri = new Uri($"{folderEndpoint}/contents"),
                Response = await new StreamReader(@"ExampleRestApiResponses\Recursion_GetFolderContents_2.json")
                    .ReadToEndAsync(),
                StatusCode = HttpStatusCode.OK
            },
            new()
            {
                RequestUri =
                    new Uri(
                        $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/folders/urn:adsk.wipprod:fs.folder:co.-Cj9GOznROaMrLBtjvWzdg/contents"),
                Response = await new StreamReader(@"ExampleRestApiResponses\Recursion_GetFolderContents_3.json")
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
        Folder folder = await sut.GetFolder(projectId, folderId);
        await folder.GetContents();
        Folder subfolder = folder.Subfolders[0];
        await subfolder.GetContents();
        File folderFile = folder.Files[0];
        File subfolderFile = subfolder.Files[0];


        // Act
        string folderFilePath = folderFile.GetPath();
        string subfolderFilePath = subfolderFile.GetPath();

        // Assert
        messageHandler.NumberOfCalls.Should().Be(4);
        folderFilePath.Should().Be(@"\Recursion\A001 - ARCHITECTURAL - GRAPHIC SYMBOLS & ABBREVIATIONS.pdf");
        subfolderFilePath.Should().Be(@"\Recursion\1\A505 - OFFICE - ROOFING DETAILS.pdf");
    }
}