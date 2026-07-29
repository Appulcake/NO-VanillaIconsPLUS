using HarmonyLib;

namespace VanillaIconsPLUS;

[HarmonyPatch(typeof(UnitMapIcon), "UpdateIcon")]
public static class Patch_Map_AAColour
{
    private static void Postfix(UnitMapIcon __instance)
    {
        if (__instance == null || __instance.unit == null) return;
        var instance = Plugin.Instance;
        var i = SceneSingleton<DynamicMap>.i;
        if (i == null || (i.selectedIcons != null && i.selectedIcons.Contains(__instance))) return;
        var hQ = i.HQ;
        if (!(hQ == null) && __instance.unit.NetworkHQ != null && __instance.unit.NetworkHQ != hQ &&
            AAUnitHelper.IsAA(__instance.unit))
        {
            var image = __instance.GetImage();
            if (image != null) image.color = instance.AAUnitsHUD.Value;
        }
        else if (!(hQ == null) && __instance.unit.NetworkHQ != null &&
                 __instance.unit.NetworkHQ != hQ && AAUnitHelper.IsSpecialAA(__instance.unit))
        {
            var image = __instance.GetImage();
            if (image != null) image.color = instance.SpecialAAUnitsHUD.Value;
        }
    }
}