using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SmolAmeArchipelagoMod.Source
{
    [HarmonyPatch(typeof(HUDScript), "Start")]
    static class HUDScript_Start_Patch
    {
        static void Postfix(HUDScript __instance)
        {
            // Duplicate HUDs destroy themselves inside Start, only build on the real one
            if (HUDScript.HUD != __instance) return;

            HUD.Create(__instance);
        }
    }

    internal class Console : MonoBehaviour
    {
        void Start()
        {
            SmolAmeData.APCoinSprite = HUD.LoadSprite("APIcon.png");
            SmolAmeData.APCoinTexture = HUD.LoadTexture("APIcon.png");
            SmolAmeData.APsEmpty = HUD.CreateAPsEmpty(SmolAmeData.APCoinSprite);

            SmolAmeData.APAnims = SmolAmeData.APsEmpty.GetComponentsInChildren<Animator>();


        }
        void Update()
        {
            HUD.Flush();

            for (int i = 0; i < SmolAmeData.APAnims.Length; i++)
            {
                if (!SmolAmeData.APAnims[i].gameObject.activeSelf) SmolAmeData.APAnims[i].gameObject.SetActive(true);
                SmolAmeData.APAnims[i].SetBool("Collected", SmolAmeData.APCoins[i]);
            }
        }
    }

    internal class HUD
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

            Text = GameObject.Instantiate(source, source.transform.parent, false);
            Text.name = "InfoText";
            RectTransform rt = Text.rectTransform;

            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(-Padding.x, -Padding.y);
            rt.sizeDelta = BoxSize;

            Text.enableAutoSizing = false;
            Text.enableWordWrapping = true;
            Text.overflowMode = TextOverflowModes.Overflow;

            Text.alignment = TextAlignmentOptions.TopRight;
            Text.raycastTarget = false;           
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

        public static Sprite LoadSprite(string fileName)
        {
            // Looks for the PNG next to your plugin .dll
            string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string path = Path.Combine(dir, fileName);

            if (!File.Exists(path))
            {
                Debug.LogError($"Image not found: {path}");
                return null;
            }

            byte[] bytes = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(bytes))   // resizes the texture to the PNG's size
            {
                Debug.LogError("Failed to load PNG data.");
                return null;
            }

            Sprite sprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),   // pivot
                100f);                      // pixels per unit

            // Stop Unity from unloading these when scenes change
            tex.hideFlags = HideFlags.DontUnloadUnusedAsset;
            sprite.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return sprite;
        }

        public static Texture2D LoadTexture(string fileName)
        {
            // Same idea as your HUD.LoadSprite: PNG sits next to the plugin .dll
            string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string path = Path.Combine(dir, fileName);

            if (!File.Exists(path))
            {
                Debug.LogError($"Image not found: {path}");
                return null;
            }

            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(File.ReadAllBytes(path)))
            {
                Debug.LogError("Failed to load PNG data.");
                return null;
            }

            tex.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return tex;
        }

        public static void ReplaceTexture(Component saved, Texture2D tex)
        {
            // true = also search inactive children
            Renderer rend = saved.GetComponentInChildren<Renderer>(true);
            if (rend == null || tex == null) return;

            Material mat = rend.material;   // per-object instance copy

            mat.SetTexture("_MainTex", tex);        // Albedo
            mat.SetTexture("_EmissionMap", tex);    // Emission

            mat.EnableKeyword("_EMISSION");         // make sure emission is switched on
        }

        public static GameObject CreateAPsEmpty(Sprite apSprite)
        {
            // 1. Find the original
            GameObject bubbas = GameObject.Find("BubbasEmpty");
            if (bubbas == null)
            {
                Debug.LogWarning("BubbasEmpty not found.");
                return null;
            }

            Transform parent = bubbas.transform.parent;

            // Avoid creating it twice (e.g. if this runs again after a scene reload)
            Transform existing = parent != null ? parent.Find("APsEmpty") : null;
            if (existing != null) return null;

            // 2. Duplicate under the same parent (keeps local values since worldPositionStays = false)
            GameObject apsEmpty = GameObject.Instantiate(bubbas, parent);
            apsEmpty.name = "APsEmpty";   // removes the "(Clone)" suffix

            // 3. Local Y = twice the original
            Vector3 pos = bubbas.transform.localPosition;
            apsEmpty.transform.localPosition = new Vector3(pos.x, pos.y * 2f, pos.z);

            // 4. Swap the sprite on every Bubba grandchild
            foreach (Transform child in apsEmpty.transform)   // BubbaEmpty1, 2, 3
            {
                Transform bubba = child.Find("Bubba");
                if (bubba == null) continue;

                Image img = bubba.GetComponent<Image>();
                if (img != null)
                    img.sprite = apSprite;
            }

            return apsEmpty;
        }
    }
}
