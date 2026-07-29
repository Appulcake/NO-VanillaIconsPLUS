using HarmonyLib;
using TMPro;
using UnityEngine;

namespace VanillaIconsPLUS;

[HarmonyPatch(typeof(HUDUnitMarker), "UpdateVisibility")]
public static class Patch_HUD_UpdateVisibility
{
    private static void Postfix(HUDUnitMarker __instance)
    {
        if (__instance == null || __instance.unit == null) return;
        var aircraft = __instance.unit as Aircraft;
        if (aircraft == null || aircraft.Player == null) return;
        var instance = Plugin.Instance;
        var holder = __instance.GetHolder();
        var text = holder.label;
        if (text == null)
        {
            var gameObject = new GameObject("HUD_PlayerName");
            gameObject.transform.SetParent(SceneSingleton<CombatHUD>.i.iconLayer, false);
            text = gameObject.AddComponent<TextMeshProUGUI>();
            text.overflowMode = TextOverflowModes.Overflow;
            text.enableWordWrapping = false;
            text.enableAutoSizing = false;
            if (holder.font == null)
            {
                var componentInChildren = SceneSingleton<CombatHUD>.i.GetComponentInChildren<TextMeshProUGUI>(true);
                holder.font = componentInChildren != null
                    ? componentInChildren.font
                    : TMP_Settings.defaultFontAsset;
            }
            
            text.font = holder.font;
            text.fontSize = instance.HUDNameFontSize.Value;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            __instance.SetLabel(text);
            holder.spawnTime = Time.timeSinceLevelLoad;
        }
        
        text.text = aircraft.Player.GetNameOrCensored();
        if (Time.timeSinceLevelLoad - holder.spawnTime < 0.01f) text.enabled = false;
    }
}