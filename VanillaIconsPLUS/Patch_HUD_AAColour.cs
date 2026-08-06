using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace VanillaIconsPLUS;

[HarmonyPatch(typeof(CombatHUD), nameof(CombatHUD.UpdateMarkers))]
public static class Patch_HUD_AAColour
{
    private static void Postfix(CombatHUD __instance)
    {
        if (__instance == null || __instance.aircraft == null) return;
        var instance = Plugin.Instance;
        var fieldInfo = AccessTools.Field(typeof(CombatHUD), "markers");
        if (!(fieldInfo.GetValue(__instance) is List<HUDUnitMarker> list)) return;
        foreach (var item in list)
            if (!(item?.unit == null) && !(item.image == null))
            {
                var flag = item.unit.NetworkHQ != null;
                var flag2 = flag && __instance.aircraft.NetworkHQ != null &&
                            item.unit.NetworkHQ == __instance.aircraft.NetworkHQ;
                if (flag && !flag2 && AAUnitHelper.IsAA(item.unit) && !item.selected)
                {
                    var color = item.image.color;
                    var value = instance.AAUnitsHUD.Value;
                    item.image.color = new Color(value.r, value.g, value.b, color.a);
                }
                else if (flag && !flag2 && AAUnitHelper.IsSpecialAA(item.unit) && !item.selected)
                {
                    var color = item.image.color;
                    var value = instance.SpecialAAUnitsHUD.Value;
                    item.image.color = new Color(value.r, value.g, value.b, color.a);
                }
            }
    }
}