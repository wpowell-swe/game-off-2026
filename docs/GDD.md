# ShiftJam: Game Design Document (draft 2)

Working title: **Threshold** (rename freely). Game Off 2026, theme **SHIFT**.

> **Status:** draft 2, updated with your answers on scope, camera, Ki, enemy layout, tone and art. Items marked **[DECIDE]** are still open.

## Constraints

- **Team:** 2 people. You own design, code and the first pass of everything. Your friend does sprite art and is learning git.
- **Time:** about 2 weeks (one sprint).
- **Look:** 2D sprites, 1980s arcade feel.
- **Tone:** dark, dry comedy. The game comes from a D&D campaign, and the monk is based on your friend's character, **Joseph**.

## One-line pitch

A 2D scrolling brawler where a monk can step into the Astral Plane and back, and every fight is a question of *which plane should I be standing in right now?*

## Pillars

1. **Shift is the whole game.** Every enemy and room is built around *when* you shift.
2. **Shifting is expensive.** A big up-front Ki cost makes you commit. Staying costs little, so the choice is "is this worth it", not "hurry".
3. **Teach one thing at a time.** Level 1 teaches each plane on its own. Level 2 mixes them.
4. **Small and finished.** Two levels and a boss, done well, beats five levels half done.

## Why Joseph fights

**[DECIDE] Premise: "Hollowing", now with a drier edge.**

A collector called **the Tithe** is stealing the *souls* of the mountain town **Lanternfall**. A stolen soul is torn into the Astral Plane while the body keeps working, as a **Hollowed**, obedient and armed. Joseph is the last monk of a small order, **the Quiet Hand**, whose one secret art is stepping between the Material and Astral Planes. The Tithe hollowed the order first, including Joseph's teacher, **Master Oss**.

Three reasons, in rising personal cost:

1. **Rescue.** Beat a Hollowed in the Material Plane to break its guard, then shift to free its soul. Freed people return to a safe hub and give Joseph charms (upgrades).
2. **Duty.** The Quiet Hand's vow: *no one is left behind the Threshold.* Joseph says it grimly and a little sarcastically.
3. **Guilt.** Joseph saw the first signs and told no one, because it would have meant paperwork. **[DECIDE]** The comic version is that Joseph's laziness caused this, and the story is about not being that person any more.

Why it works with the mechanic: bodies live in the Material Plane and souls in the Astral, so rescuing someone needs both.

## Camera and level shape

- 2D scrolling, **one lane** (no walking up and down the screen). Easier to read and to build.
- Levels scroll **horizontally and downward** (descending stairs, shafts, wells). Both are in the base game.
- **Stretch goal:** vertical *upward* sections.
- Astral movement ignores some walls and floors, so downward drops and hidden routes are a natural use of the shift.

## Controls

Move, jump, light attack, heavy attack, block (Material only), **Shift**.

## The planes

| | Material Plane | Astral Plane |
|---|---|---|
| Look | Warm, solid, detailed | Cold blue-violet, translucent outlines |
| You can hit | Bodies | Souls |
| Defence | Block works | No block, but you pass through body attacks |
| Movement | Normal | Pass through marked walls and floors |
| Ki | Recovers | Drains slowly |

### Ki and health (two meters)

- **Health:** lost to hits. Recovered only by pickups and the hub.
- **Ki:** 100 max.
  - **Shift into Astral:** large up-front cost (starting guess **40**).
  - **Staying in Astral:** small drain (starting guess **4 per second**).
  - **Shift back to Material:** free.
  - **Material:** Ki recovers (starting guess **10 per second**).
  - Not enough Ki to shift means you can't. If Ki runs out in Astral you are pulled back to Material and briefly stunned.

These are tuning numbers, kept in one data file so you can change them without touching code. Charms (items) adjust them.

## Enemies

**Level 1: one plane at a time.**

| Enemy | Plane | Behaviour |
|---|---|---|
| **Shell** | Material only | An empty body, no soul left. Hit it in Material. It cannot see you in the Astral Plane. |
| **Wraith** | Astral only | A drifting soul with no body. Hit it in Astral. It cannot hurt you in the Material Plane. |

Level 1 teaches: fight Shells in Material, shift to deal with Wraiths, and shift to get past Astral-only obstacles.

**Level 2: both planes at once.**

| Enemy | Behaviour |
|---|---|
| **Hollowed** | Body plus soul. Break guard in Material, shift, free the soul (the main rescue loop). |
| **Flicker** | Swaps plane on a visible beat (ring pulse). Hit it only while you share its plane. |

**Boss (end of level 2): the Tithe's Clerk.** Alternates planes and uses every rule from both levels. **[DECIDE]** The final boss, the Tithe itself, is out of scope for this sprint.

## Content from the AI pipeline

The pipeline in `/pipeline` generates text the game loads from `Content/Generated`:
- **Dialogue** for freed townsfolk, Master Oss and the Clerk.
- **Items (charms)** that change Ki or shift rules.
- **Next kinds to add:** enemy descriptors and level intro text.

## Art direction

- **Style:** 1980s arcade sprites, small palette. Planned sizes (change if your friend prefers): character 32x48 px, tiles 16x16 px.
- **Two looks, one set of sprites:** the Astral Plane is the same art with a palette swap, so each sprite is drawn once.
- **Animations Joseph needs:** idle, walk, jump, light attack, heavy attack, block, shift (in and out), hurt.
- **First art tasks for your friend:** Joseph idle and walk, a Shell, a tileset for the street. This also gives them a small, reviewable git task.

## Scope plan (2 weeks)

| Days | Goal |
|---|---|
| 1-3 | Joseph moves, jumps, attacks. Material only. One test room. |
| 4-5 | Shift, Ki, the Astral look, Astral-only wall passage. |
| 6-8 | Level 1: Shells, Wraiths, level layout, dialogue in game. |
| 9-11 | Level 2: Hollowed, Flickers, descending section. |
| 12-13 | Boss, hub with charms, audio, tuning. |
| 14 | Polish, web build, itch.io page. |

**Cut order if late:** hub and charms, Flicker, boss complexity, then Level 2 length.

## Open questions

1. Accept the Hollowing premise and the "laziness caused it" comic angle?
2. Is the monk Joseph or a renamed version of Joseph (the campaign character)?
3. How much of the campaign's inside humour goes into the text?
4. Public wording: "Material Plane" and "Astral Plane" appear in D&D and older myth. Keep trademarked D&D terms out of public text.
