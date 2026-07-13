using System.Text;
using MemoryPack;
using NFMWorldLibrary.Multiplayer;
using NFMWorldLibrary.Multiplayer.HttpMessages;
using WebSocketSharp.Server;

Console.WriteLine("NFMWorld Game Master starting...");

var endpoint = Environment.GetEnvironmentVariable("GM_HTTP_ENDPOINT") ?? "http://localhost:7003/";
var gamePort = ushort.Parse(Environment.GetEnvironmentVariable("GM_GAME_PORT") ?? "7002");

// HMAC key configuration: "keyId=base64secret,..." (e.g. "primary=c2VjcmV0a2V5")
var keysConfig = Environment.GetEnvironmentVariable("GM_HMAC_KEYS") ?? "";
var knownKeys = HmacAuth.ParseKnownKeys(keysConfig);

if (knownKeys.Count == 0)
    Console.WriteLine("[GameMaster] WARNING: no HMAC keys configured. All requests will be rejected.");

ENet.Library.Initialize();
var transport = new ENetMultiplayerServerTransport(gamePort);
var orchestrator = new RaceOrchestrator(transport);
orchestrator.Start();

var httpServer = new HttpServer(endpoint);
httpServer.OnPost += (sender, e) =>
{
    var req = e.Request;
    var res = e.Response;
    Console.WriteLine($"[GameMaster] HTTP {req.HttpMethod} {req.RawUrl} from {req.RemoteEndPoint}");

    // Read body for HMAC verification
    using var ms = new MemoryStream();
    req.InputStream.CopyTo(ms);
    var bodyArray = ms.ToArray();

    var method = req.HttpMethod;
    var path = req.RawUrl ?? "/";

    var authHeader = req.Headers["Authorization"];
    var error = HmacAuth.Verify(method, path, bodyArray, authHeader, knownKeys);

    if (error is not null)
    {
        Console.WriteLine($"[GameMaster] Auth failed: {error}");
        res.StatusCode = 401;
        res.Close(Encoding.UTF8.GetBytes(error), false);
        return;
    }

    if (path == "/create-race")
    {
        var raceParams = MemoryPackSerializer.Deserialize<Lobby2RaceServer_CreateRace>(bodyArray);
        var response = orchestrator.CreateRace(raceParams);

        res.ContentType = "application/octet-stream";
        res.Close(MemoryPackSerializer.Serialize(response), false);
    }
    else
    {
        res.StatusCode = 404;
        res.Close();
    }
};
httpServer.Start();

Console.WriteLine($"[GameMaster] Game port {gamePort}, HTTP on {endpoint}");
Console.WriteLine("[GameMaster] Press Ctrl+C to stop.");

var tcs = new TaskCompletionSource();
Console.CancelKeyPress += (_, _) =>
{
    Console.WriteLine("[GameMaster] Shutting down...");
    httpServer.Stop();
    orchestrator.Stop();
    ENet.Library.Deinitialize();
    tcs.SetResult();
};

await tcs.Task;