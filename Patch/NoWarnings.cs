using BepInEx.Logging;

namespace ChunkLoader.Patch;

[HarmonyPatch]
public class NoWarnings
{
    [HarmonyPatch(typeof(ConsoleLogListener), nameof(ConsoleLogListener.LogEvent))] [HarmonyPrefix]
    private static bool NoUselessLogs_ConsoleLogListener_LogEvent(object sender, LogEventArgs eventArgs) =>
        Void(eventArgs.Data.ToString(), eventArgs.Level, eventArgs.Source.SourceName);

    [HarmonyPatch(typeof(ZLog), nameof(ZLog.Log))] [HarmonyPrefix]
    private static bool NoUselessLogs_ZLog_Log(object o) => Void(o.ToString(), LogLevel.Message, "ZLog");

    [HarmonyPatch(typeof(Terminal), nameof(Terminal.Log))] [HarmonyPrefix]
    private static bool NoUselessLogs_Terminal_Log(object obj) => Void(obj.ToString(), LogLevel.Message, "ZLog");

    private static bool Void(string log, LogLevel level, string source)
    {
        if (level == LogLevel.Fatal || level == LogLevel.Error) return true;
        if (log.Contains("Failed to find expected binary shader data in"))
            return false;

        return true;
    }
}