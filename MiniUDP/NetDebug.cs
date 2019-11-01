using System;
using System.Diagnostics;

namespace MiniUDP
{
    public interface INetDebugLogger
    {
        void LogMessage(object message);
        void LogWarning(object message);
        void LogError(object message);
    }

    internal class NetConsoleLogger : INetDebugLogger
    {
        public void LogError(object message)
        {
            Log("ERROR: " + message, ConsoleColor.Red);
        }

        public void LogWarning(object message)
        {
            Log("WARNING: " + message, ConsoleColor.Yellow);
        }

        public void LogMessage(object message)
        {
            Log("INFO: " + message, ConsoleColor.Gray);
        }

        private static void Log(object message, ConsoleColor color)
        {
            ConsoleColor current = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = current;
        }
    }

    public static class NetDebug
    {
        public static INetDebugLogger Logger = new NetConsoleLogger();

        [Conditional("DEBUG")]
        public static void LogMessage(object message)
        {
            if (Logger != null)
            {
                lock (Logger)
                {
                    Logger.LogMessage(message);
                }
            }
        }

        [Conditional("DEBUG")]
        public static void LogWarning(object message)
        {
            if (Logger != null)
            {
                lock (Logger)
                {
                    Logger.LogWarning(message);
                }
            }
        }

        [Conditional("DEBUG")]
        public static void LogError(object message)
        {
            if (Logger != null)
            {
                lock (Logger)
                {
                    Logger.LogError(message);
                }
            }
        }

        [Conditional("DEBUG")]
        public static void Assert(bool condition)
        {
            if (condition == false)
            {
                LogError("Assert Failed!");
            }
        }

        [Conditional("DEBUG")]
        public static void Assert(bool condition, object message)
        {
            if (condition == false)
            {
                LogError("Assert Failed: " + message);
            }
        }
    }
}
