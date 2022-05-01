using Newtonsoft.Json;

namespace AutodeskConstructionCloud.ApiClient.RestApiResponses;

public class FolderResponse
{
    [JsonProperty("data")] public FolderContentsResponseData Data { get; set; }
}