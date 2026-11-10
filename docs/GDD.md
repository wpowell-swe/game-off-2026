# ShiftJam: Game Design Document (draft 1)

Working title: **Threshold** (rename freely). Game Off 2026, theme **SHIFT**.

> **Status:** draft written by Claude from the brief "side-scrolling beat-em-up, monk who shifts between the material and astral planes, challenge is timing the shift, needs a reason to fight". Everything marked **[DECIDE]** is a proposal for you and your friend to accept or change.

## One-line pitch

A side-scrolling brawler where one monk lives in two planes at once, and every fight is a question of *which plane should I be standing in right now?*

## Pillars

1. **Shift is the whole game.** Every enemy, hazard and room is built around when you shift, not just whether you can.
2. **Two planes, two rulesets.** The same room plays differently in each plane. Learn both.
3. **Every enemy is a person.** Defeating one frees someone, which is the reason to fight (see Motivation).
4. **Small and finished.** One monk, one town, five stages, one boss. One wow, done well.

## Why the monk fights (the missing piece)

**[DECIDE] Proposed premise: "Hollowing".**

A collector called **the Tithe** is stealing the *souls* of a mountain town, **Lanternfall**. A stolen soul does not die: it is torn loose into the Astral Plane while its body keeps walking in the Material Plane as a **Husk**, obedient and armed. Husks are the enemies.

The monk, **Wren**, is the last member of a small order, **the Quiet Hand**, whose one secret art is walking in both planes at once. The Tithe hollowed the rest of the order first, including Wren's teacher **Master Oss**, who is now the second-to-last boss.

Wren fights for three reasons, in increasing order of personal cost:

1. **Rescue.** Beating a Husk is not killing: you strike the body to break its guard and the *soul* to free it. A freed soul returns to its person, who becomes a named NPC in the safe hub. Your fights literally repopulate the town.
2. **Duty.** The Quiet Hand's vow: *no one is left behind the Threshold.*
3. **Guilt.** Wren saw the first signs of the Hollowing and said nothing to the order, hoping to deal with it alone. **[DECIDE]** How much Wren knew is the story's central question; the proposal is that Wren suspected and stayed quiet.

Why this suits the mechanic: the reason to fight in *both* planes is built into the premise. Bodies live in the Material Plane, souls in the Astral. You need both to save someone.

Alternatives if this does not fit **[DECIDE]**:
- **Debt:** Wren's own soul is mortgaged to the Tithe, and each freed soul pays down the debt.
- **Tournament:** a ritual contest between plane-walkers. Simpler, weaker emotional pull.

## Core loop

Enter a street or room, read the enemies, fight by shifting at the right beat, free the souls, move on. Between stages, return to Lanternfall to meet the people you freed, spend what you found, and pick one upgrade.

## Moment to moment

**Genre:** 2D side-scrolling beat-em-up, one player (local co-op is a stretch goal, **not** in scope).

**Controls (keyboard / pad):** move, light attack, heavy attack, jump, block, **Shift**.

### The planes

| | Material Plane | Astral Plane |
|---|---|---|
| Look | Warm, solid, detailed | Cold, translucent, glowing outlines |
| You can hit | Bodies (break guard, stagger) | Souls (free, finishing blow) |
| You can't | Touch souls, pass walls | Touch bodies, use doors or items |
| Defence | Block works, you take full damage from bodies | No block, but pass through most body attacks and walls |
| Resource | Recovers **Ki** | Drains **Ki** every second |
| Danger | Husks, traps | Astral wraiths, soul-hazards |

**Shift** is instant but has a short recovery. A shift made just as an attack lands is a **Perfect Shift** (brief invulnerability and a damage bonus). Shifting with no Ki left snaps you back to Material.

### Why timing matters

Enemies are designed so neither plane is always right:

- **Husk (basic):** armoured body in Material. Its guard breaks only from Material hits, its soul can only be freed from Astral. Standard rhythm: break guard, shift, finish.
- **Flicker:** swaps plane on a visible beat (a ring pulse). Hit it only while it shares your plane. Teaches reading the tell.
- **Wraith:** lives in the Astral and only hurts you there. Ignore it in Material, but it blocks the path.
- **Anchor:** a heavy Husk whose body shields its soul in Material; shifting to Astral while its attack is mid-swing lets you pass through and strike the soul.
- **Tide pulses:** some rooms flip a hazard (spikes, fire, pits) between planes on a timer, so the safe plane changes.

**[DECIDE]** Hit points: one health bar plus Ki, or a single combined meter? Proposal: two meters (health, Ki) because Ki is the decision pressure.

## Structure and scope

| Stage | Setting | New idea |
|---|---|---|
| 1 | Lanternfall lower street | Husks, break-guard then free |
| 2 | Market | Flickers, plane-swapping crates |
| 3 | Temple stairs | Wraiths, Astral-only bridges |
| 4 | Bell tower | Anchors, tide hazards |
| 5 | The Tithe's hall | Boss, uses every rule |

Plus the hub (Lanternfall square) between stages. **Cut list if we run late:** local co-op, stage 2, Perfect Shift bonus, upgrades.

## Content produced by the AI pipeline

The pipeline (`/pipeline`) generates text the game loads from `Content/Generated`:

- **Dialogue:** rescued townsfolk, Master Oss, the Tithe.
- **Items:** charms and relics, each with a `shiftEffect` that bends the plane rules slightly.
- **Next kinds to add:** enemy descriptors (name, plane behaviour, tell), stage intro text.

All generated text must pass the schema and the reviewer agent, and uses the lore in `pipeline/lore/`.

## Art and audio direction **[DECIDE]**

Pixel art, two palettes: warm and saturated for Material, desaturated blue-violet with glow for Astral. A single shader or palette swap shifts the whole screen. Sound: a low hum in Astral, muffled and tonal; sharp and dry in Material.

## Open questions

1. Accept the Hollowing premise, or choose an alternative?
2. Is Wren the final name, and is the monk's gender or background specified?
3. Perfect Shift: reward or cut?
4. Public naming: the Material and Astral Plane terms are common to D&D and older myth; avoid "D&D" and Wizards of the Coast terms in public text.
