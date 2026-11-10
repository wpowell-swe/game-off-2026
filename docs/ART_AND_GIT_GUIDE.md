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
| `dummy` | 24x32 | idle (1), hurt (1) |

Example: `joseph_walk.png` is 128x48 (4 frames of 32x48). The exact list is in `ShiftJam.Game/Gfx/SpriteDefs.cs`; if you want a different size or frame count, ask the coder to change it there.

Placeholder strips already exist for all of these. Replace one by saving your own PNG with the **same name**.

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

## Suggested first art tasks

Joseph idle and walk, then a Shell, then a street tileset (floor, wall, window, lantern), then the Wraith.
