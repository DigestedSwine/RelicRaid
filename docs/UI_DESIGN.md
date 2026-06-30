# Relic Raid — UI Design Spec

> **Handoff doc.** Authored in the UI-design chat; implemented by the Unity build chat.
> Design lives here as the source of truth; the build chat implements + verifies in the live project.
> This is a living doc — update it as the skin evolves. Keep it in `docs/` (not `Assets/`).

## Direction
- **Game:** top-down (45–60°) fantasy ARPG, mobile-first (landscape) + PC. North star: Diablo Immortal.
- **Look:** stylized fantasy, **flat** (matches the flat-shaded Tripo art — no gradients/glows in the UI itself; "glow" = in-engine emission+bloom, not UI). Dark stone/wood panels, ornate gold/bronze frames, parchment text.
- **Tech:** UI Toolkit primary (code-built today). Direction: build a real **USS/.tss theme layer** so styling is data-driven + iterable; keep components in C#, migrate to UXML opportunistically. uGUI stays only for the touch joystick + world-space NPC bars.

## Theme tokens (target USS custom properties)
Define once in the shared `.tss`/USS theme; everything references these. Values are starting points — tune in-engine.

### Color
| Token | Hex | Use |
|---|---|---|
| `--rr-bg-panel` | `#241f17` | Panel/frame background (dark wood) |
| `--rr-bg-panel-deep` | `#0e0c08` | Bar track / inset wells |
| `--rr-frame` | `#b88a3e` | Ornate frame border (bronze-gold) |
| `--rr-frame-lit` | `#e7c98a` | Highlighted/active frame (gold) |
| `--rr-text` | `#ecdfc0` | Primary parchment text |
| `--rr-text-muted` | `#a99a7d` | Secondary/labels |
| `--rr-hp` | `#bf3b2c` | Health fill |
| `--rr-mp` | `#3a7bbf` | Mana fill |
| `--rr-xp` | `#d4a72c` | XP fill |
| `--rr-cast` | `#3fb0a0` | Mining/cast/crystal (teal) |
| `--rr-enemy` | `#8e2f6e` | Enemy/boss HP fill |
| `--rr-crystal` | `#6fd6c2` | Wellspring crystal accent |

Faction accents (swap `--rr-frame-lit` per character/screen context): Oakhaven `#7fc69a`, Crownsworn `#e7c98a`, Ironfrost `#7fb5d6`.

### Type / spacing / shape
| Token | Value | Use |
|---|---|---|
| `--rr-font-display` | fantasy display font (TBD) | Headers, names, big numbers |
| `--rr-font-body` | clean readable sans | Body, stat numbers, tooltips |
| `--rr-radius` | 6–8px | Panel/button corners |
| `--rr-frame-width` | 2px | Default ornate border width |
| `--rr-gap` | 8px | Component-internal gap |
| `--rr-pad` | 7–10px | Panel inner padding |

Readability beats flair on mobile — numbers in the clean body font, ornament in frames/headers only.

## HUD components (see mockup)
Landscape; left thumb = movement, right thumb = abilities.
- **Player frame** (top-left): portrait (class icon), name, level, HP bar, MP bar, thin XP bar. *States:* HP/MP/XP fill %, level text.
- **Boss/target frame** (top-center): enemy name + wide HP bar. *Shown only when a target/boss is engaged.* Uses `--rr-enemy`.
- **Minimap** (top-right): framed map, dots (enemies `--rr-hp`, crystals `--rr-cast`, player gold ring), Wellspring crystal count below.
- **Options ⚙** (right, under minimap): opens existing gear popup (zoom, indestructible, refill).
- **Cast / mining bar** (center, above abilities): label + progress fill (`--rr-cast`). *Shown only while channeling/mining.*
- **Touch joystick** (bottom-left): dashed ring + thumb knob. uGUI On-Screen Stick, touch only.
- **Ability bar** (bottom-right): 4 ability buttons (keybind 1–4) + 1 larger primary-attack orb. *States per button:* icon, cooldown overlay (darken + remaining seconds), MP-unaffordable (desaturate), keybind label, tappable.

## Art assets needed
Source from a **fantasy/RPG UI kit** (Asset Store / itch) for coherence; AI-generate bespoke gaps. Use kit **art only** (sprites/icons), implemented in UI Toolkit via USS `background-image` + 9-slice.
- **9-slice frames:** panel, bar track, bar fill, button (normal/active), minimap frame.
- **Ability icons:** one per skill (placeholder: Tabler outline icons in the mockup).
- **Class/faction crests:** 12 class icons + 3 faction marks.
- **Fonts:** 1 display + 1 body (license-cleared for the build target).

## Implementation notes (for the build chat)
- **USS-first:** move visual props out of C# helpers (`UIX.Radius/Border`) into USS classes keyed to the tokens above. Hot-reload + reskin-in-one-file is the goal.
- **9-slice** all frames/bars so they scale to any size.
- **Incremental:** new screens use UXML+USS+UI Builder; migrate PlayerHUD/menus when already touching them — no big-bang rewrite.
- **Mobile:** flex/% layouts, big tap targets, safe-area aware.
- **Verify** by walking the visual tree (UI Toolkit doesn't render in MCP screenshots) — the mockup here is the visual target.

## Open questions
- Orbs vs bars for HP/MP? (Mockup uses bars to match current PlayerHUD; orbs are the more iconic fantasy choice — decide.)
- Which UI kit (or AI-gen) for frame art?
- Final display + body fonts.
- Does the ability bar count flex per class kit (number of abilities)?

## Front-end launch flow
```
Splash → Login → Server select → Realm select → Character select/create → Main Menu hub → Play
```
**Assumptions (pending user confirmation):** "Realm" = faction (Crownsworn/Oakhaven/Ironfrost). Realm chosen before character → **class list filtered to that realm's 4 classes** (one per role). Minimal appearance customization for now (class mesh = the look). Login + Server are thin stubs until Nakama is wired.

### Login
Account sign-in (email/pass) + mobile social (Apple/Google) + guest/play; create-account, forgot-password, logo. *(Nakama backend; stub "Play" button pre-Nakama.)*

### Server select
Region/server list with status (population, ping, online); recommended/auto-pick. *(Single stubbed server now; design the list for later.)*

### Realm select (identity screen — do it well)
3 realms, each: crest, name, lore blurb, playstyle summary (Crownsworn = holy/disciplined; Oakhaven = nature/attrition; Ironfrost = fury/strength), and later a population/balance indicator. **Permanent choice — warn the player.**

### Character select / create
- **Select:** existing characters on this realm — portrait, class, level, last played → enter or "+ Create".
- **Create (mocked — see character-create mockup):** layout = left character preview + right [4 class cards by role → description panel → name field + create]. **Class select with description** is the centerpiece per user request.
  - Class card: portrait/role-icon, name, role tag; selected = `--rr-frame-lit` border + lit bg.
  - Description panel (selected class): role/range badge, playstyle paragraph, 2 signature abilities (icon + name). Oakhaven data used in mock: Thornguard/Tank, Thicketblade/Melee, Rotweaver/Caster (Creeping Decay + Thorn Nova — matches built wizard abilities), Grovekeeper/Healer.
  - Name field + randomize (dice) + Create button.

### Front-end open questions
- Realm: per-character or account/server-locked? (3-faction PVP often restricts faction-hopping.)
- Appearance customization depth (gender variant / tint) or class-as-look only?
- Class-select layout: cards-by-role (mocked) vs a 3D rotating preview of each class?
