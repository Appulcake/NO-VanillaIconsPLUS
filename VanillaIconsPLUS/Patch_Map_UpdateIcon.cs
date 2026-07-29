using HarmonyLib;
using TMPro;
using UnityEngine;

namespace VanillaIconsPLUS;

[HarmonyPatch(typeof(UnitMapIcon), "UpdateIcon")]
public static class Patch_Map_UpdateIcon
{
    private static void Postfix(UnitMapIcon __instance, float mapDisplayFactor, float mapInverseScale,
        Transform mapTransform, bool mapMaximized)
    {
        if (__instance == null || __instance.unit == null) return;
        var instance = Plugin.Instance;
        var aircraft = __instance.unit as Aircraft;
        if (aircraft == null || aircraft.Player == null) return;
        var image = __instance.GetImage();
        if (image == null) return;
        var holder = __instance.GetHolder();
        var text = holder.label;
        if (text == null)
        {
            var gameObject = new GameObject("MAP_PlayerName");
            gameObject.transform.SetParent(image.transform.parent, false);
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
            text.fontSize = instance.MAPNameFontSize.Value;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            __instance.SetLabel(text);
        }
        
        text.text = aircraft.Player.GetNameOrCensored();
        var flag = !instance.ShowMAPNames.Value;
        text.fontSize = instance.MAPNameFontSize.Value;
        var value = instance.MAPNameOffset.Value;
        text.transform.localPosition = image.transform.localPosition + new Vector3(0f, value, 0f);
        text.transform.localScale = Vector3.one * mapInverseScale;
        var flag2 = false;
        var networkHQ = __instance.unit.NetworkHQ;
        if (networkHQ != null)
        {
            var factionMode = DynamicMap.GetFactionMode(networkHQ, true);
            flag2 = factionMode == FactionMode.Friendly;
        }
        
        text.color = flag2 ? instance.FriendlyNameMAP.Value : instance.EnemyNameMAP.Value;
        var enabled = mapMaximized && __instance.gameObject.activeInHierarchy && !JamState.PlayerIsJammed && !flag;
        text.enabled = enabled;
    }
}