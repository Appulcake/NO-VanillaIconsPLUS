// ================================================================
// VanillaIconsPLUS (unit tints + HUD & map player names + AA highlight)
// Version: 1.5.1
// GUID: com.hellcat92.vanillaiconsplus_1.5.1
// Description: Configurable HUD & map icon tints + HUD & Map Player Name labels
//              + instant HUD refresh + enemy AA highlight + player-centric jamming name suppression
// Author: Hellcat92
// Date: 08 January 2026
// ================================================================

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using NuclearOption;
using NuclearOption.Networking;

namespace VanillaIconsPLUS
{
    [BepInPlugin("com.hellcat92.vanillaiconsplus_1.5.1", "Vanilla Icons PLUS", "1.5.1")]
    public class Plugin : BaseUnityPlugin
    {
        // HUD name tints
        public ConfigEntry<Color> FriendlyNameHUD;
        public ConfigEntry<Color> EnemyNameHUD;

        // HUD icon tints
        public ConfigEntry<Color> FriendlyUnitsHUD;
        public ConfigEntry<Color> EnemyUnitsHUD;
        public ConfigEntry<Color> NeutralUnitsHUD;

        // AA unit tint (HUD + MAP, enemy only)
        public ConfigEntry<Color> AAUnitsHUD;

        // MAP name tints
        public ConfigEntry<Color> FriendlyNameMAP;
        public ConfigEntry<Color> EnemyNameMAP;

        // Toggles
        public ConfigEntry<bool> ShowHUDNames;
        public ConfigEntry<bool> ShowMAPNames;

        // Name label customization
        public ConfigEntry<int> HUDNameFontSize;
        public ConfigEntry<float> HUDNameOffset;
        public ConfigEntry<int> MAPNameFontSize;
        public ConfigEntry<float> MAPNameOffset;

        internal static ManualLogSource Log;
        private Harmony _harmony;
        internal static Plugin Instance;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            // ============================================================
            // CONFIG
            // ============================================================

            ShowHUDNames = Config.Bind("Settings", "Show Player Names",
                true, "Toggle HUD player names");

            FriendlyNameHUD = Config.Bind("Settings", "Friendly Player Names",
                new Color(0.19f, 0.58f, 1f, 1f), "Friendly HUD player names");

            EnemyNameHUD = Config.Bind("Settings", "Enemy Player Names",
                new Color(1f, 0.13f, 0.05f, 1f), "Enemy HUD player names");

            FriendlyUnitsHUD = Config.Bind("Settings", "Friendly Units",
                new Color(0.19f, 0.58f, 1f, 1f), "Friendly HUD unit icons");

            EnemyUnitsHUD = Config.Bind("Settings", "Enemy Units",
                new Color(1f, 0.13f, 0.05f, 1f), "Enemy HUD unit icons");

            NeutralUnitsHUD = Config.Bind("Settings", "Neutral Units",
                new Color(0.6f, 0.6f, 0.6f, 1f), "Neutral HUD unit icons");

            AAUnitsHUD = Config.Bind("Settings", "Enemy AA Units",
                new Color(1f, 0.13f, 0.05f, 1f),
                "Tint for enemy AA/SAM/CIWS units on HUD & Map");

            ShowMAPNames = Config.Bind("Settings", "Show Map Player Names",
                true, "Toggle map player names");

            FriendlyNameMAP = Config.Bind("Settings", "Friendly Player Names (MAP)",
                new Color(0.19f, 0.58f, 1f, 1f), "Friendly map player names");

            EnemyNameMAP = Config.Bind("Settings", "Enemy Player Names (MAP)",
                new Color(1f, 0.13f, 0.05f, 1f), "Enemy map player names");

            HUDNameFontSize = Config.Bind("Settings", "HUD Player Name Font Size",
                14, "Font size for HUD player names");

            HUDNameOffset = Config.Bind("Settings", "HUD Player Name Vertical Offset",
                25f, "Vertical offset above HUD icons");

            MAPNameFontSize = Config.Bind("Settings", "MAP Player Name Font Size",
                14, "Font size for MAP player names");

            MAPNameOffset = Config.Bind("Settings", "MAP Player Name Vertical Offset",
                5f, "Vertical offset above MAP icons");

            // ============================================================
            // PATCHING
            // ============================================================

