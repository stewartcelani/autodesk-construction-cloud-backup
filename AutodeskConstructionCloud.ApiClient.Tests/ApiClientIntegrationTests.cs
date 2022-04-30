using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using AutodeskConstructionCloud.ApiClient.Entities;
using Xunit;
using FluentAssertions;
using Library;
// ReSharper disable AsyncVoidLambda


namespace AutodeskConstructionCloud.ApiClient.Tests;

public class ApiClientIntegrationTests
{
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _accountId;

    public ApiClientIntegrationTests()
    {
        _clientId = SecretsManager.GetEnvironmentVariableOrDefaultTo("acc:clientid", "InvalidClientId");
        _clientSecret = SecretsManager.GetEnvironmentVariableOrDefaultTo("acc:clientsecret", "InvalidSecret");
        _accountId = SecretsManager.GetEnvironmentVariableOrDefaultTo("acc:accountid", "InvalidAcountId");
    }

    [Fact]
    public async Task GetAccessToken_InvalidClientId_Should_Throw_403Forbidden()
    {
        // Arrange
        const string clientId = "AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY";
        const string clientSecret = "wE3GFhuIsGJEi3d4";
        const string accountId = "f33e018a-d1f5-4ef3-ae67-606de6aeed87";
        const string forbiddenResponse = 
            $@"{{ ""developerMessage"":""The client_id specified does not have access to the api product"", ""moreInfo"": ""https://forge.autodesk.com/en/docs/oauth/v2/developers_guide/error_handling/"", ""errorCode"": ""AUTH-001""}}";
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
        Func<Task> act = async() => await sut.EnsureAccessToken();
        
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
        string clientId = SecretsManager.GetEnvironmentVariableOrDefaultTo("acc:clientid", "InvalidClientId");
        const string clientSecret = "InvalidClientSecret";
        const string accountId = "IrrelevantToTest";
        const string unauthorizedResponse = 
            $@"{{""developerMessage"":""The client_id (application key)/client_secret are not valid"",""errorCode"":""AUTH-003"",""more info"":""https://forge.autodesk.com/en/docs/oauth/v2/developers_guide/error_handling/""}}";

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
        Func<Task> act = async() => await sut.EnsureAccessToken();
        
        // Assert
        await act
            .Should()
            .ThrowAsync<UnauthorizedAccessException>()
            .WithMessage(unauthorizedResponse);
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
        await sut.EnsureAccessToken();
        
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
    }

}