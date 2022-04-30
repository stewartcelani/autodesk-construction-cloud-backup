using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Xunit;
using FluentAssertions;
using Library.Logger;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
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
        var baseAddress =  new Uri("https://www.test.com");
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
        var baseAddress =  new Uri("https://www.test.com");
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
        var messageHandler = new MockHttpMessageHandler(forbiddenResponse, HttpStatusCode.Forbidden);
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
        Func<Task> act = async() => await sut.GetAllProjects();
        
        // Assert
        await act
            .Should()
            .ThrowAsync<UnauthorizedAccessException>()
            .WithMessage(forbiddenResponse);
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
        var messageHandler = new MockHttpMessageHandler(unauthorizedResponse, HttpStatusCode.Unauthorized);
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
        Func<Task> act = async() => await sut.GetAllProjects();
        
        // Assert
        await act
            .Should()
            .ThrowAsync<UnauthorizedAccessException>()
            .WithMessage(unauthorizedResponse);
    }
    
    [Fact]
    public async Task GetAccessToken_InternalServerError_Should_Throw_GenericHttpRequestException()
    {
        // Arrange
        const string clientId = "AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY";
        const string clientSecret = "InvalidClientSecret";
        const string accountId = "f33e018a-d1f5-4ef3-ae67-606de6aeed87";
        var messageHandler = new MockHttpMessageHandler(string.Empty, HttpStatusCode.InternalServerError);
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
        Func<Task> act = async() => await sut.GetAllProjects();
        
        // Assert
        await act
            .Should()
            .ThrowAsync<HttpRequestException>();
    }
    
    
    
    
    
    
}