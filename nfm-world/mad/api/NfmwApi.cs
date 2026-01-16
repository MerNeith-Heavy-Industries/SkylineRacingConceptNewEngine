namespace nfm_world.mad.api;

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

public class NfmwApi
{
    private static readonly string _baseAddr = "http://127.0.0.1:8074";

    private static HttpClient _client = new()
    {
        BaseAddress = new Uri(_baseAddr),
    };

    static NfmwApi() {
        _client.DefaultRequestHeaders.Remove("User-Agent");
        _client.DefaultRequestHeaders.Add("User-Agent", "NfmwClientAgent");

        _client.DefaultRequestHeaders.Accept.Clear();
        _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public static void SetAuthorization(string token)
    {
        _client.DefaultRequestHeaders.Remove("Authorization");
        _client.DefaultRequestHeaders.Add("Authorization", token);
    }

    public static async Task<(HttpStatusCode, T?)> GetAsync<T>(string route)
    {
        using HttpResponseMessage response = await _client.GetAsync(route);

        return await ProcessResponse<T>(response);
    }

    public static async Task<(HttpStatusCode, T?)> PostAsync<T>(string route, object body)
    {
        using StringContent jsonContent = new(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json"
        );

        using HttpResponseMessage response = await _client.PostAsync(route, jsonContent);

        return await ProcessResponse<T>(response);
    }

    public static async Task<(HttpStatusCode, T?)> PatchAsync<T>(string route, object body)
    {
        using StringContent jsonContent = new(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json"
        );

        using HttpResponseMessage response = await _client.PatchAsync(route, jsonContent);

        return await ProcessResponse<T>(response);
    }

    public static async Task<(HttpStatusCode, T?)> DeleteAsync<T>(string route)
    {
        using HttpResponseMessage response = await _client.DeleteAsync(route);

        return await ProcessResponse<T>(response);
    }

    private static async Task<(HttpStatusCode, T?)> ProcessResponse<T>(HttpResponseMessage response)
    {
        var status = response.StatusCode;
        var content = await response.Content.ReadAsStreamAsync();

        var res = await JsonSerializer.DeserializeAsync<T>(content);

        return (status, res);
    }
}