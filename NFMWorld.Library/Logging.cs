using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using RayTech.RayLog.MEL;
#pragma warning disable CA2254

namespace nfm_world_library;

public static class Logging
{
    public const string SentryDsn = "https://baadef1bd7ebd872a30c292477d45ed6@sentry.puppykitty.racing/1";

    public static string Release { get; } = AppDomain.CurrentDomain.GetAssemblies()
        .FirstOrDefault(ass => ass.FullName?.StartsWith("NFMWorld,") == true)
        ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
        ?.InformationalVersion ?? "NFM-World dev";

    public static readonly ILoggerFactory LoggerFactory =
        Microsoft.Extensions.Logging.LoggerFactory.Create(builder => builder
            .AddConsole(options =>
            {
                options.FormatterName = RayLogConsoleFormatter.FormatterName;
            })
            .AddConsoleFormatter<RayLogConsoleFormatter, ConsoleFormatterOptions>()
            .AddSentry(o => o.Dsn = SentryDsn)
            .AddProvider(new NfmwLoggerProvider())
        );

    private static readonly ILogger General = LoggerFactory.CreateLogger("general");

    public static void Info(string message) => General.LogInformation(message);
    public static void Warning(string message) => General.LogWarning(message);
    public static void Error(string message) => General.LogError(message);
    public static void Debug(string message) => General.LogDebug(message);
    public static void Info(object message) => General.LogInformation(message.ToString());
    public static void Warning(object message) => General.LogWarning(message.ToString());
    public static void Error(object message) => General.LogError(message.ToString());
    public static void Debug(object message) => General.LogDebug(message.ToString());
}

public class NfmwLoggerProvider : ILoggerProvider
{
    private static readonly Queue<(string message, string level)> OutputLog = new();
    
    public static IEnumerable<(string message, string level)> GetLogs()
    {
        return OutputLog;
    }

    public void Dispose()
    {
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new NfmwLogger(categoryName);
    }

    public class NfmwLogger(string categoryName) : ILogger
    {
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            string message = formatter(state, exception);
            string level = logLevel switch
            {
                LogLevel.Trace => "debug",
                LogLevel.Debug => "debug",
                LogLevel.Information => "info",
                LogLevel.Warning => "warning",
                LogLevel.Error => "error",
                LogLevel.Critical => "error",
                _ => "default"
            };
            var formattedMessage = logLevel is LogLevel.Warning or LogLevel.Error or LogLevel.Critical
                ? $"[{categoryName.ToUpperInvariant()}] {message}"
                : message;
            OutputLog.Enqueue((formattedMessage, level));
            if (OutputLog.Count > 100) OutputLog.Dequeue(); // Limit log size
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }
    }
}