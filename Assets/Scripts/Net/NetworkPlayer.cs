using Fusion;
using UnityEngine;

// Turns the real Wizard into a networked avatar. In Shared Mode each client spawns its own Wizard with
// state authority. The LOCAL (authority) instance runs the normal single-player components (input, movement,
// abilities, camera, HUD); REMOTE proxies disable those and just receive synced position (NetworkTransform)
// + locomotion animation (NetSpeed). Combat triggers/death sync come in a later pass.
public class NetworkPlayer : NetworkBehaviour
{
    [Networked] public float NetSpeed { get; set; }

    Animator animator;
    static readonly int SpeedHash = Animator.StringToHash("Speed");

    public override void Spawned()
    {
        animator = GetComponentInChildren<Animator>();
        bool local = HasStateAuthority;

        // Local-only control components — off on remote proxies (they're driven by the network instead).
        Toggle<HeroController>(local);
        Toggle<MeleeAttacker>(local);
        Toggle<SkillCaster>(local);
        Toggle<Miner>(local);
        Toggle<PlayerTargeting>(local);
        var cc = GetComponent<CharacterController>(); if (cc != null) cc.enabled = local;

        if (local) WireLocalPlayer();
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
        // Authority publishes its locomotion speed for proxies to play.
        if (HasStateAuthority && animator != null) NetSpeed = animator.GetFloat(SpeedHash);
    }

    public override void Render()
    {
        // Proxies drive their Animator from the networked value (authority's HeroController already sets it).
        if (!HasStateAuthority && animator != null) animator.SetFloat(SpeedHash, NetSpeed);
    }
}
