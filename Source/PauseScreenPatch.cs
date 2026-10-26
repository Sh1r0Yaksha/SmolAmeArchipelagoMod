using Archipelago.MultiClient.Net.Packets;
using HarmonyLib;
using SmolAmeArchipelago;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SmolAmeArchipelagoMod.Source
{
    [HarmonyPatch(typeof(PauseScreen))]
    internal static class PauseScreenPatch
    {
        public static TMP_InputField Server;
        public static TMP_InputField Port;
        public static TMP_InputField Slot;
        public static TMP_InputField Password;

        [HarmonyPrefix]
        [HarmonyPatch("Start")]
        public static void StartPrefix(PauseScreen __instance)
        {
            var PauseMenu = __instance.pauseMenu.transform;
            PauseOption template = PauseMenu.GetComponentInChildren<PauseOption>(true);

            Server = CreateClone("Server", template.gameObject, PauseMenu);
            Port = CreateClone("Port", template.gameObject, PauseMenu);
            Slot = CreateClone("Slot", template.gameObject, PauseMenu);
            Password = CreateClone("Password", template.gameObject, PauseMenu);

            GameObject Connect = GameObject.Instantiate(template.gameObject, PauseMenu);
            Connect.name = "Connect";

            PauseOption option = Connect.GetComponent<PauseOption>();
            option.methods = new UnityEngine.Events.UnityEvent();
            option.selectionStrings = new List<string>
            {
                ""
            };

            option.methods.AddListener(() =>
            {
                SmolAmeClient.OnConnectionInvoked();
            });


            option.labels = new TranslationCollection
            {
                translations = new List<Translation>
                {
                    new Translation
                    {
                        language = Language.English,
                        text = "Connect"
                    },
                    new Translation
                    {
                        language = Language.Japanese,
                        text = "TBD"
                    }
                }
            };
        }


        static TMP_InputField CreateClone(
            string name,
            GameObject original,
            Transform parent)
        {
            GameObject clone = GameObject.Instantiate(original, parent);
            clone.name = name;

            PauseOption option = clone.GetComponent<PauseOption>();

            option.methods = new UnityEngine.Events.UnityEvent();
            option.selectionStrings.Clear();
            option.selectionStrings.Add(name);

            option.labels = new TranslationCollection
            {
                translations = new List<Translation>
                {
                    new Translation
                    {
                        language = Language.English,
                        text = name
                    },
                    new Translation
                    {
                        language = Language.Japanese,
                        text = "TBD"
                    }
                }
            };
            // Use the selection belonging to this specific option.
            RectTransform selectionRect =
                option.selectionText.GetComponent<RectTransform>();

            // Preserve the original selection's layout.
            Vector2 anchorMin = selectionRect.anchorMin;
            Vector2 anchorMax = selectionRect.anchorMax;
            Vector2 offsetMin = selectionRect.offsetMin;
            Vector2 offsetMax = selectionRect.offsetMax;

            GameObject.Destroy(option.selectionText.gameObject);


            var input = CreateInputField(
                clone.transform,
                anchorMin,
                anchorMax,
                offsetMin,
                offsetMax);

            GameObject throwAway = new GameObject();
            option.selectionText = throwAway.AddComponent<TextMeshProUGUI>();

            return input;
        }




        static TMP_InputField CreateInputField(
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            GameObject root = new GameObject(
                "InputField (TMP)",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(TMP_InputField));

            root.transform.SetParent(parent, false);

            RectTransform rootRect = root.GetComponent<RectTransform>();

            // Preserve the original selection's placement.
            rootRect.anchorMin = anchorMin;
            rootRect.anchorMax = anchorMax;
            rootRect.offsetMin = offsetMin;
            rootRect.offsetMax = offsetMax;

            // Widen the field without changing the option's height.
            rootRect.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal, 220f);

            rootRect.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical, 30f);

            Image background = root.GetComponent<Image>();
            background.color = new Color(0.12f, 0.12f, 0.12f, 1f);

            GameObject area = new GameObject(
                "Text Area",
                typeof(RectTransform),
                typeof(RectMask2D));

            area.transform.SetParent(root.transform, false);

            RectTransform areaRect = area.GetComponent<RectTransform>();
            areaRect.anchorMin = Vector2.zero;
            areaRect.anchorMax = Vector2.one;
            areaRect.offsetMin = new Vector2(8f, 2f);
            areaRect.offsetMax = new Vector2(-8f, -2f);

            TextMeshProUGUI text =
                CreateText("Text", area.transform, "", Color.white);

            TextMeshProUGUI placeholder =
                CreateText(
                    "Placeholder",
                    area.transform,
                    "Enter text...",
                    new Color(1f, 1f, 1f, 0.5f));

            placeholder.fontStyle = FontStyles.Italic;

            TMP_InputField input = root.GetComponent<TMP_InputField>();
            input.textViewport = areaRect;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.targetGraphic = background;
            input.text = "";

            return input;
        }


        private static TextMeshProUGUI CreateText(
        string name,
        Transform parent,
        string initialText,
        Color color)
        {
            GameObject go = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI)
            );

            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = initialText;
            tmp.color = color;
            tmp.fontSize = 18;
            tmp.alignment = TextAlignmentOptions.Midline;
            tmp.enableWordWrapping = false;

            return tmp;
        }
    }

    [HarmonyPatch(typeof(PauseOption))]
    internal class PauseOptionPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        public static bool UpdatePrefix(PauseOption __instance)
        {
            if (__instance.selectionText != null &&
                __instance.selectionStrings != null &&
                __instance.selectionStrings.Count > 0)
            {
                __instance.currentSelection = Mathf.Clamp(
                    __instance.currentSelection,
                    0,
                    __instance.selectionStrings.Count - 1
                );

                __instance.selectionText.text =
                    __instance.selectionStrings[__instance.currentSelection];
            }

            return false;
        }
    }
}
