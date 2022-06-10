using Newtonsoft.Json;

namespace ACC.ApiClient.RestApiResponses;

/*
 * https://forge.autodesk.com/en/docs/oauth/v1/reference/http/authenticate-POST/
 */
public class AuthenticateResponse
{
    [JsonProperty("access_token")] public string AccessToken { get; set; }

    [JsonProperty("expires_in")] public int ExpiresIn { get; set; }
}