using System.Collections;
using HarmonyLib;

namespace VanillaIconsPLUS;

[HarmonyPatch(typeof(DynamicMap), nameof(DynamicMap.SetFaction))]
public static class Patch_Map_SetFaction
{
    private static void Postfix(DynamicMap __instance)
    {
        Plugin.Instance.StartCoroutine(Delayed());
    }
    
    private static IEnumerator Delayed()
    {
        yield return null;
        Plugin.ApplyHUDTints();
        Plugin.RefreshHUDIcons();
        Plugin.RefreshMapIcons();
    }
}