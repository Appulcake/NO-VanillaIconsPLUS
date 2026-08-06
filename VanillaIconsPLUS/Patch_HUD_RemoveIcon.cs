using HarmonyLib;
using UnityEngine;

namespace VanillaIconsPLUS;

[HarmonyPatch(typeof(HUDUnitMarker), nameof(HUDUnitMarker.RemoveIcon))]
public static class Patch_HUD_RemoveIcon
{
    private static void Prefix(HUDUnitMarker __instance)
    {
        var label = __instance.GetLabel();
        if (label != null)
        {
            Object.Destroy(label.gameObject);
            __instance.SetLabel(null);
        }
    }
}