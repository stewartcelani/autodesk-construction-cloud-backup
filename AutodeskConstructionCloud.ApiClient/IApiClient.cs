namespace AutodeskConstructionCloud.ApiClient;

public interface IApiClient
{
    public Task GetAllProjects(string[] projectsToExclude, string[] projectsToInclude);
}