using Lua;
using LuauBenchmark;

// Quiet the game's per-node Debug logging (LuaUiLibrary logs every setProperty/commitTextUpdate),
// which would otherwise flood stdout and skew the timing. Default to Warning; override to e.g.
// "Trace" if you want verbose logs. Must be set before the first Logging use (host creation).
if (Environment.GetEnvironmentVariable("NFMW_LOG_MIN_LEVEL") is null)
{
    Environment.SetEnvironmentVariable("NFMW_LOG_MIN_LEVEL", "Warning");
}

// CLI:  LuauBenchmark <scenario> [runs]
//   scenario: fixed64 | preact-small | preact-large | all   (default: all)
//   runs    : best-of-N runs per scenario                    (default: 3)

var scenario = args.Length > 0 ? args[0] : "all";
var runs = ParseInt(args, 1, 3);

var root = FindRepoRoot();
var libRoot = Path.Combine(root, "NFMWorld.Library", "data", "library");
var scripts = Path.Combine(root, "bench", "luau-benchmark", "scripts");

using var host = new BenchmarkHost(libRoot);

Console.WriteLine("=== Luau VM benchmark (Lua-CSharp host) ===");
Console.WriteLine($"Best of {runs} runs. CPU = os.clock (in-Lua), Wall = C# Stopwatch.\n");

switch (scenario)
{
    case "fixed64":
        RunFixed64(host, scripts, runs);
        break;
    case "preact-small":
        RunPreact(host, scripts, runs, name: "preact-small", size: 16, fresh: true, iterations: 10000);
        break;
    case "preact-large":
        RunPreact(host, scripts, runs, name: "preact-large", size: 1024, fresh: false, iterations: 100);
        break;
    default:
        RunFixed64(host, scripts, runs);
        RunPreact(host, scripts, runs, "preact-small", 16, true, 10000);
        RunPreact(host, scripts, runs, "preact-large", 1024, false, 100);
        break;
}

// ---------------------------------------------------------------- scenarios

static void RunFixed64(BenchmarkHost host, string scripts, int runs)
{
    var path = Path.Combine(scripts, "fixed64_kernel.luau");
    const int iterations = 10000, nodes = 500;
    Console.WriteLine($"fixed64 : {iterations} iters x {nodes}-node min-dist scan + trig (el_stupido kernel)");

    double bestCpu = double.MaxValue, bestWall = double.MaxValue;
    string? checksum = null;
    for (var r = 0; r < runs; r++)
    {
        var (cpu, wall, rets) = host.RunScript(path, new LuaValue((double)iterations), new LuaValue((double)nodes));
        if (cpu < bestCpu) { bestCpu = cpu; bestWall = wall; }
        if (rets.Length > 1) checksum = rets[1].ToString();
    }
    PrintResult("fixed64", bestCpu, bestWall);
    Console.WriteLine($"  checksum: {checksum}\n");
}

static void RunPreact(BenchmarkHost host, string scripts, int runs, string name, int size, bool fresh, int iterations)
{
    var path = Path.Combine(scripts, "preact_render.luau");
    var mode = fresh ? "fresh-props (interop-bound)" : "stable-props (reconciler-bound)";
    Console.WriteLine($"{name} : {iterations} re-renders x {size}-node tree, {mode}");

    double bestCpu = double.MaxValue, bestWall = double.MaxValue;
    for (var r = 0; r < runs; r++)
    {
        var (cpu, wall, _) = host.RunScript(path, new LuaValue((double)iterations), new LuaValue((double)size), new LuaValue(fresh));
        if (cpu < bestCpu) { bestCpu = cpu; bestWall = wall; }
    }
    PrintResult(name, bestCpu, bestWall);
    Console.WriteLine();
}

static void PrintResult(string name, double cpuSec, double wallSec)
{
    var cpuMs = cpuSec * 1000.0;
    var wallMs = wallSec * 1000.0;
    var ratio = cpuSec > 0 ? wallSec / cpuSec : double.NaN;
    Console.WriteLine($"  {name,-12} CPU {cpuMs,10:F3} ms | wall {wallMs,10:F3} ms | wall/CPU {ratio,6:F2}x");
}

// ---------------------------------------------------------------- helpers

static string FindRepoRoot()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir != null)
    {
        if (Directory.Exists(Path.Combine(dir.FullName, "NFMWorld.Library", "data", "library")))
        {
            return dir.FullName;
        }
        dir = dir.Parent;
    }
    throw new DirectoryNotFoundException("repo root not found (NFMWorld.Library/data/library)");
}

static int ParseInt(string[] args, int index, int fallback)
{
    if (args.Length > index && int.TryParse(args[index], out var v))
    {
        return v;
    }
    return fallback;
}
