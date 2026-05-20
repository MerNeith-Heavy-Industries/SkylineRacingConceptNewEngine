using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Maxine.Extensions;
using Maxine.VFS;
using Microsoft.Extensions.Logging;
using NFMWorldLibrary.Backend;
using NFMWorldLibrary.Backend.Gamemodes;
using NFMWorldLibrary.Files;
using NFMWorldLibrary.Rad;
using NFMWorldLibrary.Util;

namespace NFMWorldLibrary;

public static class BackendGameSparker
{
    public static Dictionary<Collection, UnlimitedArray<Rad3d>> cars = new();
    public static UnlimitedArray<Rad3d> stage_parts = [];
    public static UnlimitedArray<Rad3d> vendor_stage_parts = [];
    public static UnlimitedArray<Rad3d> src_stage_parts = [];
    public static UnlimitedArray<Rad3d> user_stage_parts = [];
    public static Dictionary<string, (int Index, Rad3d Rad)> dynamic_models = new();
    public static Rad3d error_mesh;

    public static readonly string[] CarRads =
    [
        "2000tornados", "formula7", "canyenaro", "lescrab", "nimi", "maxrevenge", "leadoxide", "koolkat", "drifter",
        "policecops", "mustang", "king", "audir8", "masheen", "radicalone", "drmonster"
    ];

    public static readonly string[] StageRads =
    [
        "road", "froad", "twister2", "twister1", "turn", "offroad", "bumproad", "offturn", "nroad", "nturn",
        "roblend", "noblend", "rnblend", "roadend", "offroadend", "hpground", "ramp30", "cramp35", "dramp15",
        "dhilo15", "slide10", "takeoff", "sramp22", "offbump", "offramp", "sofframp", "halfpipe", "spikes", "rail",
        "thewall", "checkpoint", "fixpoint", "offcheckpoint", "sideoff", "bsideoff", "uprise", //45
        "riseroad", "sroad", "soffroad", "tside", "launchpad", "thenet", "speedramp", "offhill", "slider", "uphill",
        "roll1", "roll2", "roll3", "roll4", "roll5", "roll6", "opile1", "opile2", "aircheckpoint",
        "tree1", "tree2", "tree3", "tree4",  "tree5", "tree6", "tree7", "tree8", "cac1", "cac2", "cac3",
        "8sroad", "8soffroad"
    ];

    private static bool _loaded;

