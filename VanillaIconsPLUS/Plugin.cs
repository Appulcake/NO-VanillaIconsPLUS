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
    internal static ManualLogSource Log;
    
    internal static Plugin Instance;
    
    public static ConfigEntry<bool> DisableAllyInfoHover;
    
    
    private Harmony _harmony;
    
    public ConfigEntry<Color> AAUnitsHUD;
    
    public ConfigEntry<Color> EnemyNameHUD;
    
    public ConfigEntry<Color> EnemyNameMAP;
    
    public ConfigEntry<Color> EnemyUnitsHUD;
    public ConfigEntry<Color> FriendlyNameHUD;
    
    public ConfigEntry<Color> FriendlyNameMAP;
    
    public ConfigEntry<Color> FriendlyUnitsHUD;
    
    public ConfigEntry<int> HUDNameFontSize;
    
    public ConfigEntry<float> HUDNameOffset;
    
    public ConfigEntry<int> MAPNameFontSize;
    
    public ConfigEntry<float> MAPNameOffset;
    
    public ConfigEntry<Color> NeutralUnitsHUD;
    
    public ConfigEntry<bool> ShowHUDNames;
    
    public ConfigEntry<bool> ShowMAPNames;
    
    public ConfigEntry<Color> SpecialAAUnitsHUD;
    
    private void Awake()
    {
        Instance = this;
        Log = Logger;
        ShowHUDNames = Config.Bind("Settings", "Show Player Names", true, "Toggle HUD player names");
        FriendlyNameHUD = Config.Bind("Settings", "Friendly Player Names", new Color(0.19f, 0.58f, 1f, 1f),
            "Friendly HUD player names");
        DisableAllyInfoHover = Config.Bind("Settings", "Disable Vanilla Friendly Hover Names",
            true, "Disable the new 0.34 vanilla feature showing the name of a friendly player you hover over.");
        EnemyNameHUD = Config.Bind("Settings", "Enemy Player Names", new Color(1f, 0.13f, 0.05f, 1f),
            "Enemy HUD player names");
        FriendlyUnitsHUD = Config.Bind("Settings", "Friendly Units", new Color(0.19f, 0.58f, 1f, 1f),
            "Friendly HUD unit icons");
        EnemyUnitsHUD = Config.Bind("Settings", "Enemy Units", new Color(1f, 0.13f, 0.05f, 1f), "Enemy HUD unit icons");
        NeutralUnitsHUD = Config.Bind("Settings", "Neutral Units", new Color(0.6f, 0.6f, 0.6f, 1f),
            "Neutral HUD unit icons");
        AAUnitsHUD = Config.Bind("Settings", "Enemy AA Units", new Color(1f, 0.369f, 1f, 1f),
            "Tint for enemy AA/SAM/CIWS units on HUD & Map");
        SpecialAAUnitsHUD = Config.Bind("Settings", "Enemy AA (Special) Units", new Color(0f, 1f, 1f, 1f),
            "Tint for enemy Special AA units on HUD & Map (CRAM/LADS/HEL/Radar/Boltstrike)");
        ShowMAPNames = Config.Bind("Settings", "Show Map Player Names", true, "Toggle map player names");
        FriendlyNameMAP = Config.Bind("Settings", "Friendly Player Names (MAP)", new Color(0.19f, 0.58f, 1f, 1f),
            "Friendly map player names");
        EnemyNameMAP = Config.Bind("Settings", "Enemy Player Names (MAP)", new Color(1f, 0.13f, 0.05f, 1f),
            "Enemy map player names");
        HUDNameFontSize = Config.Bind("Settings", "HUD Player Name Font Size", 14, "Font size for HUD player names");
        HUDNameOffset = Config.Bind("Settings", "HUD Player Name Vertical Offset", 25f,
            "Vertical offset above HUD icons");
        MAPNameFontSize = Config.Bind("Settings", "MAP Player Name Font Size", 14, "Font size for MAP player names");
        MAPNameOffset = Config.Bind("Settings", "MAP Player Name Vertical Offset", 5f,
            "Vertical offset above MAP icons");
        
        var aaWhiteList = new AAConfigReadWrite(Path.Combine(Paths.ConfigPath,
            "com.hellcat92.vanillaiconsplus_AA_Whitelist.cfg"), Logger);
        
        aaWhiteList.ReadAAList();
        
        _harmony = new Harmony("com.hellcat92.vanillaiconsplus");
        _harmony.PatchAll();
        FriendlyUnitsHUD.SettingChanged += delegate
        {
            ApplyHUDTints();
            RefreshHUDIcons();
            RefreshMapIcons();
        };
        EnemyUnitsHUD.SettingChanged += delegate
        {
            ApplyHUDTints();
            RefreshHUDIcons();
            RefreshMapIcons();
        };
        NeutralUnitsHUD.SettingChanged += delegate
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
        Log.LogInfo($"{Info.Metadata.Name} v{Info.Metadata.Version} loaded.");
    }
    
    
    internal static void ApplyHUDTints()
    {
        var gameAssets = Resources.FindObjectsOfTypeAll<GameAssets>().FirstOrDefault() ?? GameAssets.i;
        if (gameAssets == null)
        {
            Log.LogWarning("GameAssets not found.");
            return;
        }
        
        gameAssets.HUDFriendly = Instance.FriendlyUnitsHUD.Value;
        gameAssets.HUDHostile = Instance.EnemyUnitsHUD.Value;
        gameAssets.HUDNeutral = Instance.NeutralUnitsHUD.Value;
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
                var color = flag3 ? Instance.NeutralUnitsHUD.Value :
                    !flag4 ? Instance.FriendlyUnitsHUD.Value : Instance.EnemyUnitsHUD.Value;
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