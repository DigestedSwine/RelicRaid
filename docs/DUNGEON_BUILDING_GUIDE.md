# Dungeon Building Guide
*How we build levels together: you shape the space, I wire the systems.*

---

## The division of labor

Building falls into two halves that play to different strengths:

- **You (in the Unity editor):** layout, room shapes, where things go, how it *feels*. Judging spatial flow, pacing, and aesthetics needs eyes on the scene — that's your job and you're fast at it.
- **Me (via the Unity MCP):** building the reusable components, bulk-placing and wiring them on request, and verifying systems fire. Hand-placing geometry by coordinate is my weakest area; wiring and logic is my strongest.

**The loop:** you grayblock a space → you describe what to populate/wire → I place & wire it → we screenshot/playtest → repeat.

---

## Part 1 — Working in the Unity editor (your half)

### Core tools (hotkeys)
| Key | Tool | Notes |
|---|---|---|
| **W** | Move | drag colored arrows / center planes |
| **E** | Rotate | drag rings |
| **R** | Scale | drag cube handles |
| **Q** | Pan view | moves the camera, not the object |
| **F** | Focus | frames the selected object — use constantly |

### Snapping (makes placement clean)
- **Hold Ctrl while moving** → snap to grid steps (set in *Edit ▸ Grid and Snap*). Use for walls that line up.
- **Hold V and drag** → *vertex snap*: stick one corner exactly to another. Best for butting walls together with no gaps.
- **Hold Shift+Ctrl while dragging** → snap onto the surface under the cursor (drop a spawner right on the floor).

### Top-down authoring view
Click the **green Y cone** on the scene gizmo (top-right axis widget), then click its center cube to toggle **orthographic** — now you author in the same view the game uses.

### Placing objects (three ways)
1. **Primitives / empties:** `GameObject ▸ 3D Object ▸ Cube/Plane/…` (primitives include colliders).
2. **Prefabs:** drag from `Assets/Prefabs` (Bear, Wizard) into the Scene/Hierarchy.
3. **Toolkit components:** create an empty, position it, then **Inspector ▸ Add Component ▸** type the name (e.g. `EnemySpawner`), and fill its fields. Gizmos show ranges/links in the Scene.

Make anything reusable: drag a configured Scene object **into a Project folder** → it becomes a prefab.

### Sculpting the environment
- **Outdoor / organic** (the grass field): select the **Terrain** object → its Inspector toolbar has brushes: *Raise/Lower* (Shift = lower), *Smooth*, *Paint Texture*, *Trees*, *Details*. Tune Brush Size/Opacity. *(Terrain module is installed.)*
- **Dungeon interiors:** **ProBuilder** is the right tool (make a cube, extrude faces into rooms/halls) — **not installed yet; ask me to add it.** Or zero-install grayblock with scaled **Cubes** snapped together.

### See-through walls (so walls never hide the player)
Any wall that should fade when it's between the camera and the player needs **two things**:
1. **Layer = `Occluder`**
2. **Material using the `DitherFadeLit` shader** (e.g. `Assets/Shaders/WallFade.mat`)

That's it — the `CameraOcclusionFader` on the Main Camera does the rest (SphereCasts to the player and dither-fades any Occluder wall in the way, restoring it when clear). Tune on the camera: `fadedAmount` (how see-through), `fadeSpeed`, `castRadius` (how wide a cone counts as blocking). There's a `TestWall` in the scene as a live example (deletable).