    public static void Load()
    {
        if (_loaded)
            return;
        _loaded = true;
        
        SentrySdk.Init(options =>
        {
            // A Sentry Data Source Name (DSN) is required.
            // See https://docs.sentry.io/product/sentry-basics/dsn-explainer/
            // You can set it in the SENTRY_DSN environment variable, or you can set it in code here.
            options.Dsn = Logging.SentryDsn;

            // When debug is enabled, the Sentry client will emit detailed debugging information to the console.
            // This might be helpful, or might interfere with the normal operation of your application.
            // We enable it here for demonstration purposes when first trying Sentry.
            // You shouldn't do this in your applications unless you're troubleshooting issues with Sentry.
            options.Debug = false;

            // This option is recommended. It enables Sentry's "Release Health" feature.
            options.AutoSessionTracking = true;

            // Set TracesSampleRate to 1.0 to capture 100%
            // of transactions for tracing.
            // We recommend adjusting this value in production.
            options.TracesSampleRate = 0.05;
            
            // Enable logs to be sent to Sentry
            options.EnableLogs = true;
            
            // Try get NFMWorld assembly version first
            options.Release = Logging.Release;
        });
        SentrySdk.CaptureMessage("Hello world", SentryLevel.Debug);
        
        var realFs = new RelativeFileSystem(AppDomain.CurrentDomain.BaseDirectory);
        var realFs2 = new RelativeFileSystem(Directory.GetCurrentDirectory());
        VFS.MountNewFileTarget(realFs2);
        
        // VFS.MountFileSystem(new HttpFileSystem());
        VFS.MountFileSystem(realFs);
        VFS.MountFileSystem(realFs2);
        var modsFolder = Path.Combine(Directory.GetCurrentDirectory(), "mods");
        if (Directory.Exists(modsFolder))
            VFS.MountFileSystem(new RelativeFileSystem(modsFolder));

        cars.Add(Collection.NFMM, []);
        FileUtil.LoadFiles("./data/models/nfmm/cars", CarRads, (ais, id, fileName) =>
        {
            cars[Collection.NFMM][id] = RadParser.ParseRad(Encoding.UTF8.GetString(ais)) with
            {
                FileName = "nfmm/" + fileName
            };
        });

        FileUtil.LoadFiles("./data/models/nfmm/stage", StageRads, (ais, id, fileName) =>
        {
            stage_parts[id] = RadParser.ParseRad(Encoding.UTF8.GetString(ais)) with
            {
                FileName = "nfmm/" + fileName
            };
        });

        cars.Add(Collection.World, []);
        FileUtil.LoadFiles("./data/models/world/cars", (ais, fileName) =>
        {
            cars[Collection.World].Add(RadParser.ParseRad(Encoding.UTF8.GetString(ais)) with
            {
                FileName = "world/" + fileName
            });
        });

        cars.Add(Collection.Elo, []);
        FileUtil.LoadFiles("./data/models/elo/cars", (ais, fileName) =>
        {
            cars[Collection.Elo].Add(RadParser.ParseRad(Encoding.UTF8.GetString(ais)) with
            {
                FileName = "elo/" + fileName
            });
        });

        cars.Add(Collection.Football, []);
        FileUtil.LoadFiles("./data/models/football/cars", (ais, fileName) =>
        {
            cars[Collection.Football].Add(RadParser.ParseRad(Encoding.UTF8.GetString(ais)) with
            {
                FileName = "football/" + fileName
            });
        });

        cars.Add(Collection.Skyline, []);
        FileUtil.LoadFiles("./data/models/src/cars", (ais, fileName) =>
        {
            cars[Collection.Skyline].Add(RadParser.ParseRad(Encoding.UTF8.GetString(ais)) with
            {
                FileName = "src/" + fileName
            });
        });

        FileUtil.LoadFiles("./data/models/world/stage", (ais, fileName) =>
        {
            vendor_stage_parts.Add(RadParser.ParseRad(Encoding.UTF8.GetString(ais)) with
            {
                FileName = "world/" + fileName
            });
        });

        FileUtil.LoadFiles("./data/models/football/stage", (ais, fileName) =>
        {
            vendor_stage_parts.Add(RadParser.ParseRad(Encoding.UTF8.GetString(ais)) with
            {
                FileName = "football/" + fileName
            });
        });

        string[] srcStageParts =
        [
            "[43] tjunction",
            "[44] xjunction",
            "[45] highwaystraight",
            "[46] highwayturn",
            "[47] sewer_dual_ramp",
            "[48] tollbooth1",
            "[49] tollbooth2",
            "[50] highwayescape",
            "[51] carrierjump",
            "[52] dockstraight",
            "[53] dockjump1",
            "[54] dockjump2",
            "[55] dockinside",
            "[56] dockoutside",
            "[57] canyonstraight",
            "[58] canyonroadend1",
            "[59] canyonbridge",
            "[60] canyonedgeinside",
            "[61] canyonedgeoutside",
            "[62] canyontop",
            "[63] canyoncheckpt",
            "[64] canyonntrance1",
            "[65] tunnelstraight",
            "[66] tunnelturn",
            "[67] buildingstraight",
            "[68] buildingoverpass",
            "[69] supeshop",
            "[70] turbo",
            "[71] understraight",
            "[72] underturn",
            "[73] underntrance",
            "[74] subwaystraight",
            "[75] subwaytrain",
            "[76] subwayturn",
            "[77] substation",
            "[78] subarea2",
            "[79] subarea3",
            "[80] subentrance",
            "[81] subarea1",
            "[82] subtrain",
            "[83] subtrain2",
            "[84] house0",
            "[85] house1",
            "[86] house2",
            "[87] condo1",
            "[88] condo2",
            "[89] skyscraper1",
            "[90] skyscraper2",
            "[91] skyscraper3",
            "[92] warehouse1",
            "[93] citywall1",
            "[94] chainfence",
            "[95] desertwall",
            "[96] desertwallturn",
            "[97] waterroute",
            "[98] sewerjump",
            "[99] tree",
            "[100] streetlamp",
            "[101] hatchback",
            "[102] sedan",
            "[103] sports",
            "[104] docksXhighway",
            "[105] super",
            "[106] dapigs",
            "[107] rdblkpigs",
            "[108] supapigs",
            "[109] Rdblkgate1",
            "[110] rdblkbarrier1",
            "[111] spikestrip",
            "[112] suspensionbridge",
            "[113] roofjump",
            "[114] urbanramp",
            "[115] urbanstraight",
            "[116] urbanX",
            "[117] canyonroadend2",
            "[118] large dumpstaa",
            "[119] urbanT",
            "[120] urbanTurn",
            "[121] urbanEnd",
            "[122] tunnelramp",
            "[123] grandstand",
            "[124] canyonntrance2",
            "[125] rocktotunnel",
            "[126] dirtWaterX",
            "[127] docksTurban",
            "[128] urbanlowramp",
            "[129] waterwall",
            "[130] canyonwall",
            "[131] classicwall",
            "[132] gpwall",
            "[133] forrestwall",
            "[134] advert1",
            "[135] UrbanXtunnel",
            "[136] highwayjump",
            "[137] sewerend",
            "[138] sewerturn",
            "[139] snowbridge",
            "[140] snowcheckpt",
            "[141] snowedgeinside",
            "[142] snowedgeoutside",
            "[143] snowntrance1",
            "[144] snowntrance2",
            "[145] snowroadend1",
            "[146] snowroadend2",
            "[147] snowstraight",
            "[148] snowtop",
            "[149] snow2highway",
            "[150] sideoff",
            "[151] bsideoff",
            "[152] uprise",
            "[153] riseroad",
            "[154] sroad",
            "[155] soffroad",
            "[156] tside",
            "[157] launchpad",
            "[158] thenet",
            "[159] speedramp",
            "[160] offhill",
            "[161] slider",
            "[162] uphill",
            "[163] roll1",
            "[164] roll2",
            "[165] roll3",
            "[166] roll4",
            "[167] roll5",
            "[168] roll6",
            "[169] opile1",
            "[170] opile2",
            "[171] aircheckpoint",
            "[172] tree1",
            "[173] tree2",
            "[174] tree3",
            "[175] tree4",
            "[176] tree5",
            "[177] tree6",
            "[178] tree7",
            "[179] tree8",
            "[180] cac1",
            "[181] cac2",
            "[182] cac3",
            "[183] 8sroad",
            "[184] 8soffroad",
            "[185] ufo",
            "[186] ai_node",
            "[187] base1",
            "[188] flag1",
            "[189] guideflag1",
            "[190] pedflag1",
            "[191] base2",
            "[192] flag2",
            "[193] guideflag2",
            "[194] pedflag2",
            "[195] dickwall",
            "[196] futbowlgoal",
            "[197] soccer",
            "[198] cash",
            "[199] krystal",
            "[200] perfboost",
            "[201] highwayxtunnel",
            "[202] highwayxurban",
            "[203] tunhwyrmp",
            "[204] deserthwy",
            "[205] canyonramp",
            "[206] desertxurban",
            "[207] docksXtunnel",
            "[208] docksXhighwayM",
            "[209] docksXtunnelM",
            "[210] GP_road",
            "[211] GP_race_start_light",
            "[212] GP_twister2",
            "[213] GP_twister1",
            "[214] GP_turn",
            "[215] GP_pit_exit_01",
            "[216] GP_pit_exit_02",
            "[217] GP_pitlane_s_01",
            "[218] GP_pitlane_s_02",
            "[219] GP_roadend",
            "[220] uni_start",
            "[221] fuelpumps",
        ];

        foreach (var file in srcStageParts)
        {
            var path = VFS.Path.Combine("./data/models/src/stage", file + ".rad");
            
            src_stage_parts.Add(RadParser.ParseRad(Encoding.UTF8.GetString(VFS.ReadAllBytes(path))) with
            {
                FileName = "src/" + path
            });
        }

        cars.Add(Collection.User, []);
        FileUtil.LoadFiles("./data/models/user/cars", (ais, fileName) =>
        {
            try
            {
                cars[Collection.User].Add(RadParser.ParseRad(Encoding.UTF8.GetString(ais)) with
                {
                    FileName = "user/" + fileName
                });
            }
            catch (Exception ex)
            {
                Logging.Info($"Error loading user car '{fileName}': {ex.Message}\n{ex.StackTrace}");
            }
        });

        FileUtil.LoadFiles("./data/models/user/stage", (ais, fileName) =>
        {
            try
            {
                user_stage_parts.Add(RadParser.ParseRad(Encoding.UTF8.GetString(ais)) with
                {
                    FileName = "user/" + fileName
                });
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureEvent(new SentryEvent(ex)
                {
                    Message = $"Error loading user stage part '{fileName}'"
                });
                Logging.Info($"Error loading user stage part '{fileName}': {ex.Message}\n{ex.StackTrace}");
            }
        });

        error_mesh = RadParser.ParseRad(Encoding.UTF8.GetString(VFS.ReadAllBytes("./data/models/error.rad"))) with
        {
            FileName = "error.rad"
        };
        
        for (var i = 0; i < StageRads.Length; i++) {
            if (stage_parts[i] == null) {
                SentrySdk.CaptureMessage("No valid ContO (Stage Part) has been assigned to ID " + i + " (" + StageRads[i] + ")", SentryLevel.Error);
                throw new Exception("No valid ContO (Stage Part) has been assigned to ID " + i + " (" + StageRads[i] + ")");
            }
        }
        for (var i = 0; i < CarRads.Length; i++) {
            if (cars[Collection.NFMM][i] == null)
            {
                SentrySdk.CaptureMessage("No valid ContO (Vehicle) has been assigned to ID " + i + " (" + StageRads[i] + ")", SentryLevel.Error);
                throw new Exception("No valid ContO (Vehicle) has been assigned to ID " + i + " (" + StageRads[i] + ")");
            }
        }
    }