            _harmony = new Harmony("com.hellcat92.vanillaiconsplus_1.5.1");
            _harmony.PatchAll();

            // HUD unit colours → HUD + map refresh
            FriendlyUnitsHUD.SettingChanged += (_, __) =>
            {
                ApplyHUDTints();
                RefreshHUDIcons();
                RefreshMapIcons();
            };

            EnemyUnitsHUD.SettingChanged += (_, __) =>
            {
                ApplyHUDTints();
                RefreshHUDIcons();
                RefreshMapIcons();
            };

            NeutralUnitsHUD.SettingChanged += (_, __) =>
            {
                ApplyHUDTints();
                RefreshHUDIcons();
                RefreshMapIcons();
            };

            // AA recolour refresh (HUD + map)
            AAUnitsHUD.SettingChanged += (_, __) =>
            {
                RefreshHUDIcons();
                RefreshMapIcons();
            };

            // Initial application
            ApplyHUDTints();
            RefreshHUDIcons();
            RefreshMapIcons();

            Log.LogInfo("VanillaIconsPLUS 1.5.1 loaded.");
        }

        internal static void ApplyHUDTints()
        {
            var ga = Resources.FindObjectsOfTypeAll<GameAssets>().FirstOrDefault() ?? GameAssets.i;
            if (ga == null)
            {
                Log.LogWarning("GameAssets not found.");
                return;
            }

            ga.HUDFriendly = Instance.FriendlyUnitsHUD.Value;
            ga.HUDHostile = Instance.EnemyUnitsHUD.Value;
            ga.HUDNeutral = Instance.NeutralUnitsHUD.Value;
        }

        internal static void RefreshHUDIcons()
        {
            var hud = SceneSingleton<CombatHUD>.i;
            if (hud == null || hud.aircraft == null)
                return;

            var field = AccessTools.Field(typeof(CombatHUD), "markers");
            var markers = field.GetValue(hud) as List<HUDUnitMarker>;
            if (markers == null)
                return;

            foreach (var marker in markers)
            {
                if (marker?.unit == null || marker.image == null)
                    continue;

                bool hasHQ = marker.unit.NetworkHQ != null;
                bool sameHQ = hasHQ && hud.aircraft.NetworkHQ != null &&
                              marker.unit.NetworkHQ == hud.aircraft.NetworkHQ;
                bool isNeutral = !hasHQ;
                bool isEnemy = hasHQ && !sameHQ;

                // Preserve selection colour
                if (marker.selected)
                    continue;

                Color baseTint;
                if (isNeutral)
                    baseTint = Instance.NeutralUnitsHUD.Value;
                else if (isEnemy)
                    baseTint = Instance.EnemyUnitsHUD.Value;
                else
                    baseTint = Instance.FriendlyUnitsHUD.Value;

                float a = marker.image.color.a;
                Color result = new Color(baseTint.r, baseTint.g, baseTint.b, a);

                // AA override (enemy only)
                if (isEnemy && AAUnitHelper.IsAA(marker.unit))
                {
                    var aa = Instance.AAUnitsHUD.Value;
                    result = new Color(aa.r, aa.g, aa.b, a);
                }

                marker.image.color = result;
            }
        }

