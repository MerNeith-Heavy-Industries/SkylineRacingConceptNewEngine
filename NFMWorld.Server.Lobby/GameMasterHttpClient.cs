using MemoryPack;
using NFMWorldLibrary.Multiplayer.HttpMessages;

namespace NFMWorldLibrary.Multiplayer;

/// <summary>
/// Sends HTTP requests from the Lobby to a Game Master instance.
/// 
/// Endpoints:
/// - POST /create-race   → Create a new race, returns join tokens + game address
/// - POST /race-ended     → Notify GM that a race has been cleaned up (future)
/// </summary>
public class GameMasterHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly string _secretKey;

    public GameMasterHttpClient(string secretKey)
    {
        _secretKey = secretKey;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
    }

    /// <summary>
    /// Creates a race on the specified Game Master.
    /// Returns the response with per-player join tokens and the game server address.
    /// </summary>
    public async Task<Lobby2RaceServer_CreateRaceResponse> CreateRaceAsync(
        ResolvedGameMaster master,
        Lobby2RaceServer_CreateRace request)
    {
        var url = new Uri(master.HttpEndpoint, "/create-race");
        var body = MemoryPackSerializer.Serialize(request);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new ByteArrayContent(body)
        };
        httpRequest.Headers.Add("Authorization", $"Bearer {_secretKey}");
        httpRequest.Content.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

        using var response = await _httpClient.SendAsync(httpRequest);
        response.EnsureSuccessStatusCode();

        var responseBytes = await response.Content.ReadAsByteArrayAsync();
        return MemoryPackSerializer.Deserialize<Lobby2RaceServer_CreateRaceResponse>(responseBytes);
    }

    /// <summary>
    /// Notifies the Game Master that a race has ended (cleanup callback from Lobby).
    /// </summary>
    public async Task NotifyRaceEndedAsync(
        ResolvedGameMaster master,
        RaceServer2Lobby_RaceResults results)
    {
        var url = new Uri(master.HttpEndpoint, "/race-ended");
        var body = MemoryPackSerializer.Serialize(results);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new ByteArrayContent(body)
        };
        httpRequest.Headers.Add("Authorization", $"Bearer {_secretKey}");
        httpRequest.Content.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

        using var response = await _httpClient.SendAsync(httpRequest);
        response.EnsureSuccessStatusCode();
    }
}
