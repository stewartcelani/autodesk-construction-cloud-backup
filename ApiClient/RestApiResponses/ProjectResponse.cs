using Newtonsoft.Json;

namespace ACC.ApiClient.RestApiResponses;

public class ProjectResponse
{
    [JsonProperty("data")] public ProjectResponseProject Data { get; set; }
}

public class ProjectResponseProject
{
    [JsonProperty("type")] public string Type { get; set; }

    [JsonProperty("id")] public string Id { get; set; }

    [JsonProperty("attributes")] public ProjectResponseDataProjectAttributes Attributes { get; set; }

    [JsonProperty("relationships")] public ProjectResponseDataProjectRelationships Relationships { get; set; }
}

public class ProjectResponseDataProjectAttributes
{
    [JsonProperty("name")] public string Name { get; set; }
}

public class ProjectResponseDataProjectRelationships
{
    [JsonProperty("rootFolder")] public ProjectResponseDataProjectRelationshipsRootFolder RootFolder { get; set; }
}

public class ProjectResponseDataProjectRelationshipsRootFolder
{
    [JsonProperty("data")] public ProjectResponseDataProjectRelationshipsRootFolderData Data { get; set; }
}

public class ProjectResponseDataProjectRelationshipsRootFolderData
{
    [JsonProperty("id")] public string Id { get; set; }
}