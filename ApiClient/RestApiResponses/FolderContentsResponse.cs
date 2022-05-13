using Newtonsoft.Json;

namespace ACC.ApiClient.RestApiResponses;

public class FolderContentsResponse
{
    [JsonProperty("links")] public FolderContentsResponseLinks Links { get; set; }

    [JsonProperty("data")] public List<FolderContentsResponseData> Data { get; set; }

    [JsonProperty("included")] public List<FolderContentsResponseIncluded> Included { get; set; }
}

public class FolderContentsResponseLinks
{
    [JsonProperty("self")] public FolderContentsResponseLink Self { get; set; }

    [JsonProperty("first")] public FolderContentsResponseLink First { get; set; }

    [JsonProperty("next")] public FolderContentsResponseLink Next { get; set; }
}

public class FolderContentsResponseLink
{
    [JsonProperty("href")] public string Href { get; set; }
}

public class FolderContentsResponseDataAttributesExtensionSchema
{
    [JsonProperty("href")] public string Href { get; set; }
}

public class FolderContentsResponseData
{
    [JsonProperty("type")] public string Type { get; set; }
    [JsonProperty("id")] public string Id { get; set; }
    [JsonProperty("attributes")] public FolderContentsResponseDataAttributes Attributes { get; set; }
    [JsonProperty("relationships")] public FolderContentsResponseRelationships Relationships { get; set; }
}

public class FolderContentsResponseDataAttributesExtension
{
    [JsonProperty("type")] public string Type { get; set; }

    [JsonProperty("version")] public string Version { get; set; }

    [JsonProperty("schema")] public FolderContentsResponseDataAttributesExtensionSchema Schema { get; set; }

    [JsonProperty("data")] public FolderContentsResponseDataAttributesExtensionData Data { get; set; }
}

public class FolderContentsResponseDataAttributesExtensionData
{
    [JsonProperty("sourceFileName")] public string SourceFileName { get; set; }

    [JsonProperty("visibleTypes")] public string[] VisibleTypes { get; set; }

    [JsonProperty("allowedTypes")] public string[] AllowedTypes { get; set; }

    [JsonProperty("namingStandardIds")] public string[] NamingStandardIds { get; set; }
}

public class FolderContentsResponseDataAttributes
{
    [JsonProperty("name")] public string Name { get; set; }

    [JsonProperty("displayName")] public string DisplayName { get; set; }

    [JsonProperty("createTime")] public DateTime CreateTime { get; set; }

    [JsonProperty("createUserId")] public string CreateUserId { get; set; }

    [JsonProperty("createUserName")] public string CreateUserName { get; set; }

    [JsonProperty("lastModifiedTime")] public DateTime LastModifiedTime { get; set; }

    [JsonProperty("lastModifiedUserId")] public string LastModifiedUserId { get; set; }

    [JsonProperty("lastModifiedUserName")] public string LastModifiedUserName { get; set; }

    [JsonProperty("lastModifiedTimeRollup")]
    public DateTime LastModifiedTimeRollup { get; set; }

    [JsonProperty("path")] public string Path { get; set; }

    [JsonProperty("objectCount")] public int ObjectCount { get; set; }

    [JsonProperty("hidden")] public bool Hidden { get; set; }

    [JsonProperty("extension")] public FolderContentsResponseDataAttributesExtension Extension { get; set; }
}

public class FolderContentsResponseIncluded
{
    [JsonProperty("type")] public string Type { get; set; }

    [JsonProperty("id")] public string Id { get; set; }

    [JsonProperty("attributes")] public FolderContentsResponseIncludedAttributes Attributes { get; set; }

    [JsonProperty("relationships")] public FolderContentsResponseIncludeRelationships Relationships { get; set; }
}

public class FolderContentsResponseIncludedAttributes
{
    [JsonProperty("name")] public string Name { get; set; }

    [JsonProperty("displayName")] public string DisplayName { get; set; }

    [JsonProperty("createTime")] public DateTime CreateTime { get; set; }

    [JsonProperty("createUserId")] public string CreateUserId { get; set; }

    [JsonProperty("createUserName")] public string CreateUserName { get; set; }

    [JsonProperty("lastModifiedTime")] public DateTime LastModifiedTime { get; set; }

    [JsonProperty("lastModifiedUserId")] public string LastModifiedUserId { get; set; }

    [JsonProperty("lastModifiedUserName")] public string LastModifiedUserName { get; set; }

    [JsonProperty("versionNumber")] public int VersionNumber { get; set; }

    [JsonProperty("storageSize")] public int StorageSize { get; set; }

    [JsonProperty("fileType")] public string FileType { get; set; }

    [JsonProperty("hidden")] public bool Hidden { get; set; }

    [JsonProperty("reserved")] public bool Reserved { get; set; }

    [JsonProperty("reservedTime")] public DateTime ReservedTime { get; set; }

    [JsonProperty("reservedUserId")] public string ReservedUserId { get; set; }

    [JsonProperty("reservedUserName")] public string ReservedUserName { get; set; }
}

public class FolderContentsResponseIncludeRelationships
{
    [JsonProperty("storage")] public FolderContentsResponseIncludeRelationshipsStorage Storage { get; set; }
}

public class FolderContentsResponseIncludeRelationshipsStorage
{
    [JsonProperty("data")] public FolderContentsResponseIncludeRelationshipsStorageData Data { get; set; }

    [JsonProperty("meta")] public FolderContentsResponseIncludeRelationshipsStorageMeta Meta { get; set; }
}

public class FolderContentsResponseIncludeRelationshipsStorageMeta
{
    [JsonProperty("link")] public FolderContentsResponseLink Link { get; set; }
}

public class FolderContentsResponseIncludeRelationshipsStorageData
{
    [JsonProperty("type")] public string Type { get; set; }
    [JsonProperty("id")] public string Id { get; set; }
}

public class FolderContentsResponseRelationships
{
    [JsonProperty("parent")] public FolderContentsResponseRelationshipsParent Parent { get; set; }
}

public class FolderContentsResponseRelationshipsParent
{
    [JsonProperty("data")] public FolderContentsResponseRelationshipsParentData Data { get; set; }
}

public class FolderContentsResponseRelationshipsParentData
{
    [JsonProperty("type")] public string Type { get; set; }

    [JsonProperty("id")] public string Id { get; set; }
}