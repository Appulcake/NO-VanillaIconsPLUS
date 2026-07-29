using System.Reflection;
using HarmonyLib;
using UnityEngine.UI;

namespace VanillaIconsPLUS;

public static class MapIconHelpers
{
    private static readonly FieldInfo ImageField = AccessTools.Field(typeof(UnitMapIcon), "iconImage");
    
    public static Image GetImage(this UnitMapIcon icon) => ImageField?.GetValue(icon) as Image;
}