# Quest Recovery (Endless Legend 2)

Quest Recovery is a small **single-player recovery mod** for *Endless Legend 2*.

It provides a simple in-game button that lets you **mark a stuck major faction quest step as completed**, allowing the quest chain to continue when a quest becomes blocked or fails to progress correctly during early access.

This mod is designed to **unblock stuck quests**, not to automate gameplay or replace normal progression.

---

## What this mod does

- Marks the **current major faction quest step** as completed so the quest chain can continue
- Uses the game’s internal quest progression logic
- Requires an explicit, in-game click (no automatic actions)

**Limitations:**
- ❌ No automation or bulk quest skipping
- ❌ No save-file editing
- ❌ Disabled in multiplayer (SP-only; no actions available)

---

![Quest Recovery overlay in the Quest window](screenshot.jpg)

---

## Installation

### 1. Install BepInEx
1. Download **BepInEx 5.x (Windows x64)**  
   https://github.com/BepInEx/BepInEx/releases

   BepInEx is a commonly used mod loader for Unity games. It does not modify game files and can be removed at any time.

2. Extract it into your *Endless Legend 2* install directory
3. Launch the game once to generate the `BepInEx` folders

The game directory will typically be located at:
```
C:\Program Files (x86)\Steam\steamapps\common\ENDLESS Legend 2\
```

---

### 2. Install Quest Recovery
1. Download `QuestRecovery_v1.1.0.zip`
   https://github.com/AdBergman/EL2Mods/releases/tag/v1.1.0
2. Extract the contents into the *same directory that contains* `ENDLESS Legend 2.exe`  
   (so that `BepInEx/plugins/QuestRecovery/EL2.QuestRecovery.dll` exists)
3. Launch the game

---

## Usage

1. Open the **Quest** window in-game
2. A small **Quest Recovery** panel will appear
3. If a recoverable quest step is detected, click **Complete Quest**
4. The action is applied once and then locked until the quest state updates

If the quest UI does not update immediately, end the turn or perform any action that triggers a game refresh (for example selecting an army with movement remaining).

---

## Uninstall

Delete the `QuestRecovery` folder from: 
```
BepInEx/plugins/
```

---

## Troubleshooting

### The Quest Recovery panel does not appear

If the mod is installed correctly but the Quest Recovery panel is not visible, check the following:

#### 1. Confirm BepInEx and the mod are loading
After launching the game once, check that this file exists:
```
ENDLESS Legend 2\BepInEx\LogOutput.log
```
You should see log entries similar to the following:
```
[Info   :EL2 Quest Recovery] EL2 Quest Recovery loaded.
[Info   :EL2 Quest Recovery] [Safety] SinglePlayer (sandbox snapshot: remoteLocal=0, remoteReplicated=0, serverId=0x0000000000000000, netSync=Unknown, remoteIds=[])
[Info   :EL2 Quest Recovery] [FactionQuest] index=93 status=InProgress stepIndex=1 def=FactionQuest_KinOfSheredyn_Chapter05_Step01 turnStart=88 pendingChoices=null
```

---

#### 2. Make sure you are opening the full Quest screen
The Quest Recovery panel only appears in the **full Quest window** (opened with `J`), not in smaller objective or notification panels.

---

#### 3. The panel Overlaps UI elements
The panel is **draggable** and will remember its position between games.

---

#### 4. The Quest completes but laters steps do not start
Some quests depend on earlier conditions being met.

If you manually complete a quest step that normally requires a specific action (for example building the Holy Oculum district), a later step that checks for that condition may not trigger as expected.

---

## Release notes

### v1.1.0

**New**
- Optional **Details / Quest Goals** view
- Shows raw quest goal information
- Includes a **Copy** button for bug reporting
- Disabled by default and has no effect unless explicitly enabled

**Improved**
- Quest completion now attempts to **finalize quest rewards correctly**
- This includes most common rewards such as resources, items, units, faction traits, and councillors

**Changed**
- Renamed **Skip Quest** to **Complete Quest**
- Improved internal safety checks to prevent repeated triggers

**Notes**
- The Details view is intentionally technical and may expose raw quest data
- It can be safely ignored if you only need to unblock quest progression

---

## Notes

This is a recovery tool for edge cases.  
Always keep backups of important saves when using mods.

---

## License

MIT License. See the root `LICENSE` file for details.
