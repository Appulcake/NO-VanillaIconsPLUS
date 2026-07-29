using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace VanillaIconsPLUS;

[BepInPlugin("com.hellcat92.vanillaiconsplus", "Vanilla Icons PLUS", PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private static ManualLogSource _log;
    
    internal static Plugin Instance;
    
    public static ConfigEntry<bool> DisableAllyInfoHover;
    private ConfigEntry<Color> _enemyUnitsHUD;
    private ConfigEntry<Color> _friendlyUnitsHUD;
    
    private Harmony _harmony;
    private ConfigEntry<Color> _neutralUnitsHUD;
    
    public ConfigEntry<Color> AAUnitsHUD;
    public ConfigEntry<Color> EnemyNameHUD;
    public ConfigEntry<Color> EnemyNameMap;
    public ConfigEntry<Color> FriendlyNameHUD;
    public ConfigEntry<Color> FriendlyNameMap;
    public ConfigEntry<int> HUDNameFontSize;
    public ConfigEntry<float> HUDNameOffset;
    public ConfigEntry<int> MapNameFontSize;
    public ConfigEntry<float> MapNameOffset;
    public ConfigEntry<bool> ShowHUDNames;
    public ConfigEntry<bool> ShowMapNames;
    public ConfigEntry<Color> SpecialAAUnitsHUD;
    
    private void Awake()
    {
        Instance = this;
        _log = Logger;
        ShowHUDNames = Config.Bind("Settings", "Show Player Names", true, "Toggle HUD player names");
        FriendlyNameHUD = Config.Bind("Settings", "Friendly Player Names", new Color(0.19f, 0.58f, 1f, 1f),
            "Friendly HUD player names");
        DisableAllyInfoHover = Config.Bind("Settings", "Disable Vanilla Friendly Hover Names",
            true, "Disable the new 0.34 vanilla feature showing the name of a friendly player you hover over.");
        EnemyNameHUD = Config.Bind("Settings", "Enemy Player Names", new Color(1f, 0.13f, 0.05f, 1f),
            "Enemy HUD player names");
        _friendlyUnitsHUD = Config.Bind("Settings", "Friendly Units", new Color(0.19f, 0.58f, 1f, 1f),
            "Friendly HUD unit icons");
        _enemyUnitsHUD = Config.Bind("Settings", "Enemy Units", new Color(1f, 0.13f, 0.05f, 1f),
            "Enemy HUD unit icons");
        _neutralUnitsHUD = Config.Bind("Settings", "Neutral Units", new Color(0.6f, 0.6f, 0.6f, 1f),
            "Neutral HUD unit icons");
        AAUnitsHUD = Config.Bind("Settings", "Enemy AA Units", new Color(1f, 0.369f, 1f, 1f),
            "Tint for enemy AA/SAM/CIWS units on HUD & Map");
        SpecialAAUnitsHUD = Config.Bind("Settings", "Enemy AA (Special) Units", new Color(0f, 1f, 1f, 1f),
            "Tint for enemy Special AA units on HUD & Map (CRAM/LADS/HEL/Radar/Boltstrike)");
        ShowMapNames = Config.Bind("Settings", "Show Map Player Names", true, "Toggle map player names");
        FriendlyNameMap = Config.Bind("Settings", "Friendly Player Names (MAP)", new Color(0.19f, 0.58f, 1f, 1f),
            "Friendly map player names");
        EnemyNameMap = Config.Bind("Settings", "Enemy Player Names (MAP)", new Color(1f, 0.13f, 0.05f, 1f),
            "Enemy map player names");
        HUDNameFontSize = Config.Bind("Settings", "HUD Player Name Font Size", 14, "Font size for HUD player names");
        HUDNameOffset = Config.Bind("Settings", "HUD Player Name Vertical Offset", 25f,
            "Vertical offset above HUD icons");
        MapNameFontSize = Config.Bind("Settings", "MAP Player Name Font Size", 14, "Font size for MAP player names");
        MapNameOffset = Config.Bind("Settings", "MAP Player Name Vertical Offset", 5f,
            "Vertical offset above MAP icons");
        
        var aaWhiteList = new AAConfigReadWrite(Path.Combine(Paths.ConfigPath,
            "com.hellcat92.vanillaiconsplus_AA_Whitelist.cfg"), Logger);
        
        aaWhiteList.ReadAAList();
        
        _harmony = new Harmony("com.hellcat92.vanillaiconsplus");
        _harmony.PatchAll();
        _friendlyUnitsHUD.SettingChanged += delegate
        {
            ApplyHUDTints();
            RefreshHUDIcons();
            RefreshMapIcons();
        };
        _enemyUnitsHUD.SettingChanged += delegate
        {
            ApplyHUDTints();
            RefreshHUDIcons();
            RefreshMapIcons();
        };
        _neutralUnitsHUD.SettingChanged += delegate
        {
            ApplyHUDTints();
            RefreshHUDIcons();
            RefreshMapIcons();
        };
        AAUnitsHUD.SettingChanged += delegate
        {
            RefreshHUDIcons();
            RefreshMapIcons();
        };
        SpecialAAUnitsHUD.SettingChanged += delegate
        {
            RefreshHUDIcons();
            RefreshMapIcons();
        };
        ApplyHUDTints();
        RefreshHUDIcons();
        RefreshMapIcons();
        _log.LogInfo($"{Info.Metadata.Name} v{Info.Metadata.Version} loaded.");
    }
    
    
    internal static void ApplyHUDTints()
    {
        var gameAssets = Resources.FindObjectsOfTypeAll<GameAssets>().FirstOrDefault() ?? GameAssets.i;
        if (gameAssets == null)
        {
            _log.LogWarning("GameAssets not found.");
            return;
        }
        
        gameAssets.HUDFriendly = Instance._friendlyUnitsHUD.Value;
        gameAssets.HUDHostile = Instance._enemyUnitsHUD.Value;
        gameAssets.HUDNeutral = Instance._neutralUnitsHUD.Value;
    }
    
    internal static void RefreshHUDIcons()
    {
        var i = SceneSingleton<CombatHUD>.i;
        if (i == null || i.aircraft == null) return;
        var fieldInfo = AccessTools.Field(typeof(CombatHUD), "markers");
        if (!(fieldInfo.GetValue(i) is List<HUDUnitMarker> list)) return;
        foreach (var item in list)
        {
            if (item?.unit == null || item.image == null) continue;
            var flag = item.unit.NetworkHQ != null;
            var flag2 = flag && i.aircraft.NetworkHQ != null && item.unit.NetworkHQ == i.aircraft.NetworkHQ;
            var flag3 = !flag;
            var flag4 = flag && !flag2;
            if (!item.selected)
            {
                var color = flag3 ? Instance._neutralUnitsHUD.Value :
                    !flag4 ? Instance._friendlyUnitsHUD.Value : Instance._enemyUnitsHUD.Value;
                var a = item.image.color.a;
                var color2 = new Color(color.r, color.g, color.b, a);
                if (flag4 && AAUnitHelper.IsAA(item.unit))
                {
                    var value = Instance.AAUnitsHUD.Value;
                    color2 = new Color(value.r, value.g, value.b, a);
                }
                else if (flag4 && AAUnitHelper.IsSpecialAA(item.unit))
                {
                    var value = Instance.SpecialAAUnitsHUD.Value;
                    color2 = new Color(value.r, value.g, value.b, a);
                }
                
                item.image.color = color2;
            }
        }
    }
    
    internal static void RefreshMapIcons()
    {
        var i = SceneSingleton<DynamicMap>.i;
        if (i == null) return;
        var fieldInfo = AccessTools.Field(typeof(DynamicMap), "iconLookup");
        if (!(fieldInfo.GetValue(i) is Dictionary<Unit, UnitMapIcon> dictionary)) return;
        foreach (var item in dictionary) item.Value.UpdateColor();
    }
}