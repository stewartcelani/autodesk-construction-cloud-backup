using Newtonsoft.Json;

namespace AutodeskConstructionCloud.ApiClient.RestApiResponses;

/*
 * https://forge.autodesk.com/en/docs/data/v2/reference/http/hubs-hub_id-projects-GET/
 */
public class ProjectsResponse
{
    [JsonProperty("links")]
    public ProjectsResponseLinks Links { get; set; }

    [JsonProperty("data")]
    public List<ProjectsResponseProject> Data { get; set; }
}

public class ProjectsResponseLinks
{
    [JsonProperty("self")]
    public ProjectsResponseLink Self { get; set; }

    [JsonProperty("first")]
    public ProjectsResponseLink First { get; set; }

    [JsonProperty("next")]
    public ProjectsResponseLink Next { get; set; }
}

public class ProjectsResponseLink
{
    [JsonProperty("href")]
    public string Href { get; set; }
}
 
public class ProjectsResponseProject
{
    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("attributes")]
    public ProjectsResponseDataProjectAttributes Attributes { get; set; }

    [JsonProperty("relationships")]
    public ProjectsResponseDataProjectRelationships Relationships { get; set; }

}

public class ProjectsResponseDataProjectAttributes
{
    [JsonProperty("name")]
    public string Name { get; set; }
}

public class ProjectsResponseDataProjectRelationships
{
    [JsonProperty("rootFolder")]
    public ProjectsResponseDataProjectRelationshipsRootFolder RootFolder {get; set; }
}

public class ProjectsResponseDataProjectRelationshipsRootFolder
{
    [JsonProperty("data")]
    public ProjectsResponseDataProjectRelationshipsRootFolderData Data { get; set; }
}

public class ProjectsResponseDataProjectRelationshipsRootFolderData
{
    [JsonProperty("id")]
    public string Id { get; set; }
}