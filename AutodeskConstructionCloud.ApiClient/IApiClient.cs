using AutodeskConstructionCloud.ApiClient.Entities;

namespace AutodeskConstructionCloud.ApiClient;

public interface IApiClient
{
    public Task EnsureAccessToken();
    public Task<List<Project>> GetProjects();
}