    private static IntPtr ImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        static string GetPlatformName()
        {
            if (OperatingSystem.IsWindows())
            {
                return "windows";
            }

            if (OperatingSystem.IsMacOS())
            {
                return  "osx";
            }

            if (OperatingSystem.IsLinux())
            {
                return "linux";
            }

            if (OperatingSystem.IsFreeBSD())
            {
                return "freebsd";
            }

            if (OperatingSystem.IsAndroid())
            {
                return "android";
            }

            // What is this platform??
            return "unknown";
        }

        if (OperatingSystem.IsIOS() || OperatingSystem.IsTvOS())
        {
            return NativeLibrary.GetMainProgramHandle(); // statically linked
        }

        string os = GetPlatformName();
        string cpu = RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
        string wordsize = (IntPtr.Size * 8).ToString();
        
#if DEBUG
        string debugLibrarySuffix = "d";
#else
        string debugLibrarySuffix = System.Diagnostics.Debugger.IsAttached ? "d" : string.Empty;
#endif

        if (libraryName == "Kernel32.dll")
        {
            return NativeLibrary.Load("kernel32.dll", assembly, searchPath);
        }

        var newLibraryName = libraryName switch
        {
            _ => os switch
            {
                "windows" => $"{libraryName}.dll",
                "osx" => $"lib{libraryName}.dylib",
                "linux" or "freebsd" or "netbsd" => $"lib{libraryName}.so",
                _ => throw new PlatformNotSupportedException($"Unsupported platform: {os}, please update {nameof(ImportResolver)}")
            }
        };
        
