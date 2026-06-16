using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Maxine.Extensions;
using NFMWorldLibrary.Backend;
using NFMWorldLibrary.Backend.Gamemodes;
using NFMWorldLibrary.Files;
using NFMWorldLibrary.Gamemodes;

namespace NFMWorldLibrary;

public class UnmanagedEntryPoints
{
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
        [Description("Type name of the exception (UTF-8 bytes, null-terminated)")]
        public ErrorMessageBuffer TypeName;
        [Description("Message of the exception (UTF-8 bytes, null-terminated)")]
        public ErrorMessageBuffer Message;
        [Description("Stack trace of the exception (UTF-8 bytes, null-terminated)")]
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
        [Description("Pointer to time trial data")]
        public byte* TimeTrialData;
        [Description("Length of time trial data in bytes")]
        public int TimeTrialDataLength;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct GetTTInfoResult
    {
        [Description("Number of checkpoints in the time trial")]
        public required int CheckpointCount;
        [Description("Number of ticks in the time trial")]
        public required int TickCount;
        [Description("Version of the replay")]
        public required int ReplayVersion;
        [Description("Version of the backend")]
        public required int BackendVersion;

        [Description("Whether an error occurred during GetTTInfo")]
        public required bool HasError;
        [Description("Error information if HasError is true")]
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
            BackendGameSparker.Load();
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
                ? BackendGamemodeData.Create(Encoding.UTF8.GetString(args->StageName), stageData)
                : BackendGamemodeData.Create(Encoding.UTF8.GetString(args->StageName));

            var gamemode = new TimeTrialSimulationGamemode(new BaseGamemodeParameters()
            {
                Players =
                [
                    new PlayerParameters()
                    {
                        PlayerName = "Player",
                        CarName = Encoding.UTF8.GetString(args->Cars[0].CarName),
                        Color = new Color3(255, 0, 0),
                        IsBot = false,
                        IsClientPlayer = false
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