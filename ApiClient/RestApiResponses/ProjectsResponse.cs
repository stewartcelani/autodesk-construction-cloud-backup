using Newtonsoft.Json;

namespace ACC.ApiClient.RestApiResponses;

/*
 * https://forge.autodesk.com/en/docs/data/v2/reference/http/hubs-hub_id-projects-GET/
 */
public class ProjectsResponse
{
    [JsonProperty("links")] public ProjectsResponseLinks Links { get; set; }

    [JsonProperty("data")] public List<ProjectResponseProject> Data { get; set; }
}

public class ProjectsResponseLinks
{
    [JsonProperty("self")] public ProjectsResponseLink Self { get; set; }

    [JsonProperty("first")] public ProjectsResponseLink First { get; set; }

    [JsonProperty("next")] public ProjectsResponseLink Next { get; set; }
}

public class ProjectsResponseLink
{
    [JsonProperty("href")] public string Href { get; set; }
}