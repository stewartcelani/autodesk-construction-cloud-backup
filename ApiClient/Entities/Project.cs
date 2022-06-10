using Library.Extensions;

namespace ACC.ApiClient.Entities;

public class Project
{
    private readonly ApiClient _apiClient;

    public Project(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /*
     * These properties are mapped from Autodesk Api
     */
    public string ProjectId { get; set; }
    public string AccountId { get; set; }
    public string Name { get; set; }

    public string RootFolderId { get; set; }

    /*
     * Properties not directly from the Autodesk Api are below
     */
    public Folder? RootFolder { get; set; }

    public IEnumerable<Folder> SubfoldersRecursive => RootFolder is null
        ? new List<Folder>()
        : RootFolder.Subfolders.FlattenRecursive(x => x.Subfolders);

    public IEnumerable<File> FilesRecursive => RootFolder is null
        ? new List<File>()
        : RootFolder.Files.Concat(SubfoldersRecursive.SelectMany(x => x.Files));

    public async Task GetRootFolder()
    {
        RootFolder ??= await _apiClient.GetFolder(ProjectId, RootFolderId);
        await RootFolder.GetContents();
    }

    public async Task GetContentsRecursively()
    {
        _apiClient.Config.Logger?.Trace("Top");
        RootFolder ??= await _apiClient.GetFolder(ProjectId, RootFolderId);
        await RootFolder.GetContentsRecursively();
    }

    public async Task DownloadContentsRecursively(string downloadPath, CancellationToken ct = default)
    {
        _apiClient.Config.Logger?.Trace("Top");
        ApiClient.CreateDirectories(SubfoldersRecursive, downloadPath);
        await _apiClient.DownloadFiles(FilesRecursive, downloadPath, ct);
    }
}