using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DeadCells.Core
{
    public enum LogTopic { General, Gameplay, Combat, Enemy, Audio, UI, Physics, AI }

    /// <summary>
    /// Logging wrapper. Calls are compiled only in the Editor and in Development builds,
    /// so release builds carry no logging cost.
    /// </summary>
    public static class GameDebug
    {
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("ENABLE_LOGS")]
        public static void Log(
            string message,
            LogTopic topic = LogTopic.General,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0) =>
            UnityEngine.Debug.Log(Format(topic, message, file, member, line));

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("ENABLE_LOGS")]
        public static void LogWarning(
            string message,
            LogTopic topic = LogTopic.General,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0) =>
            UnityEngine.Debug.LogWarning(Format(topic, message, file, member, line));

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("ENABLE_LOGS")]
        public static void LogError(
            string message,
            LogTopic topic = LogTopic.General,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0) =>
            UnityEngine.Debug.LogError(Format(topic, message, file, member, line));

        private static string Format(LogTopic topic, string msg, string file, string member, int line)
        {
            var fileName = System.IO.Path.GetFileNameWithoutExtension(file);
            return $"[{topic}] {fileName}.{member}:{line} — {msg}";
        }
    }
}
