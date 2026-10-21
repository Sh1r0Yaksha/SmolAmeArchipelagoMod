using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SmolAmeArchipelagoMod.Source
{
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
