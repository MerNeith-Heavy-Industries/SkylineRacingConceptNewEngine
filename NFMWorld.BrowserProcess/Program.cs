#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using NFMWorld.UI.Cef;
using Xilium.CefGlue;
using Xilium.CefGlue.BrowserProcess.Helpers;
using Xilium.CefGlue.Common.Shared;

file static class CommandLineArgs
{
    public const string CustomScheme = "--custom-scheme";
    public const string ParentProcessId = "--parent-pid";
}

file static class CustomSchemeHelpers
{
    private static CustomScheme? DeserializeFromCommandLineValue(string value)
    {
        var strArray = value.Split('|');
        if (strArray.Length < 3)
            return null;
        Enum.TryParse(strArray[2], out CefSchemeOptions result);
        return new CustomScheme
        {
            SchemeName = strArray[0],
            DomainName = strArray[1],
            IsStandard = result.HasFlag(CefSchemeOptions.Standard),
            IsLocal = result.HasFlag(CefSchemeOptions.Local),
            IsDisplayIsolated = result.HasFlag(CefSchemeOptions.DisplayIsolated),
            IsSecure = result.HasFlag(CefSchemeOptions.Secure),
            IsCorsEnabled = result.HasFlag(CefSchemeOptions.CorsEnabled),
            IsCSPBypassing = result.HasFlag(CefSchemeOptions.CspBypassing),
            IsFetchEnabled = result.HasFlag(CefSchemeOptions.FetchEnabled)
        };
    }
    
    internal static CustomScheme[] FromCommandLineValue(string? value)
    {
        var strArray = value?.Split(';', StringSplitOptions.RemoveEmptyEntries);
        return (strArray == null
            ? []
            : strArray.Select(DeserializeFromCommandLineValue).Where(static s => s != null).ToArray())!;
    }
}

namespace Xilium.CefGlue.BrowserProcess
{
    class Program
    {
        static void Main(string[] args)
        {
#if DEBUG
            try
            {
#endif
                NativeLibsLoader.Install();

                var parentProcessId = GetArgumentValue(args, CommandLineArgs.ParentProcessId);
                if (parentProcessId != null && int.TryParse(parentProcessId, out var parentProcessIdAsInt))
                {
                    ParentProcessMonitor.StartMonitoring(parentProcessIdAsInt);
                }

                CefRuntime.Load();

                var customSchemesArg = GetArgumentValue(args, CommandLineArgs.CustomScheme);
                var customSchemes = CustomSchemeHelpers.FromCommandLineValue(customSchemesArg);
                // first argument is the path of the executable, but its ignored for now
                var mainArgs = new CefMainArgs(["BrowserProcess", ..args]);
                var exitCode = CefRuntime.ExecuteProcess(mainArgs, new NfmwCefApp(new NfmwRenderProcessHandler(), customSchemes), IntPtr.Zero);
                if (exitCode != -1)
                {
                    Environment.Exit(exitCode);
                }
#if DEBUG
            }
            catch
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    System.Diagnostics.Debugger.Launch();
                }
                throw;
            }
#endif
        }

        private static string GetArgumentValue(string?[] args, string argName)
        {
            var arg = args.FirstOrDefault(a => a?.StartsWith(argName + "=") == true);
            return arg?[(argName.Length + 1)..] ?? "";
        }
    }
}
