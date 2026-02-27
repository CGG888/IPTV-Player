using System;

namespace LibmpvIptvClient.Diagnostics
{
    public static class Logger
    {
        public static event Action<string>? OnMessage;
        public static void Log(string message)
        {
            OnMessage?.Invoke($"[{DateTime.Now:HH:mm:ss}] {message}");
        }
    }
}
