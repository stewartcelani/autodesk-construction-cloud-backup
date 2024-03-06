using Newtonsoft.Json;

namespace ACC.ApiClient.RestApiResponses;

/*
 * https://aps.autodesk.com/en/docs/oauth/v2/reference/http/gettoken-POST/
 */
public class AuthenticateResponse
{
    [JsonProperty("access_token")] public string AccessToken { get; set; }

    [JsonProperty("expires_in")] public int ExpiresIn { get; set; }
}