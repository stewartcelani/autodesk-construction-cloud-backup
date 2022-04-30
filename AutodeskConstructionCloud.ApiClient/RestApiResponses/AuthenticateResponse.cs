using Newtonsoft.Json;

namespace AutodeskConstructionCloud.ApiClient.RestApiResponses;

/*
 * https://forge.autodesk.com/en/docs/oauth/v1/reference/http/authenticate-POST/
 */
public class AuthenticateResponse
{
    [JsonProperty("access_token")]
    public string AccessToken { get; set; }

    [JsonProperty("token_type")]
    public string TokenType { get; set; }

    [JsonProperty("expires_in")]
    public int ExpiresIn { get; set; }
}