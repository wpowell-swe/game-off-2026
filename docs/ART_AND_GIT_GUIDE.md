# Art and git guide (for the artist)

This is a starter guide. The aim is that you can draw sprites and get them into the game yourself, learning git on small safe tasks.

## Sprite basics (change these if you prefer; tell the coder)

- Style: 1980s arcade. Small palette (about 8 to 16 colours), hard edges, no anti-aliasing.
- Joseph: 32x48 px frame. Tiles: 16x16 px.
- Draw once. The Astral Plane look is the same sprites with a palette swap, done in code, so you do not draw a second set.
- File type: PNG with transparent background. One sprite sheet per character, frames in a row, left to right.
- Name files like `joseph_idle.png`, `joseph_walk.png`, `shell_idle.png`.

## First art tasks

1. Joseph idle (2 frames) and walk (4 frames).
2. One Shell (the Material-only enemy), idle and walk.
3. A street tileset: floor, wall, a window, a lantern.

Then the Wraith (the Astral-only enemy).

## Where files go

Put finished PNGs in `ShiftJamContent/Sprites/`. The coder adds them to the content list (`ShiftJamContent.mgcb`), so just tell them when a file is ready.

## git in six steps

1. `git switch main` then `git pull` to get the latest.
2. `git switch -c art/joseph-idle` makes a branch for one piece of work.
3. Add your PNG to `ShiftJamContent/Sprites/`.
4. `git add ShiftJamContent/Sprites/joseph_idle.png` then `git commit -m "Add Joseph idle sprite"`.
5. `git push -u origin art/joseph-idle`
6. Open a pull request on GitHub so the coder can look before it joins `main`.

Tips: one branch per piece of work, small commits, and ask before deleting anything. Credit any asset you did not make yourself in `CREDITS.md`.
