using System.Runtime.CompilerServices;
using TMPro;

namespace VanillaIconsPLUS;

public static class HUDUnitMarkerExtensions
{
    public static readonly ConditionalWeakTable<HUDUnitMarker, NameHolder> table = new();
    
    public static NameHolder GetHolder(this HUDUnitMarker marker) => table.GetOrCreateValue(marker);
    
    public static TextMeshProUGUI GetLabel(this HUDUnitMarker marker) => marker.GetHolder().label;
    
    public static void SetLabel(this HUDUnitMarker marker, TextMeshProUGUI label)
    {
        marker.GetHolder().label = label;
    }
    
    public class NameHolder
    {
        public TMP_FontAsset font;
        public TextMeshProUGUI label;
        
        public float spawnTime;
    }
}