using Maxine.Extensions;
using MemoryPack;
using NFMWorldLibrary.Multiplayer;
using NFMWorldLibrary.Multiplayer.HttpMessages;
using WebSocketSharp.Server;

Console.WriteLine("NFMWorld Lobby Server starting...");

var endpoint = Environment.GetEnvironmentVariable("LOBBY_HTTP_ENDPOINT") ?? "http://localhost:7001/";

ENet.Library.Initialize();

// HTTP endpoint for Game Masters to report race results
var httpServer = new HttpServer(endpoint);

var orchestrator = new GameOrchestrator(new WebSocketMultiplayerServerTransport(httpServer));
orchestrator.Start();

httpServer.OnPost += (sender, e) =>
{
    var req = e.Request;
    var res = e.Response;
    var path = req.RawUrl;

    if (path == "/race-ended")
    {
        using var seq = req.InputStream.AsPooledReadOnlySequence();
        var results = MemoryPackSerializer.Deserialize<RaceServer2Lobby_RaceResults>(seq.Sequence);

        Console.WriteLine(
            $"[Lobby] Race ended: MatchKey={results.MatchKey}, " +
            $"Players={results.PlayerResults?.Count ?? 0}");

        // TODO: update session state, notify lobby clients, clean up after 5 min

        res.StatusCode = 200;
        res.Close();
    }
    else
    {
        res.StatusCode = 404;
        res.Close();
    }
};
httpServer.Start();

Console.WriteLine($"[Lobby] Listening on {endpoint}");
Console.WriteLine("[Lobby] Press Ctrl+C to stop.");

Console.CancelKeyPress += (_, _) =>
{
    Console.WriteLine("[Lobby] Shutting down...");
    httpServer.Stop();
    orchestrator.Stop();
    ENet.Library.Deinitialize();
};

// Keep alive
await Task.Delay(-1);