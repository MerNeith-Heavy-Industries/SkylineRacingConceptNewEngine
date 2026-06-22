namespace WorldXaml.UI.Base;

public enum LogLevel : byte
{
    Info,
    Warning,
    Error,
    Debug
}

public static class Logging
{
    internal static void Info(string message) => ReactorConfig.LogMessage(LogLevel.Info, message);
    internal static void Warning(string message) => ReactorConfig.LogMessage(LogLevel.Warning, message);
    internal static void Error(string message) => ReactorConfig.LogMessage(LogLevel.Error, message);
    internal static void Debug(string message) => ReactorConfig.LogMessage(LogLevel.Debug, message);
    internal static void Info(object? message) => ReactorConfig.LogMessage(LogLevel.Info, message?.ToString() ?? "");
    internal static void Warning(object? message) => ReactorConfig.LogMessage(LogLevel.Warning, message?.ToString() ?? "");
    internal static void Error(object? message) => ReactorConfig.LogMessage(LogLevel.Error, message?.ToString() ?? "");
    internal static void Debug(object? message) => ReactorConfig.LogMessage(LogLevel.Debug, message?.ToString() ?? "");
}