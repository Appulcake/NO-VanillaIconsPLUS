using HarmonyLib;

namespace VanillaIconsPLUS;

[HarmonyPatch(typeof(AllyInfo), nameof(AllyInfo.UpdateAllyInfoOnHover))]
public static class Patch_AllyInfoHover
{
    private static bool Prefix(AllyInfo __instance)
    {
        if (!Plugin.DisableAllyInfoHover.Value)
            return true;
        
        __instance.hoveredAllyInfo.enabled = false;
        return false;
    }
}