# Data guide: changing the game without compiling

The numbers, levels, words, stage order and sprite sizes live in plain files in `ShiftJamContent/Data/`.
You can change them without touching the C# code and see the result with one key press.

## The fast loop (desktop)

Run the game pointing at the data folder, so it reads your files directly instead of the built copies:

```bash
SHIFTJAM_DATA=ShiftJamContent/Data dotnet run --project ShiftJam.SDL2.GL
```

On Windows (PowerShell):

```powershell
$env:SHIFTJAM_DATA = "ShiftJamContent/Data"; dotnet run --project ShiftJam.SDL2.GL
```

Edit a file, save, switch to the game and press **F11**. It re-reads every data file and rebuilds the current stage
(a "DATA RELOADED" note shows in the corner). Nothing is recompiled. Without `SHIFTJAM_DATA` the game reads the copies
built into `Content/Data`, which is what the web build and normal builds always use.

If a file has a mistake, the game does not crash: it prints a `[data]` line in the terminal saying what it could not
read and uses the built-in default, so you can fix the file and press F11 again.

Run `python3 tools/sync_content.py` after **adding or removing** a data file (not after editing one), then build.

## The files

| File | What it controls |
|---|---|
| `balance.json` | Every tunable number: move speed, jump, gravity, health, damage, timings, ranges, pickup amounts, boss attack timings and attack order |
| `levels/*.txt` | The maps: one text file per level, boss arena, Keep room and the Clerk's catacombs |
| `campaign.json` | The order of the stages, their names, and which level each uses |
| `text.json` | The words on screen: title, banners, ending, door prompts |
| `sprites.json` | Each animation strip's frame size, frame count, speed and feet position |

### balance.json

Numbers grouped by what they belong to. The group `Tuning` is Joseph and the world (`WalkSpeed`, `JumpVelocity`,
`Gravity`, `MaxKi`, `ShiftKiCost`, hit-stop and shake). The groups `Shell`, `Wraith`, `Hollowed`, `Flicker`, `Pell`,
`Dova` and `Clerk` are the enemies and bosses; `Pickup` is the Ki, health and shard orbs.

```json
"Tuning": { "WalkSpeed": 80, "JumpVelocity": 260 },
"Clerk":  { "BarHealth": 96 }
```

- Times are in seconds, distances in pixels, speeds in pixels per second. 16 pixels is one tile.
- You only need to keep the lines you want to change. A missing line uses the built-in default, so deleting a line
  puts it back.
- Jump height in pixels is about `JumpVelocity squared / (2 x Gravity)`. At 260 and 600 that is 56 px, about 3.5 tiles.
- Attack order is a list, for example `"Pell": { "SeqOne": ["Swing", "Throw", "Lunge"] }`. The names are the attacks
  the boss already knows; an unknown name is skipped with a message. The Clerk's attacks are `"Kind Mode"`, for
  example `"Stamp Both"`: kinds are Swipe, Stamp, Fan, Rain, Vanish and Wave, and modes are Own (its plane),
  Opp (the other plane) and Both. Its lists are `Bar1`, `Bar2`, `Bar3`, `VaultMaterial` and `VaultAstral`.

Some values are used once when a boss is created (for example a boss's health and phase thresholds), so change
those and then press F11, or restart the stage, to see them.

### levels/*.txt

```
@name The Lantern Streets
; Lines starting with a semicolon are notes.
##############
#..P.....S..X#
##############
```

- Every other line is one row of the map, top to bottom. `@name` and similar `@` lines are settings.
- Legend: `#` solid, `.` empty, `w` wall that only blocks the Material plane, `a` block that only exists in the Astral
  plane, `x` spikes, `g` gate, `P` player start, `X` level exit, `D` dummy, `S` Shell, `R` Wraith, `H` Hollowed,
  `F` Flicker, `k` Ki orb, `h` health orb, `o` shard. Boss arenas also use `B` (the boss). The Keep hub uses `1` to `4`
  for the doors and the rooms use `E` for the way back. The Clerk's catacombs use `@roomwidth` and `@rooms`.
- Rows do not need to be the same length (short ones are filled with empty). The left, right and top edges are
  walls; the bottom is open, so a gap in the floor is a pit.
- Reach: jump about 3.5 tiles up and about 4.8 tiles across at walking speed. Keep steps to 3 tiles or less.

New level? Add `levels/mine.txt`, add a line to `campaign.json` with `"build": "level:mine"`, run
`python3 tools/sync_content.py`, and you can play it.

### campaign.json

```json
{ "name": "The Cellars", "kind": "level", "build": "level:cellars" }
```

The list order is the play order. `kind` is `level` or `boss` (it changes the "cleared" wording). `build` is one of
`level:<file>`, `boss:pell`, `boss:dova`, `boss:clerk` or `keep`. The debug keys F1 to F8 jump to stages 1 to 8.

### text.json

Keys and their words, such as `"title.name": "THRESHOLD"`. The built-in font draws capital letters, digits and
. ! - : ? / only. Delete a line to get the built-in wording back.

### sprites.json

One line per strip, such as `{ "name": "joseph_walk", "w": 32, "h": 48, "frames": 4, "fps": 8, "loop": true, "originY": 48 }`.
The PNG is `ShiftJamContent/Sprites/<character>/<animation>.png` (for `joseph_walk`, that is `Sprites/joseph/walk.png`), `frames` equal frames of `w` by `h` side by side. Changing the numbers
needs no build; a new or changed PNG does (see the art guide).

## Still in the code

These are not data files yet: colours and shapes of anything drawn by code (tiles, pickups, doors, the HUD, hazards),
the built-in font's letter shapes, the keyboard layout, and the rules of how attacks and AI work. The art guide lists
what is still drawn by the game. The keyboard and gamepad buttons are in `ShiftJam.Game/Input/InputReader.cs`.
