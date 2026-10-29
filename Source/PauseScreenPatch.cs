using HarmonyLib;
using SmolAmeArchipelago;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SmolAmeArchipelagoMod.Source
{
    /// <summary>
    /// Adds Server / Port / Slot / Password text fields and a Connect button to the pause menu.
    ///
    /// Patches in this file:
    ///   PauseScreen.Start          (prefix)   builds the extra options before the game lays the menu out
    ///   PauseScreen.Inputs         (postfix)  stops arrows / WASD from moving the menu while typing
    ///   PauseOption.EnterSelection (prefix)   stops Enter/Space from "pressing" the highlighted option while typing
    ///   PauseOption.Update         (prefix)   null-safe replacement (the field options have no selection text)
    ///
    /// </summary>
    [HarmonyPatch(typeof(PauseScreen))]
    internal static class PauseScreenPatch
    {
        public static TMP_InputField Server;
        public static TMP_InputField Port;
        public static TMP_InputField Slot;
        public static TMP_InputField Password;

        // Frame in which a field last stopped being edited. See IsTyping().
        static int lastEditEndFrame = -1;

        // ------------------------------------------------------------------
        // Menu construction
        // ------------------------------------------------------------------

        [HarmonyPrefix]
        [HarmonyPatch("Start")]
        public static void StartPrefix(PauseScreen __instance)
        {
            EnsureEventSystem();

            Transform pauseMenu = __instance.pauseMenu.transform;
            PauseOption template = pauseMenu.GetComponentInChildren<PauseOption>(true);

            if (template == null)
            {
                Dbg.Error("PauseMenu", "No PauseOption found to clone - the Archipelago options were not created");
                return;
            }

            Server = CreateInputOption("Server", template, pauseMenu);
            Port = CreateInputOption("Port", template, pauseMenu);
            Slot = CreateInputOption("Slot", template, pauseMenu);
            Password = CreateInputOption("Password", template, pauseMenu);

            Server.text = PlayerPrefs.GetString("server", "");
            Port.text = PlayerPrefs.GetInt("port", 0).ToString();
            Slot.text = PlayerPrefs.GetString("slot", "");

            CreateConnectOption(template, pauseMenu);
        }

        static void CreateConnectOption(PauseOption template, Transform parent)
        {
            GameObject go = GameObject.Instantiate(template.gameObject, parent);
            go.name = "Connect";

            PauseOption option = go.GetComponent<PauseOption>();
            option.methods = new UnityEvent();
            option.methods.AddListener(() => SmolAmeClient.OnConnectionInvoked());
            option.selectionStrings = new List<string> { "" };
            option.labels = MakeLabels("Connect");
        }

        static TMP_InputField CreateInputOption(string name, PauseOption template, Transform parent)
        {
            GameObject clone = GameObject.Instantiate(template.gameObject, parent);
            clone.name = name;

            PauseOption option = clone.GetComponent<PauseOption>();
            option.methods = new UnityEvent();
            option.labels = MakeLabels(name);
            option.selectionStrings = new List<string> { name };

            // Remember where this clone's own selection text sits, then replace it with an input field.
            RectTransform selectionRect = option.selectionText.GetComponent<RectTransform>();
            Vector2 anchorMin = selectionRect.anchorMin;
            Vector2 anchorMax = selectionRect.anchorMax;
            Vector2 offsetMin = selectionRect.offsetMin;
            Vector2 offsetMax = selectionRect.offsetMax;

            GameObject.Destroy(option.selectionText.gameObject);

            // PauseOptionPatch.UpdatePrefix skips options whose selectionText is null,
            // so no placeholder text object is needed.
            option.selectionText = null;

            TMP_InputField input = CreateInputField(
                clone.transform, anchorMin, anchorMax, offsetMin, offsetMax);

            // Pressing Enter on this option (when not typing) starts editing.
            option.methods.AddListener(() => BeginEdit(input));
            // Pressing Enter while typing ends the edit and lands here.
            input.onEndEdit.AddListener(_ => OnEndEdit(input));

            return input;
        }

        static TranslationCollection MakeLabels(string english)
        {
            return new TranslationCollection
            {
                translations = new List<Translation>
                {
                    new Translation { language = Language.English, text = english },
                    new Translation { language = Language.Japanese, text = "TBD" }
                }
            };
        }

        static TMP_InputField CreateInputField(
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            // The root stays inactive until it is fully built. TMP_InputField reads its
            // textComponent / textViewport in OnEnable, so they must be assigned before that runs.
            GameObject root = new GameObject("InputField (TMP)", typeof(RectTransform));
            root.SetActive(false);
            root.transform.SetParent(parent, false);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = anchorMin;
            rootRect.anchorMax = anchorMax;
            rootRect.offsetMin = offsetMin;
            rootRect.offsetMax = offsetMax;

            // Widen the field without changing the option's height.
            rootRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 220f);
            rootRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 30f);

            Image background = root.AddComponent<Image>();
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

            TextMeshProUGUI text = CreateText("Text", area.transform, "", Color.white);

            TextMeshProUGUI placeholder = CreateText(
                "Placeholder", area.transform, "Enter text...", new Color(1f, 1f, 1f, 0.5f));
            placeholder.fontStyle = FontStyles.Italic;

            TMP_InputField input = root.AddComponent<TMP_InputField>();
            input.textViewport = areaRect;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.targetGraphic = background;

            root.SetActive(true);
            return input;
        }

        static TextMeshProUGUI CreateText(string name, Transform parent, string initialText, Color color)
        {
            GameObject go = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
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

        // ------------------------------------------------------------------
        // Editing
        // ------------------------------------------------------------------

        static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;

            GameObject go = new GameObject("AP EventSystem");
            GameObject.DontDestroyOnLoad(go);

            EventSystem es = go.AddComponent<EventSystem>();
            es.sendNavigationEvents = false;
            go.AddComponent<StandaloneInputModule>();
        }

        // Called by the option's event when Enter is pressed on it (and we are not already typing).
        static void BeginEdit(TMP_InputField input)
        {
            PauseScreen ps = PauseScreen.pauseScreen;
            if (input == null || ps == null) return;

            ps.StartCoroutine(ActivateNextFrame(input));
        }

        static IEnumerator ActivateNextFrame(TMP_InputField input)
        {
            yield return null;

            if (input == null || !input.gameObject.activeInHierarchy) yield break;

            EnsureEventSystem();
            input.Select();
            input.ActivateInputField();
        }

        static void OnEndEdit(TMP_InputField input)
        {
            lastEditEndFrame = Time.frameCount;

            EventSystem es = EventSystem.current;
            if (es != null && !es.alreadySelecting && es.currentSelectedGameObject == input.gameObject)
                es.SetSelectedGameObject(null);
        }

        static bool AnyFieldFocused()
        {
            return (Server != null && Server.isFocused)
                || (Port != null && Port.isFocused)
                || (Slot != null && Slot.isFocused)
                || (Password != null && Password.isFocused);
        }

        // True while a field is focused, and for the whole frame in which one closed.
        internal static bool IsTyping()
        {
            return Time.frameCount == lastEditEndFrame || AnyFieldFocused();
        }

        // ------------------------------------------------------------------
        // Block menu navigation while typing
        // ------------------------------------------------------------------

        [HarmonyPostfix]
        [HarmonyPatch("Inputs")]
        public static void InputsPostfix(
            ref bool ___upInput, ref bool ___downInput, ref bool ___vInputDown,
            ref bool ___leftInput, ref bool ___rightInput, ref bool ___hInputDown)
        {
            if (!IsTyping()) return;

            ___upInput = ___downInput = ___vInputDown = false;
            ___leftInput = ___rightInput = ___hInputDown = false;
        }
    }

    [HarmonyPatch(typeof(PauseOption))]
    internal static class PauseOptionPatch
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
                    __instance.selectionStrings.Count - 1);

                __instance.selectionText.text =
                    __instance.selectionStrings[__instance.currentSelection];
            }

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch("EnterSelection")]
        public static bool EnterSelectionPrefix()
        {
            return !PauseScreenPatch.IsTyping();
        }
    }
}