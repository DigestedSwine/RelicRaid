# Relic Raid — Build Plan

*Last updated: 2026-06-13*

This is the working engineering plan. It captures the agreed direction, the architecture, and a phased, vertical-slice-first roadmap. It is meant to be edited as decisions change.

---

## 1. The Game (one paragraph)

Relic Raid is a **mobile, top-down (45–60° isometric) fantasy action game** where each player controls **one hero** belonging to **one of three factions**. There are two pillars:

- **PVP:** 5v5v5 three-faction battles.
- **PVE:** 5-player co-op, handcrafted, WoW-style **dungeons** you progress through — fighting *or* sneaking past NPC guards to reach minable resources.

Both pillars run on **one shared combat system** and feed **one shared progression**. North-star reference: **Diablo Immortal**.

---

## 2. Core Pillars & Decisions (locked)

| Decision | Value |
|---|---|
| Player control | One hero (not army command) |
| Camera | Top-down, 45–60° |
| Factions | 3 — Camelot Accord, Ironfjord Clan, Sylvaran Court (from GDD) |
| PVP scale | 5v5v5 (15 players) — start here, scale later |
| PVP modes | Session-based big map (build first) + persistent frontier (design for, add later) |
| PVE | 5-player co-op, **handcrafted** dungeons; fight-or-sneak; mining payoff |
| Mining node placement | Authored **per-dungeon** (vault-at-the-end *or* veins-throughout) |
| Stealth depth | Start **light/opportunistic** (co-op makes hardcore stealth frustrating) |
| Platform | iOS + Android **+ PC** (project already has Mobile *and* PC URP pipelines configured). PC is the lowest-friction target (no Mac/app store) and a strong *first* launch candidate. Cross-play between PC and mobile = OPEN question (Fusion supports it). Design input-agnostic (touch + mouse/keyboard via new Input System). |
| Engine | Unity 6 (6000.0.27f1), URP |

---

## 3. Architecture: the "Instance Core"

The central idea that keeps the game buildable: **the gameplay instance is mode-agnostic.**

> Spin up an instance → players act → report a result → persist it.

Everything is an *instance type* wrapping that one lifecycle:

- **PVP battle** (5v5v5) — result: win/loss + stats → mastery/rewards
- **PVE dungeon** (5-player co-op) — result: resources + loot → stash/progression

The instance never knows *what launched it* or *what happens after*. That seam is what lets us:

1. Build **session PVP** now and add **persistent frontier PVP** later as a different wrapper.
2. Reuse the **same combat system** for PVE guards and PVP heroes.
3. **Stub the world/territory data model now** (unused until frontier mode exists) so there's no migration later.

**Golden rule:** *Design for both PVP modes. Build one first. Never build both at once.*

### Shared layers (needed by everything, build early)
- **Combat core** — movement, abilities, damage, death.
- **Hero/progression data** — stats, skill trees, faction mastery.
- **Networking** — Photon Fusion, server-authoritative.

---

## 4. Build Roadmap (vertical-slice-first)

The order is deliberate: prove *fun* before *scale*, and learn *networking on forgiving co-op* before *competitive PVP*.

### Phase 0 — Combat Core (greybox, single-player, no networking) ← START HERE
The make-or-break loop. Zero art, zero netcode.
- Top-down camera rig at 45–60°
- Hero capsule, tap/click-to-move on a NavMesh
- One ability (basic attack) with cooldown + damage
- Health/damage system
- A dummy enemy with HP that dies
- Greybox test arena
- **Exit criteria:** "move → attack → kill" *feels good*. This is the question that decides the whole game.

### Phase 1 — Hero Foundation & First Kit
- One playable hero (one faction) with 2–3 abilities, resources (HP/mana/cooldowns)
- Basic enemy AI: a guard that detects, chases, and attacks
- **Exit criteria:** one hero feels like a *character*, not a capsule.

### Phase 2 — PVE Dungeon Vertical Slice (single-player first)
- One handcrafted greybox dungeon: rooms, patrolling guards, a boss
- **Fight-or-sneak**: enemy vision cones + aggro/alert states (the finicky system — prototype early)
- Mining nodes + a result/reward screen
- **Exit criteria:** a complete solo dungeon run is fun. Stealth feels fair *or* we cut it.

### Phase 3 — Networking Foundation (Photon Fusion) — co-op PVE first
- Install Fusion; get 2 players synced; then 5-player co-op in the dungeon
- Server-authoritative combat + resource validation (anti-cheat)
- **Why co-op first:** cooperative sync is far more forgiving than competitive — learn netcode here.
- **Exit criteria:** 5 players clear the Phase 2 dungeon together, synced.

