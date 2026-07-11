using System.Net;
using System.Text;
using Maxine.Extensions;
using MemoryPack;
using Microsoft.Extensions.Logging;
using NFMWorldLibrary;
using NFMWorldLibrary.Multiplayer;
using NFMWorldLibrary.Multiplayer.HttpMessages;
using WebSocketSharp.Server;

Console.WriteLine("Hello, World!");

var endpoint = Environment.GetEnvironmentVariable("SERVER_ENDPOINT") ?? $"http://localhost:7000/";
var secretKey = Environment.GetEnvironmentVariable("SERVER_SECRET_KEY");

var orchestrator = new RaceOrchestrator(new ENetMultiplayerServerTransport());
orchestrator.Start();

var server = new HttpServer(endpoint);
server.OnPost += (sender, e) =>
{
    var req = e.Request;
    var res = e.Response;
    var path = req.RawUrl;

    if (req.Headers["Authorization"] != "Bearer " + secretKey)
    {
        res.StatusCode = 401;
        res.Close();
        return;
    }

    if (path == "/create-race")
    {
        using var seq = req.InputStream.AsPooledReadOnlySequence();
        var raceParams = MemoryPackSerializer.Deserialize<Lobby2RaceServer_CreateRace>(seq.Sequence);
        
        var response = orchestrator.CreateRace(raceParams);
        
        res.ContentType = "application/octet-stream";
        res.Close(MemoryPackSerializer.Serialize(response), false);
    }
};
server.Start();

Console.CancelKeyPress += (sender, eventArgs) =>
{
    server.Stop();
    orchestrator.Stop();
};