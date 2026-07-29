using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;

namespace VanillaIconsPLUS;

internal sealed class AAConfigReadWrite
{
    private readonly ManualLogSource _log;
    private readonly string _path;
    
    //private readonly string AAListPath = Path.Combine(Paths.ConfigPath, "com.hellcat92.vanillaiconsplus_AA_Whitelist.cfg");
    
    internal AAConfigReadWrite(string path, ManualLogSource log)
    {
        _path = path;
        _log = log;
    }
    
    internal void ReadAAList()
    {
        AAUnitHelper.RestoreDefaultAAUnitNames();
        AAUnitHelper.RestoreDefaultSpecialAAUnitNames();
        
        if (!File.Exists(_path))
        {
            WriteDefaultAAList();
            return;
        }
        
        try
        {
            var regularUnitNames = new HashSet<string>(
                StringComparer.Ordinal);
            
            // Start with the defaults for backward compatibility with old files
            // that do not yet contain a [Special AA Units] section
            var specialUnitNames = new HashSet<string>(
                AAUnitHelper.SpecialAAUnitNames,
                StringComparer.Ordinal);
            
            var currentSection = AAListSection.Regular;
            var foundSpecialSection = false;
            
            foreach (var rawLine in File.ReadLines(_path))
            {
                var line = rawLine.Trim();
                
                if (string.IsNullOrWhiteSpace(line) ||
                    line.StartsWith("#", StringComparison.Ordinal))
                    continue;
                
                if (line.Equals(
                        "[AA Units]",
                        StringComparison.OrdinalIgnoreCase))
                {
                    currentSection = AAListSection.Regular;
                    continue;
                }
                
                if (line.Equals(
                        "[Special AA Units]",
                        StringComparison.OrdinalIgnoreCase))
                {
                    currentSection = AAListSection.Special;
                    
                    // The presence of this section means its contents should
                    // replace the built-in defaults, even when it is empty
                    if (!foundSpecialSection)
                    {
                        specialUnitNames.Clear();
                        foundSpecialSection = true;
                    }
                    
                    continue;
                }
                
                // Ignore unrecognised section headers rather than treating
                // something like [Unknown Section] as a unit name
                if (line.StartsWith("[", StringComparison.Ordinal) &&
                    line.EndsWith("]", StringComparison.Ordinal))
                {
                    _log.LogWarning(
                        $"Ignoring unknown AA list section {line} in {_path}.");
                    
                    continue;
                }
                
                switch (currentSection)
                {
                    case AAListSection.Regular:
                        regularUnitNames.Add(line);
                        break;
                    
                    case AAListSection.Special:
                        specialUnitNames.Add(line);
                        break;
                    
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            
            var overlappingNames = regularUnitNames
                .Intersect(specialUnitNames, StringComparer.Ordinal)
                .OrderBy(name => name)
                .ToArray();
            
            if (overlappingNames.Length > 0)
                _log.LogWarning(
                    "The following units are listed as both regular and special AA units. " +
                    $"They will be treated as special AA units: {string.Join(", ", overlappingNames)}");
            
            AAUnitHelper.SetAAUnitLists(
                regularUnitNames,
                specialUnitNames);
            
            _log.LogDebug(
                $"Loaded {AAUnitHelper.AAUnitNames.Count} regular AA unit names and " +
                $"{AAUnitHelper.SpecialAAUnitNames.Count} special AA unit names " +
                $"from {_path}.");
            
            if (!foundSpecialSection)
                _log.LogInfo(
                    $"The AA list at {_path} uses the old format. " +
                    "All file entries were loaded as regular AA units, and the " +
                    "built-in special AA list was retained.");
        }
        catch (Exception e)
        {
            _log.LogError(
                $"Could not read AA list file {_path}. " +
                $"Using the built-in default lists instead: {e}");
        }
    }
    
    private void WriteDefaultAAList()
    {
        try
        {
            var lines = new List<string>
            {
                "# Vanilla Icons PLUS AA Unit Whitelist",
                "# One unit name per line.",
                "# Units listed under [Special AA Units] use the special AA colour.",
                "# A unit should not be present in both sections.",
                "",
                "[AA Units]"
            };
            
            lines.AddRange(
                AAUnitHelper.AAUnitNames.OrderBy(name => name));
            
            lines.Add("");
            lines.Add("[Special AA Units]");
            
            lines.AddRange(
                AAUnitHelper.SpecialAAUnitNames.OrderBy(name => name));
            
            File.WriteAllLines(_path, lines);
            
            _log.LogDebug(
                $"AA whitelist file created at {_path}.");
        }
        catch (Exception e)
        {
            // Both collections already contain their built-in defaults,
            // so the plugin can continue.
            _log.LogError(
                $"Could not create AA list file {_path}. " +
                $"The built-in default lists will still be used: {e}");
        }
    }
    
    private enum AAListSection
    {
        Regular,
        Special
    }
}