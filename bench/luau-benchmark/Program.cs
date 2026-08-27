using Lua;
using LuauBenchmark;
using NFMWorldLibrary.Util;

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
    case "hud":
        RunHud(host, scripts, runs);
        break;
    case "vmcore":
        RunVmcore(host, scripts, runs);
        break;
    default:
        RunFixed64(host, scripts, runs);
        RunPreact(host, scripts, runs, "preact-small", 16, true, 10000);
        RunPreact(host, scripts, runs, "preact-large", 1024, false, 100);
        RunHud(host, scripts, runs);
        RunVmcore(host, scripts, runs);
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

static void RunVmcore(BenchmarkHost host, string scripts, int runs)
{
    var path = Path.Combine(scripts, "vmcore.luau");
    const int iterations = 2000, depth = 8;
    Console.WriteLine($"vmcore : {iterations} reconciler-core walks (depth {depth}, fresh props, no C# host)");
    Console.WriteLine("  Compare: luau.exe scripts/vmcore_driver.luau");

    double bestCpu = double.MaxValue, bestWall = double.MaxValue;
    for (var r = 0; r < runs; r++)
    {
        var (cpu, wall, _) = host.RunScript(path, new LuaValue((double)iterations), new LuaValue((double)depth), new LuaValue(true));
        if (cpu < bestCpu) { bestCpu = cpu; bestWall = wall; }
    }
    Console.WriteLine($"  Lua-CSharp CPU {bestCpu * 1000,9:F3} ms | wall {bestWall * 1000,9:F3} ms");
}

static void RunHud(BenchmarkHost host, string scripts, int runs)
{
    var path = Path.Combine(scripts, "hud_render.luau");
    const int frames = 300;
    Console.WriteLine($"hud : {frames} per-frame timetrial-HUD flushes (17-node preact tree, 3 dirty components, real defer+ExecutePendingTasks path)");

    // fresh props = current racehud.luau behaviour (inline style tables -> setProperty per node)
    LuaUiHostStats.Enabled = true;
    RunHudRegime(host, path, frames, runs, fresh: true, label: "fresh (inline styles)");

    // stable props = hoist style tables to module constants (memoize the styles)
    RunHudRegime(host, path, frames, runs, fresh: false, label: "stable (hoisted styles)");
    LuaUiHostStats.Enabled = false;
    Console.WriteLine();
}

static void RunHudRegime(BenchmarkHost host, string path, int frames, int runs, bool fresh, string label)
{
    double bestCpu = double.MaxValue, bestWall = double.MaxValue;
    long setProps = 0, commits = 0, creates = 0, structures = 0;
    long diffed = 0, rendered = 0, rrCount = 0, processCount = 0;
    double hostUs = 0;
    string names = "";
    for (var r = 0; r < runs; r++)
    {
        LuaUiHostStats.Reset();
        var (cpu, wall, rets) = host.RunScript(path, new LuaValue((double)frames), new LuaValue(fresh));
        if (cpu < bestCpu)
        {
            bestCpu = cpu;
            bestWall = wall;
            setProps = LuaUiHostStats.SetPropertyCount;
            commits = LuaUiHostStats.CommitTextCount;
            creates = LuaUiHostStats.CreateInstanceCount + LuaUiHostStats.CreateTextCount;
            structures = LuaUiHostStats.StructureCount;
            hostUs = LuaUiHostStats.Us(LuaUiHostStats.SetPropertyTicks + LuaUiHostStats.CommitTextTicks);
            if (rets.Length >= 6)
            {
                if (rets[1].TryRead<double>(out var d)) diffed = (long)d;
                if (rets[2].TryRead<double>(out var r2)) rendered = (long)r2;
                if (rets[3].TryRead<double>(out var r3)) rrCount = (long)r3;
                if (rets[4].TryRead<double>(out var r4)) processCount = (long)r4;
                if (rets[5].TryRead<LuaTable>(out var nameTable))
                {
                    foreach (var (k, v) in nameTable)
                    {
                        if (k.TryRead<string>(out var key) && v.TryRead<double>(out var val))
                        {
                            names += $"{key}={val} ";
                        }
                    }
                }
                if (rets.Length >= 7 && rets[6].TryRead<LuaTable>(out var detailTable))
                {
                    foreach (var (k, v) in detailTable)
                    {
                        if (k.TryRead<string>(out var key) && v.TryRead<LuaTable>(out var det))
                        {
                            string depth = "", parent = "";
                            foreach (var (dk, dv) in det)
                            {
                                if (dk.TryRead<string>(out var dk2))
                                {
                                    if (dk2 == "depth" && dv.TryRead<double>(out var dd)) depth = dd.ToString("F0");
                                    if (dk2 == "parent" && dv.TryRead<string>(out var dp)) parent = dp;
                                }
                            }
                            names += $" [{key}@d{depth} parent={parent}]";
                        }
                    }
                }
            }
        }
    }

    var usPerFrame = bestWall * 1_000_000.0 / frames;
    var luaUsPerFrame = usPerFrame - hostUs / frames;
    Console.WriteLine(
        $"  {label,-22} {usPerFrame,8:F1} us/frame | diffed {diffed / (double)frames:F1}/f rendered {rendered / (double)frames:F1}/f renderComp {rrCount / (double)frames:F1}/f process {processCount / (double)frames:F1}/f | setProp {setProps / (double)frames:F1}/f create {creates} struct {structures} | C#-host {hostUs / frames,6:F1} us/f | Lua {luaUsPerFrame,6:F1} us/f");
    Console.WriteLine($"      renderComponent by type: {names}");
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
