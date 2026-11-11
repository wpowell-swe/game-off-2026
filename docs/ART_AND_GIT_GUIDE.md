# Art and git guide (for the artist)

You draw PNGs, drop them in a folder, run two commands, and see them in the game. No code changes needed.

## The rules

- Style: 1980s arcade. Small palette (about 8 to 16 colours), hard edges, no anti-aliasing, transparent background.
- One PNG per animation: all frames in a single row, left to right, every frame the same size.
- **Draw everything facing RIGHT.** The game mirrors it for left.
- Feet go on the bottom edge of the frame, centred. The game lines the feet up with the character's hitbox, so keep the body centred horizontally in the frame.
- Draw once. The Astral (blue-violet) look is made from the same sprites by the game, so do not draw a second set. Tip: strong light and dark values in the sprite make the Astral look better.

## File names and sizes

Put files in `ShiftJamContent/Sprites/` named `<character>_<animation>.png`. The image width is the frame width times the number of frames.

| Character | Frame size | Animations (frames) |
|---|---|---|
| `joseph` | 32x48 | idle (2), walk (4), jump (1), light (3), heavy (4), block (1), hurt (1), shift (2), dead (1) |
| `shell` | 32x48 | walk (4), windup (2), attack (2), hurt (1) |
| `wraith` | 24x32 (body sits in the top 24 rows; the tail hangs below) | float (2), lunge (2) |
| `hollowed` | 32x48 (hitbox 16x30) | idle (2), walk (4), windup (2), attack (2), hurt (1), broken (2), freed (1) |
| `flicker` | 32x48 (hitbox 14x28) | idle (2), walk (4), windup (2), attack (2), hurt (1) |
| `soul` | 24x32 (the wisp tail is the bottom centre) | float (2) |
| `dummy` | 24x32 | idle (1), hurt (1) |

Notes on the newer ones:

- **`hollowed`** is an ordinary townsperson with a soul torn into the Astral plane. `broken` is the slump after you break its guard (the soul is exposed then). `freed` is sitting, relieved, once the soul is released. The little guard bar over its head is drawn by the game.
- **`soul`** is that Astral soul, a small pale wisp. It is only visible while the body is broken. The game tints it, so draw it in normal colours.
- **`flicker`** swaps between the Material and Astral planes every couple of seconds. You draw one set; the game tints the Astral half and draws the warning ring.

Example: `joseph_walk.png` is 128x48 (4 frames of 32x48). The exact list is `ShiftJamContent/Data/sprites.json`. If you want a different size or frame count, edit the numbers there (no coder needed; see `docs/DATA_GUIDE.md`) and press F11 in the game.

Placeholder strips already exist for all of the characters in the table above, including the Hollowed, Flicker and soul (not the bosses, see below). `python3 tools/make_placeholder_sprites.py` writes any that are missing and never overwrites your art. Replace one by saving your own PNG with the **same name**.

### Bosses

All three bosses use the same six animations, so you draw the same set for each. The frame is bigger than the hitbox so weapons and hats fit; keep the feet on the bottom edge, centred, as always.

| Boss | Frame size | Hitbox | Look (from the story) |
|---|---|---|---|
| `pell` | 48x56 | 18x36 | Pell, the lantern-keeper of the Lantern Streets. A pole with a lantern, a worn robe. |
| `dova` | 48x56 | 20x34 | Dova, who runs the market stall in the Cellars. Stout, apron with a price tag, hair in a bun. |
| `clerk` | 40x56 | 18x30 | The Tithe's Clerk, the final boss. Tall and thin, a long dark coat, a tall hat, a blank face, ink stains and a big stamp. Never raises its voice. |

For each boss the animations are:

| Animation | Frames | When it plays |
|---|---|---|
| `idle` | 2 | Standing, waiting, between attacks (loops) |
| `walk` | 4 | Moving toward the player (loops) |
| `windup` | 2 | The warning before an attack: raising the weapon, gathering up. Make it readable, the player dodges by it |
| `attack` | 2 | The strike itself and the follow-through |
| `hurt` | 1 | The flinch when a health bar empties or a phase changes |
| `freed` | 1 | Relieved and calm, as the boss is freed. They are hollowed townspeople, not villains |

Files are named `<boss>_<animation>.png`, for example `pell_windup.png` (96x56, two frames of 48x56) or `clerk_idle.png` (80x56).

Things to know about the bosses:

- **Astral phases:** the game tints your normal sprites blue-violet for the Astral phases, so you still draw one set. Some bosses float in their Astral phases; the same frames are used.
- **Vanishing:** when a boss vanishes (Dova's receipts, the Clerk's reappear attack) the game simply hides the sprite. You do not draw a vanish frame.
- **Attack colours:** the warning boxes on the floor are drawn by the game (orange for Material, blue for Astral, violet for both planes). Your `windup` frames sit on top of them.
- **No placeholders for bosses:** until you add a PNG, a boss is drawn by the game as coloured blocks, so you can add one animation at a time. If a pose has no PNG, the game uses `idle`, and if there is no `idle` either it uses the blocks.

## Getting your art into the game

1. Save the PNG into `ShiftJamContent/Sprites/`.
2. In a terminal in the project folder: `python3 tools/sync_content.py` (adds new files to the content list).
3. Then: `dotnet build`, and run the game.

You only need step 2 when you add or rename a file, not when you just change a PNG you already have.

### Linux only: one-time FreeImage setup

The content builder needs the FreeImage library, and on Fedora it is installed under a name .NET does not look for. Do this once:

```bash
sudo dnf install -y freeimage
mkdir -p ~/.local/lib/freeimage
ln -sf /usr/lib64/libfreeimage.so.3 ~/.local/lib/freeimage/libFreeImage.so
echo 'export LD_LIBRARY_PATH=$HOME/.local/lib/freeimage:$LD_LIBRARY_PATH' >> ~/.bashrc
```

Open a new terminal afterwards. Windows and macOS usually need nothing.

## If something is missing or wrong

If an animation has no PNG (or its size is wrong), the game just draws the old coloured rectangle for that character instead. Nothing crashes, so you can add one animation at a time. A message in the console says how many strips were loaded.

## git in six steps

1. `git switch main` then `git pull` to get the latest.
2. `git switch -c art/joseph-idle` makes a branch for one piece of work.
3. Add your PNG to `ShiftJamContent/Sprites/`, then run `python3 tools/sync_content.py`.
4. `git add ShiftJamContent/Sprites/joseph_idle.png ShiftJamContent/ShiftJamContent.mgcb` then `git commit -m "Add Joseph idle sprite"`.
5. `git push -u origin art/joseph-idle`
6. Open a pull request on GitHub so the coder can look before it joins `main`.

Tips: one branch per piece of work, small commits, and ask before deleting anything. Credit any asset you did not make yourself in `CREDITS.md`.

## Art the game still draws by itself

These are made from code today, so there is nothing to put in a folder yet. Ask the coder to hook one up when you want to draw it:

- **Tiles:** floors and walls, the Material-only walls, the Astral blocks, spikes and the gates.
- **Pickups:** the Ki diamond, the heart for health, the shard (a quarter circle).
- **Doors, the HUD and the font.**
- **The icon** (`tools/make_icon.py`).

The title screen is the exception: replace `ShiftJamContent/Ui/title.png` (320x180) with your own, run `python3 tools/sync_content.py`, and it shows up.

## Suggested first art tasks

Joseph idle and walk, then a Shell, then a street tileset (floor, wall, window, lantern), then the Wraith. After that, the three bosses: Pell, Dova and the Clerk, an idle and a windup for each first, since those are what the player reads the most.
