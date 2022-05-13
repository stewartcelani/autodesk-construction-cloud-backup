using Newtonsoft.Json;

namespace ACC.ApiClient.RestApiResponses;

public class FolderResponse
{
    [JsonProperty("data")] public FolderContentsResponseData Data { get; set; }
}