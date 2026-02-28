using System;
using System.Runtime.CompilerServices;

namespace LibmpvIptvClient.Diagnostics
{
    public enum LogLevel
    {
        Info,
        Warning,
        Error
    }

    public static class Logger
    {
        public static event Action<string>? OnMessage;

        public static void Log(string message, LogLevel level = LogLevel.Info, [CallerMemberName] string caller = "", [CallerFilePath] string filePath = "")
        {
            var fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
            var prefix = level switch
            {
                LogLevel.Info => "INFO",
                LogLevel.Warning => "WARN",
                LogLevel.Error => "FAIL",
                _ => "INFO"
            };
            
            // Format: [Time][Level][Class.Method] Message
            OnMessage?.Invoke($"[{DateTime.Now:HH:mm:ss.fff}][{prefix}][{fileName}.{caller}] {message}");
        }

        public static void Info(string message, [CallerMemberName] string caller = "", [CallerFilePath] string filePath = "") 
            => Log(message, LogLevel.Info, caller, filePath);
            
        public static void Warn(string message, [CallerMemberName] string caller = "", [CallerFilePath] string filePath = "") 
            => Log(message, LogLevel.Warning, caller, filePath);
            
        public static void Error(string message, [CallerMemberName] string caller = "", [CallerFilePath] string filePath = "") 
            => Log(message, LogLevel.Error, caller, filePath);
    }
}