### Phase 4 — PVP Session Mode (5v5v5)
- Session wrapper: hub → matchmaking → 5v5v5 battle on a large open map → win condition → rewards
- This is the harder netcode, attempted *after* co-op experience.
- **Exit criteria:** a real 5v5v5 match plays and resolves.

### Phase 5 — Progression & The Loop
- Hero progression + faction mastery (from GDD)
- **Decide what mined resources DO** (see Open Questions) and wire PVE rewards → hero power → PVP
- **Exit criteria:** PVE and PVP feed each other. There is a reason to do both.

### Phase 6 — Content & Polish
- 3 factions × heroes, more dungeons, more PVP maps
- Art pass (asset packs — see Asset Strategy), UI (UI Toolkit per GDD)
- Mobile optimization (LODs, GPU instancing, occlusion, texture compression)

### Phase 7 — Mobile Build & Soft Launch
- Android build first (directly from the Dell)
- iOS later (**requires a Mac** — Mac Mini M4 ~$599 is the clean path)
- Device testing, live-ops scaffolding

### Designed-for-later (NOT built in this plan)
- Persistent frontier PVP (the world/territory data model is stubbed in Phase 4)
- Battle sizes beyond 15

---

## 5. Asset Strategy

**Build in greybox now; add art late.** Combat, dungeons, and PVP can all be proven with primitives.

| Asset type | Source | Notes |
|---|---|---|
| Hero/humanoid models | AI-gen (full-body, **A-pose**) → **Mixamo** for rig + animations | Top-down hides AI-gen's weaknesses |
| Creatures/bosses (dragon, spider) | **Buy pre-rigged + animated packs** (Malbers, Synty) | Do NOT hand-rig as a beginner; Unity is not a rigging tool |
| Dungeon + environment | **Coherent asset kit** (Synty POLYGON Dungeon/Nature) | One kit = matching style; I assemble layouts/patrols |
| World / terrain | Modular mesh tiles preferred over Unity Terrain for mobile | Terrain engine is heavy on mobile |
| One-off props | AI-gen (single object) | Fine for a unique relic; not for a coherent cast |

**Key rigging rule:** pick **one humanoid skeleton** (Mixamo standard) and rig every humanoid to it → the whole animation library is reusable across heroes.

**Gear/armor:** use **whole-outfit mesh swaps on a shared skeleton** (works with AI-gen tiers *and* a future modular pack). Avoid relying on AI-gen for modular, mix-and-match armor — it produces single fused meshes.

---

## 6. Current Project State (verified 2026-06-13)

**Engine/config (good, keep):**
- Unity 6 `6000.0.27f1`, URP with **Mobile + PC** pipelines configured
- New Input System, AI Navigation (NavMesh), Timeline, Test Framework installed
- **Photon Fusion NOT installed yet** (Phase 3)

**Existing code — RTS-flavored, mostly SHELVED by the pivot:**
- `GameManager.cs` (gold economy), `LassoController.cs` + `PolygonUtils.cs` (army lasso-select), `UnitGroup.cs`/`UnitRegistry.cs`
- *Partially reusable:* `Unit.cs` (HP/damage/move shapes), `UnitData.cs` + `FactionData.cs` (ScriptableObject schemas) — can be repurposed for heroes.

**Empty/scaffold:** ScriptableObject instances (none created), `UI/` (empty), only default `SampleScene`, no prefabs, no art.

**NOT part of the game — ignore/move out:** `PROTOTYPE_ASSEMBLY_GUIDE.md` and `CYD_Commander*` folders are an unrelated hardware project (MTG life-tracker electronics).

---

## 7. Open Questions

1. **What do mined resources DO?** — crafting materials / currency / **faction war effort** (last option ties PVE directly to the 3-faction PVP). Decide in Phase 5. Likely a combination.
2. **Faction mechanics for solo heroes** — GDD's Chivalric Bond (buff aura), Bloodrage (attack speed on hit taken), Arcane Harmony (shared mana) were army mechanics. They likely translate to **party-level** mechanics (buff the 5-player party). Confirm in Phase 1/5.
3. **Stealth final depth** — start light; revisit after Phase 2 playtest.
4. **Backend/database** — for accounts, progression, persistence. NOT PlanetScale (no free tier since 2024). Consider Supabase/Firebase. Decide before Phase 5.

---

## 8. Tooling Note

To let Claude read/drive Unity directly (scene hierarchy, console, screenshots, edits), install an existing open-source **Unity MCP server** — do **not** build a bridge from scratch (mature ones exist: CoplayDev/unity-mcp, CoderGamester/mcp-unity). This is optional but high-leverage for debugging.

---

## 9. Immediate Next Step

**Phase 0, Combat Core.** Scaffold the greybox: camera rig, hero, tap-to-move, one attack, damage/death, a dummy enemy, a test arena. Prove the combat feels good before anything else gets built.