        var dir = os switch
        {
            "windows" => cpu switch
            {
                "arm64" or "armv8" or "armv8-a" or "aarch64" or "arm64-v8a" => "arm64",
                "x64" or "x86_64" or "amd64" => "x64",
                "x86" or "x86_32" or "i386" => "x86",
                _ => throw new PlatformNotSupportedException($"Unsupported CPU architecture: {cpu}, please update {nameof(ImportResolver)}")
            },
            "osx" => "osx",
            "linux" or "freebsd" or "netbsd" => cpu switch
            {
                "arm32" or "armv7" or "aarch32" or "armeabi-v7a" => "libarmhf",
                "arm64" or "armv8" or "armv8-a" or "aarch64" or "arm64-v8a" => "libaarch64",
                "x64" or "x86_64" or "amd64" => "lib64",
                "x86" or "x86_32" or "i386" => "lib32",
                _ => throw new PlatformNotSupportedException($"Unsupported CPU architecture: {cpu}, please update {nameof(ImportResolver)}")
            },
            "android" => cpu switch
            {
                "arm32" or "armv7" or "aarch32" or "armeabi-v7a" => "android-armeabi-v7a",
                "arm64" or "armv8" or "armv8-a" or "aarch64" or "arm64-v8a" => "android-arm64-v8a",
                "x64" or "x86_64" or "amd64" => "android-x86_64",
                "x86" or "x86_32" or "i386" => "android-x86",
                _ => throw new PlatformNotSupportedException($"Unsupported CPU architecture: {cpu}, please update {nameof(ImportResolver)}")
            },
            _ => throw new PlatformNotSupportedException($"Unsupported platform: {os}, please update {nameof(ImportResolver)}")
        };
        