### A first grayblock (no install needed)
1. `GameObject ▸ 3D Object ▸ Plane` → scale to a room floor.
2. Add **Cubes**, scale thin/tall into walls, **V-snap** to floor edges.
3. Set floor/walls **Layer** so the player + NPC ground-raycast land on them (that's what `groundMask` reads).
4. Drop an **EnemySpawner** and a **ResourceNode** in the room.
5. **Play** → bear spawns grounded; walk in and fight.

---

## Part 2 — How to direct me (placement & wiring)

You don't need coordinates — I read the live scene (names, positions, components) and act through the MCP.

### Telling me locations (loosest → tightest)
1. **Relative description** (usually enough): *"a bear spawner 4m in front of the Wellspring node, count 3."*
2. **Markers you place by eye** (best for precise spots): drop empties where you want things, name them clearly (`Spawn_A`, `BossArenaCenter`, `PortalSpot`), then *"put a spawner on each Spawn_ empty."*
3. **Selection:** select objects in the editor and say *"wire the three I selected into a pack."*
4. **Exact coords:** *"at (12, 0, -5)"* — supported, rarely needed.

### Telling me what to wire
Name the objects + the relationship:
- *"Make an EncounterPack from BearSpawner_A/B/C, and have the Corridor Gate require it cleared."*
- *"Set the boss's portal spawn point to the empty named PortalSpot."*

### The one habit that keeps this clean
**Name things meaningfully in the Hierarchy.** `EntryRoom_BearPack_Left` beats `EnemySpawner (3)`.

### Safety loop
Before committing I **echo back what I understood**, then place/wire, then we **top-down screenshot** to confirm. Ask me to *"print the scene hierarchy"* anytime to see exact names to reference.

---

## Part 3 — The dungeon toolkit (components to place)

All values are serialized in the Inspector — nothing hardcoded. Gizmos draw in the Scene view.

### Enemies & encounters
- **`EnemySpawner`** — `enemyPrefab`, `count`, `scatterRadius`, `spawnOnStart`, `enemiesRespawn` (turn OFF in the boss room). Gizmo: spawn area.
- **`EncounterPack`** — `spawners[]`; fires when all its enemies are dead (gate hook). `alertTogether` is a reserved hook (pack-alert not wired yet).
- **`NPCController`** (on the enemy prefab) — detection/attack/leash radii, moveSpeed, attack timing, `groundMask` (set to your floor layer), foot grounding. Already on the Bear.

### Progression / gating
- **`Gate`** — `condition` = PacksCleared / CrystalsMined / Manual; assign `requiredPacks[]` or `crystalsRequired`; assign a `barrier` object that disables when it opens. Or call `Open()` from a switch.
- **Wellspring node** = **`ResourceNode`** (logic: Full/Depleted/Respawning, yield, channel/respawn times) + **`ResourceNodeVisual`** (assign full/depleted GameObjects — reuse on any future static asset). Player mines via **`Miner`**.

### Boss & exit
- **`BossController`** (needs `HealthComponent`, give it NO `Respawner`) — `phases[]` each with `hpThreshold` (e.g. 0.5, 0.25), optional `moveSpeed`/`attackMultiplier`/`phaseVfx`; `exitPortalPrefab` + `portalSpawnPoint` spawned on death.
- **`ExitPortal`** (trigger collider) — detects the player; the HUD shows the confirm, DungeonManager handles the exit.
- **`DungeonManager`** — the instance lifecycle (active → boss defeated → auto-close, 30 min). Reused by Mode 2.

### Shared combat/reward pieces (already built)
- **`HealthComponent`** (HP/MP/DoT) on every fighter; **`EnemyIdentity`** (name/kind/xpReward) on enemies for kill credit.
- **`Party`** grants all rewards group-wide — kills (damage-tagged, multi-party share) and mining flow through it into each member's **`PlayerProgress`** (resources, XP/level, kill tallies).

---

## Existing prefabs / assets
- `Assets/Prefabs/Bear.prefab` — enemy (Generic-rigged quadruped, AI + grounding).
- `Assets/Prefabs/Wizard.prefab` — player hero.
- The glowing relic in the scene = the Wellspring/ResourceNode demo.
- Tripo asset import pipeline notes live in Claude's memory (`reference_tripo_asset_pipeline`).

---

*Camera is locked TOP-DOWN — design rooms open/wide with forgiving ceilings and sightlines that read from above.*
