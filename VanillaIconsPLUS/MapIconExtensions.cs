using System.Runtime.CompilerServices;
using TMPro;

namespace VanillaIconsPLUS;

public static class MapIconExtensions
{
    public static readonly ConditionalWeakTable<UnitMapIcon, NameHolder> table = new();
    
    public static NameHolder GetHolder(this UnitMapIcon icon) => table.GetOrCreateValue(icon);
    
    public static TextMeshProUGUI GetLabel(this UnitMapIcon icon) => icon.GetHolder().label;
    
    public static void SetLabel(this UnitMapIcon icon, TextMeshProUGUI label)
    {
        icon.GetHolder().label = label;
    }
    
    public class NameHolder
    {
        public TMP_FontAsset font;
        public TextMeshProUGUI label;
    }
}