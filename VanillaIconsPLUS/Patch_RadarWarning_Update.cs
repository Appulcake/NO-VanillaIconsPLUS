using System.Collections;
using System.Reflection;
using HarmonyLib;

namespace VanillaIconsPLUS;

[HarmonyPatch(typeof(RadarWarning), "Update")]
public static class Patch_RadarWarning_Update
{
    private static readonly FieldInfo JammingLookupField = AccessTools.Field(typeof(RadarWarning), "jammingIconLookup");
    
    private static void Postfix(RadarWarning __instance)
    {
        if (__instance == null || JammingLookupField == null ||
            !(JammingLookupField.GetValue(__instance) is IDictionary dictionary)) return;
        JamState.JammedUnits.Clear();
        foreach (DictionaryEntry item in dictionary)
        {
            var unit = item.Key as Unit;
            if (unit != null) JamState.JammedUnits.Add(unit);
        }
        
        JamState.PlayerIsJammed = JamState.JammedUnits.Count > 0;
    }
}