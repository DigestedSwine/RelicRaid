using Fusion;
using UnityEngine;

// Turns the real Wizard into a networked avatar. In Shared Mode each client spawns its own Wizard with
// state authority. The LOCAL (authority) instance runs the normal single-player components (input, movement,
// abilities, camera, HUD); REMOTE proxies disable those and just receive synced position (NetworkTransform)
// + locomotion animation (NetSpeed). Combat triggers/death sync come in a later pass.
public class NetworkPlayer : NetworkBehaviour
{
    [Networked] public float NetSpeed { get; set; }
    [Networked] public NetworkString<_32> PlayerName { get; set; }   // shown on the nameplate over other players

    Animator animator;
    HeroController hero;
    static readonly int SpeedHash = Animator.StringToHash("Speed");

    public override void Spawned()
    {
        animator = GetComponentInChildren<Animator>();
        hero = GetComponent<HeroController>();
        bool local = HasStateAuthority;

        // CRITICAL: the InputReader is a SHARED ScriptableObject. A proxy's HeroController.OnDisable would
        // call input.Disable() and kill input for THIS device's local player. Detach it from the proxy first.
        if (!local && hero != null) hero.input = null;

        // Local-only control components — off on remote proxies (they're driven by the network instead).
        Toggle<HeroController>(local);
        Toggle<MeleeAttacker>(local);
        Toggle<SkillCaster>(local);
        Toggle<Miner>(local);
        Toggle<PlayerTargeting>(local);
        var cc = GetComponent<CharacterController>(); if (cc != null) cc.enabled = local;
        if (hero != null) hero.networkDriven = local;   // local player moves from the net tick (FixedUpdateNetwork)

        // Minimap: you're "Self" (centers the map); everyone else is a realm ally for now. A party system will
        // promote same-party players to PartyMember at runtime — same marker, different type.
        var mark = GetComponent<MinimapMarker>();
        if (mark != null) { mark.type = local ? MinimapMarkerType.Self : MinimapMarkerType.RealmAlly; mark.clampToEdge = !local; }

        if (local)
        {
            var boot = UnityEngine.Object.FindFirstObjectByType<NetworkBootstrap>();
            if (boot != null) PlayerName = boot.PlayerName;
            WireLocalPlayer();
        }
    }

    void Toggle<T>(bool on) where T : UnityEngine.Behaviour { var c = GetComponent<T>(); if (c != null) c.enabled = on; }

    // Re-point the scene's camera / HUD / menu at THIS player (it spawned at runtime, so the refs are stale).
    void WireLocalPlayer()
    {
        var hp = GetComponent<HealthComponent>();
        var cam = Camera.main;
        var cf = cam != null ? cam.GetComponent<CameraFollow>() : null;
        if (cf != null) cf.target = transform;
        var hero = GetComponent<HeroController>();
        if (hero != null && cam != null) hero.cameraTransform = cam.transform;
        if (hero != null && hero.input != null) hero.input.Enable();   // make sure THIS device's input is live

        var hud = UnityEngine.Object.FindFirstObjectByType<PlayerHUD>();
        if (hud != null)
        {
            hud.player = hp;
            hud.caster = GetComponent<SkillCaster>();
            hud.miner = GetComponent<Miner>();
            hud.progress = GetComponent<PlayerProgress>();
            hud.targeting = GetComponent<PlayerTargeting>();
            hud.cameraFollow = cf;
            hud.Rebuild();   // re-bind the ability bar etc. to this player's components
        }

        var menu = UnityEngine.Object.FindFirstObjectByType<MenuManager>();
        if (menu != null)
        {
            menu.player = hp;
            menu.progress = GetComponent<PlayerProgress>();
            menu.inventory = GetComponent<Inventory>();
            menu.equipment = GetComponent<Equipment>();
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;
        // Move on the network tick so NetworkTransform captures it (moving in Update gets overwritten).
        if (hero != null && hero.enabled) hero.DoMove(Runner.DeltaTime);
        // Publish locomotion speed for proxies to play.
        if (animator != null) NetSpeed = animator.GetFloat(SpeedHash);
    }

    public override void Render()
    {
        // Proxies drive their Animator from the networked value (authority's HeroController already sets it).
        if (!HasStateAuthority && animator != null) animator.SetFloat(SpeedHash, NetSpeed);
    }

    // ---- one-shot action triggers (Cast/Attack/AoE) — SkillCaster calls this so remotes see you act ----
    public void FireAction(string trigger)
    {
        if (string.IsNullOrEmpty(trigger)) return;
        if (!HasStateAuthority) return;                              // only the owner self-animates; proxies get RPC_Action
        if (animator != null) animator.SetTrigger(trigger);          // local, immediate
        if (Object != null && Object.IsValid) RPC_Action(TriggerId(trigger));
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_Action(int id)
    {
        if (HasStateAuthority) return;                                // already played locally
        string t = TriggerName(id);
        if (animator != null && t != null) animator.SetTrigger(t);
    }

    static int TriggerId(string t) { switch (t) { case "Cast": return 1; case "Attack": return 2; case "AoE": return 3; default: return 0; } }
    static string TriggerName(int id) { switch (id) { case 1: return "Cast"; case 2: return "Attack"; case 3: return "AoE"; default: return null; } }
}
