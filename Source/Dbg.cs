using BepInEx.Logging;
using System;
using System.Threading;

namespace SmolAmeArchipelagoMod.Source
{
    /// <summary>
    /// Central debug logger. Everything goes to BepInEx's LogOutput.log
    /// (and the BepInEx console window if it is enabled in BepInEx.cfg).
    ///
    /// NOTE: BepInEx does NOT write LogLevel.Debug to disk by default, so everything
    /// here uses Info/Warning/Error, which are always written.
    /// </summary>
    internal static class Dbg
    {
        // Set to false to silence the "packet received" line in SmolAmeClient.
        public static bool LogPackets = true;

        static ManualLogSource log;
        static int mainThreadId = -1;

        // Call once, from the plugin's Awake (which runs on Unity's main thread).
        public static void Init(ManualLogSource source)
        {
            log = source;
            mainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        // "main thread" or "WORKER thread #n". Used to prove which thread a callback runs on.
        public static string ThreadTag
        {
            get
            {
                int id = Thread.CurrentThread.ManagedThreadId;
                return id == mainThreadId ? "main thread" : "WORKER thread #" + id;
            }
        }

        public static void Info(string tag, string message)
        {
            Write(LogLevel.Info, tag, message);
        }

        public static void Warn(string tag, string message)
        {
            Write(LogLevel.Warning, tag, message);
        }

        public static void Error(string tag, string message, Exception e = null)
        {
            if (e != null)
                message += "\n" + e;   // ToString() includes type, message and stack trace

            Write(LogLevel.Error, tag, message);
        }

        static void Write(LogLevel level, string tag, string message)
        {
            string line = "[" + tag + "] " + message;

            if (log != null)
                log.Log(level, line);
            else
                UnityEngine.Debug.Log("[SmolAmeAP] " + line);   // fallback if Init was never reached
        }
    }
}