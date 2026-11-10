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

Levels 3 to 5 reuse these four enemy types in new combinations (see Levels and bosses). New enemy types are **not** planned for them, to protect the schedule.

## Levels and bosses (Mega Man style)

Five levels, each ending in a boss. Each boss is a notable townsperson who has been hollowed. Defeating the boss frees them, they move into the hub, and they give you a **charm** (the Mega Man "boss power" is a Ki or shift upgrade).

| # | Level | Shape | Enemies | Boss (freed afterwards) | Charm theme |
|---|---|---|---|---|---|
| 1 | **The Lantern Streets** | Horizontal | Shells, Wraiths (one plane each) | **Pell**, lantern-keeper. Lanterns decide which plane is safe. | Cheaper shift |
| 2 | **The Cellars** | Descends | Hollowed, Flickers (both planes) | **Dova**, market stall owner. Throws goods that exist in one plane only. | Faster Ki recovery |
| 3 | **The Temple Stairs** | Horizontal plus climbing **[stretch: upward]** | Mix of all four | **The Abbess** (new). Fights from the Astral Plane, so you must stay there. | Longer Astral stay |
| 4 | **The Bell Tower** | Descends | Mix, plus hazards that flip plane on a timer | **Brother Hask**, bell-ringer. Each bell toll forces a plane swap. | Wider Perfect Shift window |
| 5 | **The Tithe's Hall** | Both | Everything | **Master Oss**, then **the Tithe's Clerk** (final boss) | Ending |

**Progression:** levels 1 and 2 are played in order, since they teach the plane rules. After that, levels 3 and 4 can be played in either order (Mega Man level select), then level 5. Decided: level select for levels 3 and 4. Each of them must work without the other's charm, since you may arrive first or second.

**Boss rules:** every boss has two phases, one in each plane, so each boss fight is a shift-timing test. Bosses reuse the player's existing move set and a few shared attack patterns, with different timings and art.

### Scope warning

Five levels in two weeks for one coder is tight. To make it feasible:
- **Levels are data, not code.** One level file format (tiles, enemy spawns, plane-wall markers), so a new level is mostly layout work.
- **Levels are short:** about 60 to 90 seconds each, one screen-height section plus a boss room.
- **Finish levels 1 and 2 plus a boss first.** That is a complete game on its own. Levels 3 to 5 are only added if levels 1 and 2 are polished.
- **Bosses share one base class** with a phase table (plane, pattern, timing).

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
| 6-7 | Level file format, Level 1 with Shells and Wraiths. |
| 8 | Boss base class, Pell (level 1 boss). **Checkpoint: a complete one-level game.** |
| 9-10 | Level 2 (descending), Hollowed, Flickers, Dova. **Checkpoint: the "safe" release.** |
| 11-12 | Levels 3 and 4 as far as time allows, using existing enemies. |
| 13 | Level 5 (Oss and the Clerk) or a shorter finale. Hub and charms. |
| 14 | Polish, tuning, web build, itch.io page. |

**Cut order if late:** level 5 (end the game with a shorter finale after level 4), then level 4, then level 3, then hub and charms, then the Flicker. Two levels with two bosses is the guaranteed minimum.

## Open questions

1. Accept the Hollowing premise and the "laziness caused it" comic angle?
2. Is the monk Joseph or a renamed version of Joseph (the campaign character)?
3. How much of the campaign's inside humour goes into the text?
4. Public wording: "Material Plane" and "Astral Plane" appear in D&D and older myth. Keep trademarked D&D terms out of public text.
