using System.Collections.Generic;

namespace VanillaIconsPLUS;

public static class JamState
{
    public static readonly HashSet<Unit> JammedUnits = new();
    
    public static bool PlayerIsJammed;
}