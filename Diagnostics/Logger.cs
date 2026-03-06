using System;
using System.Runtime.CompilerServices;

namespace LibmpvIptvClient.Diagnostics
{
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        Fatal = 4
    }

    public static class Logger
    {
        public static event Action<string>? OnMessage;
        public static event Action<LogLevel, string>? OnMessageLeveled;
        public static LogLevel MinimumLevel { get; set; } = LogLevel.Info;

        public static void Log(string message, LogLevel level = LogLevel.Info, [CallerMemberName] string caller = "", [CallerFilePath] string filePath = "")
        {
            if (level < MinimumLevel) return;
            var fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
            var prefix = level switch
            {
                LogLevel.Debug => "DEBUG",
                LogLevel.Info => "INFO",
                LogLevel.Warning => "WARN",
                LogLevel.Error => "ERROR",
                LogLevel.Fatal => "FATAL",
                _ => "INFO"
            };
            
            // Format: [Time][Level][Class.Method] Message
            var msg = $"[{DateTime.Now:HH:mm:ss.fff}][{prefix}][{fileName}.{caller}] {message}";
            OnMessage?.Invoke(msg);
            OnMessageLeveled?.Invoke(level, msg);
        }

        public static void Debug(string message, [CallerMemberName] string caller = "", [CallerFilePath] string filePath = "") 
            => Log(message, LogLevel.Debug, caller, filePath);
        public static void Info(string message, [CallerMemberName] string caller = "", [CallerFilePath] string filePath = "") 
            => Log(message, LogLevel.Info, caller, filePath);
            
        public static void Warn(string message, [CallerMemberName] string caller = "", [CallerFilePath] string filePath = "") 
            => Log(message, LogLevel.Warning, caller, filePath);
            
        public static void Error(string message, [CallerMemberName] string caller = "", [CallerFilePath] string filePath = "") 
            => Log(message, LogLevel.Error, caller, filePath);
        
        public static void Fatal(string message, [CallerMemberName] string caller = "", [CallerFilePath] string filePath = "") 
            => Log(message, LogLevel.Fatal, caller, filePath);
    }
}
