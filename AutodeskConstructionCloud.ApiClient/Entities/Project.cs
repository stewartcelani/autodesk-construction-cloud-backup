namespace AutodeskConstructionCloud.ApiClient.Entities;

public class Project
{
    public string ProjectId { get; set; }
    public string AccountId { get; set; }
    public string Name { get; set; }
    public string RootFolderId { get; set; }
    public List<Folder> Folders { get; set; } = new List<Folder>();
    public List<File> Files => Folders.SelectMany(folder => folder.Files).ToList();
    public bool GetContents(ApiClient apiClient, bool recursive = false)
    {
        throw new NotImplementedException();
    }

    public bool GetContentsRecursively(ApiClient apiClient)
    {
        return GetContents(apiClient, true);
    }
    
}