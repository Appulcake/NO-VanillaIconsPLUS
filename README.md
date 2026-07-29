# NO-VanillaIconsPLUS
Mod for Nuclear Option - QoL mod to toggle player names on and off and change icon colours in HUD and MAP.

Fork additions:
- 0.34 support (migration of PlayerName and Text => TextMeshProUGUI)
- Config file is now unified name (`com.hellcat92.vanillaiconsplus.cfg`) so doesn't change per plugin update, keeps using existing config
- Separate AA Unit vs Special AA Unit colouring possibility (in case you want to colour all AA units separate from regular enemy units, but then also separate special cases like R SAM sites)
- User-facing separate config to allow changing what units belong to AA unit / special AA unit whitelist, set up in `BepInEx/config/com.hellcat92.vanillaiconsplus_AA_Whitelist.cfg`
- Config to disable new 0.34 vanilla feature that shows ally name when hovering over them, as this also is a duplicate functionality with VIP's own HUD unit names (on by default)