        return NativeLibrary.Load($"libs/{dir}/{newLibraryName}");
    }

    public static (int Id, Rad3d? Rad) GetCar(string name)
    {
        var total = 0;
        foreach (var t in cars.Values)
        {
            foreach (var car in t)
            {
                if (string.Equals(car.FileName, name, StringComparison.OrdinalIgnoreCase))
                {
                    return (total, car);
                }

                total++;
            }
        }

        if (Path.IsPathRooted(name))
        {
            if (dynamic_models.TryGetValue(name, out var dynRad))
            {
                return dynRad;
            }
            try
            {
                total += dynamic_models.Count;
                var rad = RadParser.ParseRad(System.IO.File.ReadAllText(name)) with
                {
                    FileName = name
                };
                return dynamic_models[name] = (total, rad);
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureEvent(new SentryEvent(ex)
                {
                    Message = $"Error loading dynamic model '{name}'"
                });
                Logging.Info($"Error loading dynamic model '{name}': {ex.Message}\n{ex.StackTrace}");
            }
        }

        SentrySdk.CaptureMessage("No results for GetCar: " + name, SentryLevel.Warning);
        Logging.Info("No results for GetCar: " + name);
        return (-1, null!);
    }

    public static (int Id, Rad3d? Rad) GetStagePart(string name)
    {
        IReadOnlyList<Rad3d>[] arrays = [stage_parts, vendor_stage_parts, user_stage_parts];

        var total = 0;
        foreach (var t in arrays)
        {
            foreach (var part in t)
            {
                if (string.Equals(part.FileName, name, StringComparison.OrdinalIgnoreCase))
                {
                    return (total, part);
                }

                total++;
            }
        }

        if (Path.IsPathRooted(name))
        {
            if (dynamic_models.TryGetValue(name, out var dynRad))
            {
                return dynRad;
            }
            try
            {
                total += dynamic_models.Count;
                var rad = RadParser.ParseRad(System.IO.File.ReadAllText(name)) with
                {
                    FileName = name
                };
                return dynamic_models[name] = (total, rad);
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureEvent(new SentryEvent(ex)
                {
                    Message = $"Error loading dynamic model '{name}'"
                });
                Logging.Info($"Error loading dynamic model '{name}': {ex.Message}\n{ex.StackTrace}");
            }
        }

        SentrySdk.CaptureMessage("No results for GetStagePart: " + name, SentryLevel.Warning);
        Logging.Info("No results for GetStagePart: " + name);
        return (-1, null!);
    }
    
    public static string GetModelName(int index, bool forCar = false)
    {
        var models = forCar ? CarRads : StageRads;
        
        if (index >= 0 && index < models.Length)
        {
            return models[index];
        }
        
        return "";
    }
    
    [UnmanagedCallersOnly(EntryPoint = "nfmw_get_tt_info", CallConvs = [typeof(CallConvStdcall)])]
    public static unsafe GetTTInfoResult GetTTInfo(GetTTInfoArgs* args)
    {
        try
        {
            using var timeTrialMemory =
                new UnmanagedMemoryManager<byte>(args->TimeTrialData, args->TimeTrialDataLength);
            var timeTrial = SavedTimeTrial.Load(timeTrialMemory.Memory);
            if (timeTrial == null)
            {
                SentrySdk.CaptureMessage("Failed to load time trial data", SentryLevel.Error);
                throw new InvalidOperationException("Failed to load time trial data");
            }

            return new GetTTInfoResult
            {
                CheckpointCount = timeTrial.Splits.SplitTimes.Count,
                ReplayVersion = timeTrial.Version ?? 0,
                BackendVersion = SavedTimeTrial.CURRENT_VERSION,
                TickCount = timeTrial.DemoData.Ticks.Count,
                HasError = false
            };
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            return new GetTTInfoResult
            {
                CheckpointCount = -1,
                ReplayVersion = -1,
                BackendVersion = SavedTimeTrial.CURRENT_VERSION,
                TickCount = -1,
                HasError = true,
                Exception = NativeException.FromException(ex)
            };
        }
    }
    [InlineArray(16384)]
    public struct ErrorBuffer
    {
        public byte Data;
        public Span<byte> AsSpan()
        {
            unsafe
            {
                fixed (byte* ptr = &Data)
                {
                    return new Span<byte>(ptr, 16384);
                }
            }
        }
    }
    [InlineArray(1024)]
    public struct ErrorMessageBuffer
    {
        public byte Data;
        public Span<byte> AsSpan()
        {
            unsafe
            {
                fixed (byte* ptr = &Data)
                {
                    return new Span<byte>(ptr, 1024);
                }
            }
        }
    }
        
    [StructLayout(LayoutKind.Sequential)]
    public struct NativeException
    {
        public ErrorMessageBuffer TypeName;
        public ErrorMessageBuffer Message;
        public ErrorBuffer StackTrace;
            
        public static NativeException FromException(Exception ex)
        {
            var typeNameBytes = Encoding.UTF8.GetBytes(ex.GetType().FullName ?? "UnknownException");
            var messageBytes = Encoding.UTF8.GetBytes(ex.Message);
            var stackTraceBytes = Encoding.UTF8.GetBytes(ex.StackTrace ?? "");

            var nativeEx = new NativeException();
            typeNameBytes.AsSpan(0, Math.Min(typeNameBytes.Length, 1024)).CopyTo(nativeEx.TypeName.AsSpan());
            messageBytes.AsSpan(0, Math.Min(messageBytes.Length, 1024)).CopyTo(nativeEx.Message.AsSpan());
            stackTraceBytes.AsSpan(0, Math.Min(stackTraceBytes.Length, 16384)).CopyTo(nativeEx.StackTrace.AsSpan());
                
            return nativeEx;
        }
    }


    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct GetTTInfoArgs
    {
        // Pointer to time trial data
        public byte* TimeTrialData;
        // Length of time trial data
        public int TimeTrialDataLength;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct GetTTInfoResult
    {
        // Number of checkpoints in the time trial
        public required int CheckpointCount;
        public required int TickCount;
        public required int ReplayVersion;
        public required int BackendVersion;

        // Whether an error occurred
        public required bool HasError;
        // Error information
        public NativeException Exception;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LoadResult
    {
        // Whether an error occurred
        public required bool HasError;
        // Error information
        public NativeException Exception;
    }

    /// <summary>
    /// Loads the backend.
    /// </summary>
    /// <returns></returns>
    [UnmanagedCallersOnly(EntryPoint = "nfmw_load", CallConvs = [typeof(CallConvStdcall)])]
    public static unsafe LoadResult LoadUnmanaged()
    {
        try
        {
            Load();
            return new LoadResult
            {
                HasError = false
            };
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            return new LoadResult
            {
                HasError = true,
                Exception = NativeException.FromException(ex)
            };
        }
    }

    /// <summary>
    /// Simulates a time trial to completion with a limit of 100M ticks. Returns the number of elapsed ticks, or -1 on
    /// timeout.
    /// </summary>
    /// <param name="args">The args</param>
    /// <returns></returns>
    [UnmanagedCallersOnly(EntryPoint = "nfmw_simulate_tt", CallConvs = [typeof(CallConvStdcall)])]
    public static unsafe SimulateTimeTrialResult SimulateTimeTrial(SimulateTimeTrialArgs* args)
    {
        try
        {
            using var timeTrialMemory =
                new UnmanagedMemoryManager<byte>(args->TimeTrialData, args->TimeTrialDataLength);
            var timeTrial = SavedTimeTrial.Load(timeTrialMemory.Memory);

            var simulator = timeTrial.StageData is {} stageData
                ? BackendRaceValues.Create(Encoding.UTF8.GetString(args->StageName), stageData)
                : BackendRaceValues.Create(Encoding.UTF8.GetString(args->StageName));

            var gamemode = new TimeTrialSimulationGamemode(new BaseGamemodeParameters()
            {
                PlayerCarIndex = 0,
                Players =
                [
                    new PlayerParameters()
                    {
                        PlayerName = "Player",
                        CarName = Encoding.UTF8.GetString(args->Cars[0].CarName),
                        Color = new Color3(255, 0, 0),
                        IsBot = false
                    }
                ]
            }, simulator, timeTrial);

            return new SimulateTimeTrialResult
            {
                ElapsedTicks = gamemode.SimulateToCompletion(timeTrial.DemoData.Ticks.Count + 500) ?? -1,
                ExpectedTicks = timeTrial.DemoData.Ticks.Count,
                HasError = false
            };
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            return new SimulateTimeTrialResult
            {
                ElapsedTicks = -1,
                ExpectedTicks = -1,
                HasError = true,
                Exception = NativeException.FromException(ex)
            };
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SimulateTimeTrialResult
    {
        // The result code: number of ticks elapsed, or -1 on timeout or error
        public required int ElapsedTicks;
        // Number of input ticks in the replay
        public required int ExpectedTicks;

        // Whether an error occurred
        public required bool HasError;
        // Error information
        public NativeException Exception;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct SimulateTimeTrialArgs
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct CarInfoUnmanaged
        {
            // Pointer to UTF-8 encoded car name, null-terminated
            public byte* CarName;
        }

        // Pointer to UTF-8 encoded stage name, null-terminated
        public byte* StageName;
        
        // Pointer to array of CarInfoUnmanaged
        public CarInfoUnmanaged* Cars;
        // Number of cars
        public int CarCount;
        
        // Pointer to time trial data
        public byte* TimeTrialData;
        // Length of time trial data
        public int TimeTrialDataLength;
    }
}