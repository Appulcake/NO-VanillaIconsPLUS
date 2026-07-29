using HarmonyLib;
using UnityEngine;

namespace VanillaIconsPLUS;

[HarmonyPatch(typeof(UnitMapIcon), nameof(UnitMapIcon.OnRemoveIcon))]
public static class Patch_Map_RemoveIcon
{
    private static void Prefix(UnitMapIcon __instance)
    {
        var label = __instance.GetLabel();
        if (label != null)
        {
            Object.Destroy(label.gameObject);
            __instance.SetLabel(null);
        }
    }
}