        internal static void RefreshMapIcons()
        {
            var map = SceneSingleton<DynamicMap>.i;
            if (map == null) return;

            var field = AccessTools.Field(typeof(DynamicMap), "iconLookup");
            var dict = field.GetValue(map) as Dictionary<Unit, UnitMapIcon>;
            if (dict == null) return;

            foreach (var kvp in dict)
                kvp.Value.UpdateColor();
        }
    }

    // ============================================================
    // AA UNIT HELPER
    // ============================================================

    public static class AAUnitHelper
    {
        static readonly HashSet<string> AAUnitNames = new HashSet<string>
        {
            "23mm AAA Emplacement",
            "AFV-6 IFV",
            "AFV-6 AA",
            "AFV-8 SAM",
            "AFV6 AA",
            "AFV8 Mobile Air Defense",
            "AeroSentry SPAAG",
            "FGA-57 Anvil",
            "HLT LADS",
            "HLT CRAM",
            "HLT R9 Launcher",
            "HLT Radar Truck",
            "HLT-CRAM",
            "HLT-HEL",
            "Hexhound SAM",
            "IRM-S1 Emplacement",
            "LCV-25 AA",
            "LCV25 AA",
            "Linebreaker SAM",
            "Linebreaker IFV",
            "Linebreaker SPG",
            "MSV CRAM",
            "MSV LADS",
            "MSV R9 Launcher",
            "MSV R9 Stratolance Launcher",
            "MSV Radar",
            "SPG-30 Aerosentry",
            "StratoLance R9 Launcher",
            "T9K41 Boltstrike",
            "Type-14 LRAA"
        };

        public static bool IsAA(Unit u)
        {
            if (u == null || string.IsNullOrEmpty(u.unitName))
                return false;

            return AAUnitNames.Contains(u.unitName);
        }
    }

    // ============================================================
    // JAM STATE (RadarWarning-based, player-centric)
    // ============================================================

    public static class JamState
    {
        // Retained for compatibility / potential future use
        public static readonly HashSet<Unit> JammedUnits = new HashSet<Unit>();

        // NEW: true only when the *player's aircraft* is actively being jammed
        public static bool PlayerIsJammed;
    }

    [HarmonyPatch(typeof(RadarWarning), "Update")]
    public static class Patch_RadarWarning_Update
    {
        static readonly FieldInfo JammingLookupField =
            AccessTools.Field(typeof(RadarWarning), "jammingIconLookup");

        static void Postfix(RadarWarning __instance)
        {
            if (__instance == null || JammingLookupField == null)
                return;

            var lookup = JammingLookupField.GetValue(__instance) as IDictionary;
            if (lookup == null)
                return;

            JamState.JammedUnits.Clear();

            foreach (DictionaryEntry entry in lookup)
            {
                var unit = entry.Key as Unit;
                if (unit != null)
                    JamState.JammedUnits.Add(unit);
            }

            // Player-centric: if there is any jamming icon, the player's aircraft is being jammed
            JamState.PlayerIsJammed = JamState.JammedUnits.Count > 0;
        }
    }

    // ============================================================
    // HUD PLAYER NAME LABEL SYSTEM
    // ============================================================

    public static class HUDUnitMarkerExtensions
    {
        public class NameHolder
        {
            public UnityEngine.UI.Text label;
            public float spawnTime;
            public Font font;
        }

        public static readonly ConditionalWeakTable<HUDUnitMarker, NameHolder> table
            = new ConditionalWeakTable<HUDUnitMarker, NameHolder>();

        public static NameHolder GetHolder(this HUDUnitMarker marker)
            => table.GetOrCreateValue(marker);

        public static UnityEngine.UI.Text GetLabel(this HUDUnitMarker marker)
            => marker.GetHolder().label;

        public static void SetLabel(this HUDUnitMarker marker, UnityEngine.UI.Text label)
            => marker.GetHolder().label = label;
    }

    [HarmonyPatch(typeof(HUDUnitMarker), "UpdateVisibility")]
    public static class Patch_HUD_UpdateVisibility
    {
        static void Postfix(HUDUnitMarker __instance)
        {
            if (__instance == null || __instance.unit == null)
                return;

            Aircraft ac = __instance.unit as Aircraft;
            if (ac == null || ac.Player == null)
                return;

            var plugin = Plugin.Instance;

            var holder = __instance.GetHolder();
            UnityEngine.UI.Text label = holder.label;

            if (label == null)
            {
                GameObject go = new GameObject("HUD_PlayerName");
                go.transform.SetParent(SceneSingleton<CombatHUD>.i.iconLayer, false);

                label = go.AddComponent<UnityEngine.UI.Text>();

                if (holder.font == null)
                {
                    UnityEngine.UI.Text hudText = SceneSingleton<CombatHUD>.i.GetComponentInChildren<UnityEngine.UI.Text>(true);
                    holder.font = hudText != null ? hudText.font : Resources.GetBuiltinResource<Font>("Arial.ttf");
                }

                label.font = holder.font;
                label.fontSize = plugin.HUDNameFontSize.Value;
                label.alignment = TextAnchor.MiddleCenter;
                label.raycastTarget = false;

                __instance.SetLabel(label);
                holder.spawnTime = Time.timeSinceLevelLoad;
            }

            label.text = ac.Player.PlayerName;

            if (Time.timeSinceLevelLoad - holder.spawnTime < 0.01f)
            {
                label.enabled = false;
                return;
            }
        }
    }

    [HarmonyPatch(typeof(HUDUnitMarker), "UpdatePosition")]
    public static class Patch_HUD_UpdatePosition
    {
        static void Postfix(HUDUnitMarker __instance)
        {
            var plugin = Plugin.Instance;
            UnityEngine.UI.Text label = __instance.GetLabel();
            if (label == null)
                return;

            bool hideBySelection = __instance.selected;
            bool hideByToggle = !plugin.ShowHUDNames.Value;

            label.fontSize = plugin.HUDNameFontSize.Value;
            float offset = plugin.HUDNameOffset.Value;
            label.transform.position = __instance.image.transform.position + new Vector3(0f, offset, 0f);

            bool friendly = __instance.unit.NetworkHQ ==
                            SceneSingleton<CombatHUD>.i.aircraft.NetworkHQ;

            label.color = friendly
                ? plugin.FriendlyNameHUD.Value
                : plugin.EnemyNameHUD.Value;

            // Name hiding only when the *player's aircraft* is actively being jammed
            bool visible =
                __instance.image.enabled &&
                !JamState.PlayerIsJammed &&
                !hideBySelection &&
                !hideByToggle;

            label.enabled = visible;
        }
    }

    [HarmonyPatch(typeof(HUDUnitMarker), "RemoveIcon")]
    public static class Patch_HUD_RemoveIcon
    {
        static void Prefix(HUDUnitMarker __instance)
        {
            UnityEngine.UI.Text label = __instance.GetLabel();
            if (label != null)
            {
                GameObject.Destroy(label.gameObject);
                __instance.SetLabel(null);
            }
        }
    }

    // ============================================================
    // HUD AA UNIT RECOLOUR
    // ============================================================

    [HarmonyPatch(typeof(CombatHUD), "UpdateMarkers")]
    public static class Patch_HUD_AAColour
    {
        static void Postfix(CombatHUD __instance)
        {
            if (__instance == null || __instance.aircraft == null)
                return;

            var plugin = Plugin.Instance;

            var field = AccessTools.Field(typeof(CombatHUD), "markers");
            var markers = field.GetValue(__instance) as List<HUDUnitMarker>;
            if (markers == null)
                return;

            foreach (var marker in markers)
            {
                if (marker?.unit == null || marker.image == null)
                    continue;

                bool hasHQ = marker.unit.NetworkHQ != null;
                bool sameHQ = hasHQ && __instance.aircraft.NetworkHQ != null &&
                                 marker.unit.NetworkHQ == __instance.aircraft.NetworkHQ;
                bool isEnemy = hasHQ && !sameHQ;

                if (!isEnemy)
                    continue;

                if (!AAUnitHelper.IsAA(marker.unit))
                    continue;

                if (marker.selected)
                    continue;

                Color current = marker.image.color;
                Color aaTint = plugin.AAUnitsHUD.Value;

                marker.image.color = new Color(aaTint.r, aaTint.g, aaTint.b, current.a);
            }
        }
    }

    // ============================================================
    // MAP PLAYER NAME LABEL SYSTEM
    // ============================================================

    public static class MapIconExtensions
    {
        public class NameHolder
        {
            public UnityEngine.UI.Text label;
            public Font font;
        }

        public static readonly ConditionalWeakTable<UnitMapIcon, NameHolder> table
            = new ConditionalWeakTable<UnitMapIcon, NameHolder>();

        public static NameHolder GetHolder(this UnitMapIcon icon)
            => table.GetOrCreateValue(icon);

        public static UnityEngine.UI.Text GetLabel(this UnitMapIcon icon)
            => icon.GetHolder().label;

        public static void SetLabel(this UnitMapIcon icon, UnityEngine.UI.Text label)
            => icon.GetHolder().label = label;
    }

    public static class MapIconHelpers
    {
        static readonly FieldInfo ImageField =
            AccessTools.Field(typeof(UnitMapIcon), "iconImage");

        public static UnityEngine.UI.Image GetImage(this UnitMapIcon icon)
            => ImageField?.GetValue(icon) as UnityEngine.UI.Image;
    }

    [HarmonyPatch(typeof(UnitMapIcon), "UpdateIcon")]
    public static class Patch_Map_UpdateIcon
    {
        static void Postfix(UnitMapIcon __instance, float mapDisplayFactor, float mapInverseScale, Transform mapTransform, bool mapMaximized)
        {
            if (__instance == null || __instance.unit == null)
                return;

            var plugin = Plugin.Instance;

            Aircraft ac = __instance.unit as Aircraft;
            if (ac == null || ac.Player == null)
                return;

            UnityEngine.UI.Image img = __instance.GetImage();
            if (img == null)
                return;

            var holder = __instance.GetHolder();
            UnityEngine.UI.Text label = holder.label;

            if (label == null)
            {
                GameObject go = new GameObject("MAP_PlayerName");
                go.transform.SetParent(img.transform.parent, false);

                label = go.AddComponent<UnityEngine.UI.Text>();

                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                label.verticalOverflow = VerticalWrapMode.Overflow;
                label.resizeTextForBestFit = false;

                if (holder.font == null)
                {
                    UnityEngine.UI.Text hudText = SceneSingleton<CombatHUD>.i.GetComponentInChildren<UnityEngine.UI.Text>(true);
                    holder.font = hudText != null ? hudText.font : Resources.GetBuiltinResource<Font>("Arial.ttf");
                }

                label.font = holder.font;
                label.fontSize = plugin.MAPNameFontSize.Value;
                label.alignment = TextAnchor.MiddleCenter;
                label.raycastTarget = false;

                __instance.SetLabel(label);
            }

            label.text = ac.Player.PlayerName;

            bool hideByToggle = !plugin.ShowMAPNames.Value;

            label.fontSize = plugin.MAPNameFontSize.Value;
            float offset = plugin.MAPNameOffset.Value;
            label.transform.localPosition = img.transform.localPosition + new Vector3(0f, offset, 0f);
            label.transform.localScale = Vector3.one * mapInverseScale;

            bool friendly = false;
            var hq = __instance.unit.NetworkHQ;
            if (hq != null)
            {
                var mode = DynamicMap.GetFactionMode(hq, true);
                friendly = (mode == FactionMode.Friendly);
            }

            label.color = friendly
                ? plugin.FriendlyNameMAP.Value
                : plugin.EnemyNameMAP.Value;

            // Name hiding only when the *player's aircraft* is actively being jammed
            bool visible =
                mapMaximized &&
                __instance.gameObject.activeInHierarchy &&
                !JamState.PlayerIsJammed &&
                !hideByToggle;

            label.enabled = visible;
        }
    }

    [HarmonyPatch(typeof(UnitMapIcon), "OnRemoveIcon")]
    public static class Patch_Map_RemoveIcon
    {
        static void Prefix(UnitMapIcon __instance)
        {
            UnityEngine.UI.Text label = __instance.GetLabel();
            if (label != null)
            {
                GameObject.Destroy(label.gameObject);
                __instance.SetLabel(null);
            }
        }
    }

    // ============================================================
    // MAP AA UNIT RECOLOUR
    // ============================================================

    [HarmonyPatch(typeof(UnitMapIcon), "UpdateIcon")]
    public static class Patch_Map_AAColour
    {
        static void Postfix(UnitMapIcon __instance)
        {
            if (__instance == null || __instance.unit == null)
                return;

            var plugin = Plugin.Instance;
            var map = SceneSingleton<DynamicMap>.i;
            if (map == null)
                return;

            // Preserve selection tint (yellow)
            if (map.selectedIcons != null && map.selectedIcons.Contains(__instance))
                return;

            var playerHQ = map.HQ;
            if (playerHQ == null)
                return;

            bool isEnemy = __instance.unit.NetworkHQ != null &&
                           __instance.unit.NetworkHQ != playerHQ;

            if (!isEnemy || !AAUnitHelper.IsAA(__instance.unit))
                return;

            UnityEngine.UI.Image img = __instance.GetImage();
            if (img != null)
                img.color = plugin.AAUnitsHUD.Value;
        }
    }

    // ============================================================
    // MAP FIX — Reapply tints after SetFaction()
    // ============================================================

    [HarmonyPatch(typeof(DynamicMap), "SetFaction")]
    public static class Patch_Map_SetFaction
    {
        static void Postfix(DynamicMap __instance)
        {
            Plugin.Instance.StartCoroutine(Delayed());
        }

        static IEnumerator Delayed()
        {
            yield return null;
            Plugin.ApplyHUDTints();
            Plugin.RefreshHUDIcons();
            Plugin.RefreshMapIcons();
        }
    }
}
