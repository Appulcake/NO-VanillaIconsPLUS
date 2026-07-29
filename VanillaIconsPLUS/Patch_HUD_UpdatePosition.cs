using HarmonyLib;
using UnityEngine;

namespace VanillaIconsPLUS;

[HarmonyPatch(typeof(HUDUnitMarker), nameof(HUDUnitMarker.UpdatePosition))]
public static class Patch_HUD_UpdatePosition
{
    private static void Postfix(HUDUnitMarker __instance)
    {
        var instance = Plugin.Instance;
        var label = __instance.GetLabel();
        if (!(label == null))
        {
            var selected = __instance.selected;
            var flag = !instance.ShowHUDNames.Value;
            label.fontSize = instance.HUDNameFontSize.Value;
            var value = instance.HUDNameOffset.Value;
            label.transform.position = __instance.image.transform.position + new Vector3(0f, value, 0f);
            var flag2 = __instance.unit.NetworkHQ == SceneSingleton<CombatHUD>.i.aircraft.NetworkHQ;
            label.color = flag2 ? instance.FriendlyNameHUD.Value : instance.EnemyNameHUD.Value;
            var enabled = __instance.image.enabled && !JamState.PlayerIsJammed && !selected && !flag;
            label.enabled = enabled;
        }
    }
}