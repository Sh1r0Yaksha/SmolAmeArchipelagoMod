using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace SmolAmeArchipelagoMod.Source
{
    [HarmonyPatch(typeof(HUDScript), "Start")]
    static class HUDScript_Start_Patch
    {
        static void Postfix(HUDScript __instance)
        {
            // Duplicate HUDs destroy themselves inside Start, only build on the real one
            if (HUDScript.HUD != __instance) return;

            InfoHUD.Create(__instance);
        }
    }

    internal class Console : MonoBehaviour
    {
        void Update()
        {
            InfoHUD.Flush();
        }
    }

    internal class InfoHUD
    {
        // Size of the console box. The height decides how many lines are visible.
        static readonly Vector2 BoxSize = new Vector2(500, 100);
        // Distance in from the top right corner of the screen.
        static readonly Vector2 Padding = new Vector2(10, 10);

        public static TextMeshProUGUI Text;

        static readonly List<string> lines = new List<string>();
        static readonly Queue<string> pending = new Queue<string>();

        // One log file per game session: <game folder>/BepInEx/InfoHudLogs/InfoHud_<datetime>.txt
        static string logPath;
        static bool fileLoggingFailed;


        public static void Create(HUDScript hud)
        {
            TextMeshProUGUI source = hud.versionText;

            // Clone the version text so font, size, colour and material all match.
            // Same parent = same canvas, so scaling behaves the same as the rest of the HUD.
            Text = GameObject.Instantiate(source, source.transform.parent, false);
            Text.name = "InfoText";
            // Anchor + pivot at (1, 1) = top right corner of the parent.
            RectTransform rt = Text.rectTransform;

            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(-Padding.x, -Padding.y);
            rt.sizeDelta = BoxSize;

            // Needs to wrap inside the box and never shrink, so the height check below is reliable.
            Text.enableAutoSizing = false;
            Text.enableWordWrapping = true;
            Text.overflowMode = TextOverflowModes.Overflow;

            // TopRight keeps the look of the version text. Use TopLeft for a more typical console.
            Text.alignment = TextAlignmentOptions.TopRight;
            Text.raycastTarget = false;                    // don't block touch/mouse input to the buttons
            Text.text = "";
        }

        public static void AddLine(string s)
        {
            lock (pending) pending.Enqueue(s);
        }

        public static void Set(string s)
        {
            AddLine(s);
        }

        // Main thread only, called every frame from the plugin's Update().
        public static void Flush()
        {
            List<string> batch;
            lock (pending)
            {
                if (pending.Count == 0) return;
                batch = new List<string>(pending);
                pending.Clear();
            }

            // The file gets every line, even ones that scroll off the screen
            // or that arrive before the HUD exists.
            WriteToFile(batch);

            lines.AddRange(batch);

            if (Text != null) TrimToFit();
        }


        // Drops the oldest lines until everything left fits inside the box.
        static void TrimToFit()
        {
            float maxHeight = Text.rectTransform.rect.height;

            while (true)
            {
                Text.text = string.Join("\n", lines.ToArray());
                Text.ForceMeshUpdate();

                if (lines.Count <= 1 || Text.preferredHeight <= maxHeight) break;

                lines.RemoveAt(0);
            }
        }
        // Appends the new lines to the session's log file, one write per frame at most.
        static void WriteToFile(List<string> batch)
        {
            if (fileLoggingFailed) return;

            try
            {
                if (logPath == null)
                {
                    string dir = Path.Combine(Paths.BepInExRootPath, "InfoHudLogs");
                    Directory.CreateDirectory(dir);
                    logPath = Path.Combine(dir, "InfoHud_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".txt");
                }

                string stamp = DateTime.Now.ToString("HH:mm:ss");
                StringBuilder sb = new StringBuilder();
                foreach (string line in batch)
                {
                    sb.Append('[').Append(stamp).Append("] ").AppendLine(line);
                }

                File.AppendAllText(logPath, sb.ToString());
            }
            catch (Exception e)
            {
                // Don't let a file problem break the HUD or spam the log every frame
                fileLoggingFailed = true;
                Debug.LogWarning("InfoHud: could not write log file, file logging disabled. " + e.Message);
            }
        }
    }
}
