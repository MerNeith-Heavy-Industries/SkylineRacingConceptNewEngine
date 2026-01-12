namespace nfm_world.mad.api;

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using ApiRes = (System.Net.HttpStatusCode, ApiResponse);

public class NfmwApi
{
    private static readonly string _baseAddr = "https://nfmwapi.jacher.io";

    private static HttpClient _client = new()
    {
        BaseAddress = new Uri(_baseAddr),
        Timeout = TimeSpan.FromSeconds(10),
    };

    static NfmwApi() {
        _client.DefaultRequestHeaders.UserAgent.Clear();
        _client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("NfmwClientAgent"));

        _client.DefaultRequestHeaders.Accept.Clear();
        _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public static void SetAuthorization(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(token);
    }

    public static async Task<ApiRes> GetAsync(string route)
    {
        using HttpResponseMessage response = await _client.GetAsync(route);

        return await ProcessResponse(response);
    }

    public static async Task<ApiRes> PostAsync(string route, object body)
    {
        using StringContent jsonContent = new(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json"
        );

        using HttpResponseMessage response = await _client.PostAsync(route, jsonContent);

        return await ProcessResponse(response);
    }

    public static async Task<ApiRes> PatchAsync(string route, object body)
    {
        using StringContent jsonContent = new(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json"
        );

        using HttpResponseMessage response = await _client.PatchAsync(route, jsonContent);

        return await ProcessResponse(response);
    }

    public static async Task<ApiRes> DeleteAsync(string route)
    {
        using HttpResponseMessage response = await _client.DeleteAsync(route);

        return await ProcessResponse(response);
    }

    private static async Task<ApiRes> ProcessResponse(HttpResponseMessage response)
    {
        var status = response.StatusCode;
        var content = await response.Content.ReadAsStreamAsync();
        var res = await JsonSerializer.DeserializeAsync<ApiResponse>(content);

        return (status, res);
    }
}