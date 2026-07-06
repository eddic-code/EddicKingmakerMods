# EddicKingmakerRespec

A character respec mod for **Pathfinder: Kingmaker** (Enhanced Plus Edition).

Lets you respec your companions from level 0, letting you choose their classes, archetypes, stats, skills, feats and spells. Doesn't touch background traits: portrait, name, gender, voice, appearance and birthday stay as they were.

## Features

- Press **F2** to open the respec character selector. No gold cost, no time skip, no forced party rest.
- Story companions rebuild from level 0.
- **Companion background traits are kept.** The mod hides the portrait, name, gender and voice steps of the companion respec screen and carries the original values through.
- **Your main character uses the game's full respec.** You re-choose everything — race, gender, appearance, portrait, name, voice and alignment — exactly as the Enhanced Edition intends.
- **Races are handled for you.** Companions automatically keep their own race and racial features (including races you can't normally pick, like Nok-Nok's goblin). Races with a floating +2 bonus place it where the companion's original build had it.
- **Built on the game's own respec.** The mod uses the Enhanced Edition's built-in respecialization as much as it can.

## Hotkeys

| **F2** | Open the respec character selector (works outside combat, in a loaded area) |

The hotkey can be changed: open the Unity Mod Manager window (**Ctrl+F10**), find EddicKingmakerRespec, and click the key button next to "Open respec selector" to bind any key you prefer.

Pick a character in the window and confirm — the regular level-up interface opens and you rebuild them from there. The respec screen covers level 1; the remaining levels are then taken as normal level-ups with the banked experience.

## Requirements

- Pathfinder: Kingmaker — **Enhanced Plus Edition** (the free 2.1 update; the mod relies on its built-in respec system)
- [Unity Mod Manager](https://www.nexusmods.com/site/mods/21) (UMM)

## Installation

### With Unity Mod Manager (recommended)

1. Install Unity Mod Manager and point it at your Pathfinder: Kingmaker folder.
2. Open the **Mods** tab in UMM and drag the downloaded `EddicKingmakerRespec.zip` onto it.
3. Launch the game. The mod appears in the UMM window (Ctrl+F10) and is on by default.

### Manual

1. Install Unity Mod Manager and point it at your Pathfinder: Kingmaker folder.
2. Extract the archive into the game's `Mods` folder so that you end up with:

   ...\Steam\steamapps\common\Pathfinder Kingmaker\Mods\EddicKingmakerRespec\EddicKingmakerRespec.dll
   ...\Steam\steamapps\common\Pathfinder Kingmaker\Mods\EddicKingmakerRespec\Info.json

3. Launch the game and check the UMM window (Ctrl+F10) to confirm the mod is loaded and enabled.

## Uninstalling

Toggle the mod off in the UMM window, or delete the `Mods\EddicKingmakerRespec` folder. The mod adds no new items, feats or other content to your save. Respecs you already completed are plain vanilla character data, so saves remain safe with the mod removed.

## Good to know

- Respec requires the character to be alive, out of combat, and in a loaded area (it doesn't work on the world map).
- Class rules still apply: alignment-restricted classes check the character's actual alignment, so Lawful Neutral Valerie still can't become a Paladin.
- Companions' race, alignment and background traits are intentionally locked — that's the "partial" respec. The main character (and hired mercenaries) get the unrestricted vanilla respec instead, so re-enter their name and appearance during the rebuild.
