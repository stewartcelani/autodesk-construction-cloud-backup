﻿namespace AutodeskConstructionCloud.ApiClient.Entities;

public class File
{
    /*
     * These properties are mapped from Autodesk Api
     */  
    public string FileId { get; set; }
    public string Name { get; set; }
    public string FileType { get; set; }
    public string Type { get; set; }
    public int VersionNumber { get; set; }
    public string DisplayName { get; set; }
    public DateTime CreateTime { get; set; }
    public string CreateUserId { get; set; }
    public int StorageSize { get; set; }
    public string CreateUserName { get; set; }
    public bool Hidden { get; set; }
    public DateTime LastModifiedTime { get; set; }
    public string LastModifiedUserId { get; set; }
    public string LastModifiedUserName { get; set; }
    public string DownloadUrl { get; set; }
    public bool Reserved { get; set; }
    public DateTime ReservedTime { get; set; }
    public string ReservedUserId { get; set; }
    public string ReservedUserName { get; set; }
    /*
     * Properties not directly from the Autodesk Api are below
     */
    public Folder ParentFolder { get; set; }
    public string ProjectId { get; set; }
    public int DownloadAttempts { get; set; } = 0;
    public FileInfo? FileInfo { get; set; }
    public bool Downloaded => FileInfo != null;
    public string GetPath(string delimiter = @"\")
    {
        return $"{ParentFolder.GetPath(delimiter)}{delimiter}{Name}";
    }
}