using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using AutodeskConstructionCloud.ApiClient.Entities;
using Xunit;
using FluentAssertions;
using Library.Logger;

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
            .ThrowAsync<UnauthorizedAccessException>()
            .WithMessage(forbiddenResponse);
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
            .ThrowAsync<UnauthorizedAccessException>()
            .WithMessage(unauthorizedResponse);
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
            .ThrowAsync<HttpRequestException>();
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
            new ()
            {
                RequestUri = new Uri("https://developer.api.autodesk.com/authentication/v1/authenticate"),
                Response = await new StreamReader(@"ExampleRestApiResponses\AuthenticateResponse.json").ReadToEndAsync(),
                StatusCode = HttpStatusCode.OK
            },
            new ()
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
            new ()
            {
                RequestUri = new Uri("https://developer.api.autodesk.com/authentication/v1/authenticate"),
                Response = await new StreamReader(@"ExampleRestApiResponses\AuthenticateResponse.json").ReadToEndAsync(),
                StatusCode = HttpStatusCode.OK
            },
            new ()
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
            .ThrowAsync<HttpRequestException>();
        messageHandler.NumberOfCalls.Should().Be(3);
    }
    
    [Fact]
    public async Task GetProjects_WithPagination_Should_ReturnTwoProjects()
    {
        // Arrange
        const string clientId = "AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY";
        const string clientSecret = "wE3GFhuIsGJEi3d4";
        const string accountId = "48a4d1eb-a370-42fe-89c9-4dd9e2ad9d41";
        var messageHandlerMappings = new List<MockHttpMessageHandlerMapping>
        {
            new ()
            {
                RequestUri = new Uri("https://developer.api.autodesk.com/authentication/v1/authenticate"),
                Response = await new StreamReader(@"ExampleRestApiResponses\AuthenticateResponse.json").ReadToEndAsync(),
                StatusCode = HttpStatusCode.OK
            },
            new ()
            {
                RequestUri = new Uri($"https://developer.api.autodesk.com/project/v1/hubs/b.{accountId}/projects"),
                Response = await new StreamReader(@"ExampleRestApiResponses\ProjectsResponseWithPagination.json").ReadToEndAsync(),
                StatusCode = HttpStatusCode.OK
            },
            new ()
            {
                RequestUri = new Uri($"https://developer.api.autodesk.com/project/v1/hubs/b.{accountId}/projects?page%5Bnumber%5D=2&page%5Blimit%5D=2"),
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
}