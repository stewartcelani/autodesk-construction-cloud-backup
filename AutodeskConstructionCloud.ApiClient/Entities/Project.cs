namespace AutodeskConstructionCloud.ApiClient.Entities;

public class Project
{
    public string ProjectId { get; set; }
    public string AccountId { get; set; }
    public string Name { get; set; }
    public string RootFolderId { get; set; }
    public List<Folder> Folders { get; set; } = new List<Folder>();
    public List<File> Files { get; set; } = new List<File>();
}