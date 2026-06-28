using System.Collections;
using UnityEngine;

// Enemy AI: idle → chase → attack → leash → dead. Transform-based movement (no NavMesh yet) with terrain
// grounding. Drives the Generic locomotion blend tree via a Speed float and deals damage through CombatManager.
// All tunables are serialized — nothing hardcoded (per GDD).
[RequireComponent(typeof(HealthComponent))]
public class NPCController : MonoBehaviour
{
    public enum State { Idle, Chase, Attack, Leash, Dead }

    [Header("Targeting")]
    public Team hostileTo = Team.Players;
    public float detectionRadius = 12f;
    public float attackRange = 3f;
    public float leashRadius = 24f;

    [Header("Movement")]
    public float moveSpeed = 3.5f;
    public float turnSpeed = 360f;
    public bool stickToTerrain = true;
    public float groundOffset = 0f;        // fallback pivot→feet offset (used only if no foot bones found)
    public float footPad = 0.05f;          // lift so the paw mesh (below the foot bone) rests on the ground
    public LayerMask groundMask = ~0;      // floors to ground onto (dungeon geometry + terrain); narrow to a Ground layer to avoid hitting characters
    public float faceYawOffset = 0f;       // set 180 if the model walks backwards

    [Header("Roaming (idle wander)")]
    public RoamZone roamZone;              // assign a zone → wander inside it when not chasing (null = stand still)
    public float roamSpeed = 1.8f;         // slow wander → walk animation
    public float roamPauseMin = 2f;
    public float roamPauseMax = 6f;

    [Header("Attack")]
    public float attackCooldown = 1.6f;
    public float attackWindup = 0.35f;
    public float attackMultiplier = 1f;
    public DamageType damageType = DamageType.Physical;
    public string attackTrigger = "";      // animator trigger to play an attack clip (empty = none; bear uses AttackGlow instead)

    [Header("Animator")]
    public string speedParam = "Speed";

    public State Current => state;

    // Fired when a swing begins (passes the wind-up seconds) and when the strike lands.
    // Lets visuals (e.g. a glowing maw) telegraph the attack without a dedicated animation.
    public event System.Action<float> AttackWindup;
    public event System.Action AttackStrike;

    HealthComponent health;
    Animator animator;
    NetworkEnemy netEnemy;     // if networked, attack triggers replicate through it
    Transform target;
    HealthComponent targetHealth;
    Vector3 spawnPos;
    State state = State.Idle;
    float nextAttack, nextScan;
    bool swinging;
    Terrain terrain;
    int speedHash;
    Transform[] footBones;
    Vector3 roamTarget; bool hasRoamTarget; float roamPauseUntil;
    float stunnedUntil;
    float tickDt = 0f;
    [HideInInspector] public bool networkDriven = false;   // set by NetworkEnemy: AI runs from the net tick, not Update

    // Crowd control: freezes movement + attacks until the timer expires (extends, never shortens).
    public void ApplyStun(float duration) { stunnedUntil = Mathf.Max(stunnedUntil, Time.time + duration); }
    public bool IsStunned => Time.time < stunnedUntil;

    void Awake()
    {
        health = GetComponent<HealthComponent>();
        animator = GetComponent<Animator>();
        netEnemy = GetComponent<NetworkEnemy>();
        speedHash = Animator.StringToHash(speedParam);
    }

    void Start()
    {
        spawnPos = transform.position;
        terrain = Terrain.activeTerrain;
        CacheFootBones();
        health.OnDeath += () => { state = State.Dead; SetSpeed(0f); };
        health.OnRevived += () => { state = State.Idle; target = null; targetHealth = null; hasRoamTarget = false; stunnedUntil = 0f; };
    }

    void Update()
    {
        if (networkDriven) return;            // networked: NetworkEnemy drives Tick() on the net tick instead
        tickDt = Time.deltaTime;
        RunAI();
    }

    // Ground in LateUpdate, AFTER the Animator has posed the skeleton this frame, so foot positions are current.
    void LateUpdate()
    {
        if (!networkDriven && stickToTerrain) StickToGround();
    }

    // Movement + grounding step. Update() calls it for single-player; NetworkEnemy.FixedUpdateNetwork calls it
    // on the authority so NetworkTransform captures the movement (moving in Update gets overwritten).
    public void Tick(float dt)
    {
        tickDt = dt;
        RunAI();
        if (stickToTerrain) StickToGround();
    }

    void RunAI()
    {
        if (state == State.Dead || !health.IsAlive) { SetSpeed(0f); return; }
        if (IsStunned) { SetSpeed(0f); return; }   // frozen by crowd control

        if (Time.time >= nextScan) { Scan(); nextScan = Time.time + 0.25f; }

        switch (state)
        {
            case State.Idle:
                if (target != null) state = State.Chase;
                else if (roamZone != null) { TickRoam(); break; }
                SetSpeed(0f); break;
            case State.Chase:  TickChase();  break;
            case State.Attack: TickAttack(); break;
            case State.Leash:  TickLeash();  break;
        }
    }

    void CacheFootBones()
    {
        var smr = GetComponentInChildren<SkinnedMeshRenderer>();
        var list = new System.Collections.Generic.List<Transform>();
        if (smr != null)
            foreach (var b in smr.bones)
            {
                if (b == null) continue;
                string n = b.name.ToLower();
                if (n.Contains("leg") || n.Contains("foot") || n.Contains("paw") || n.Contains("toe") || n.Contains("ankle") || n.Contains("calf"))
                    list.Add(b);
            }
        footBones = list.ToArray();
    }

    void Scan()
    {
        if (state == State.Leash) return;  // not aggroable while returning home
        float best = detectionRadius * detectionRadius;
        target = null; targetHealth = null;
        foreach (var hc in UnityEngine.Object.FindObjectsByType<HealthComponent>(FindObjectsSortMode.None))
        {
            if (hc == null || hc == health || !hc.IsAlive || hc.team != hostileTo) continue;
            float d = (hc.transform.position - transform.position).sqrMagnitude;
            if (d < best) { best = d; target = hc.transform; targetHealth = hc; }
        }
    }

    void TickChase()
    {
        if (!HasLiveTarget()) { state = State.Idle; return; }
        if (LeashedOut()) { state = State.Leash; return; }
        if (PlanarDist(target.position) <= attackRange) { state = State.Attack; SetSpeed(0f); return; }
        MoveToward(target.position, moveSpeed);
    }

    void TickRoam()
    {
        if (Time.time < roamPauseUntil) { SetSpeed(0f); return; }
        if (!hasRoamTarget) { roamTarget = roamZone.RandomPoint(); hasRoamTarget = true; }
        if (PlanarDist(roamTarget) <= 0.6f)
        {
            hasRoamTarget = false;
            roamPauseUntil = Time.time + Random.Range(roamPauseMin, roamPauseMax);
            SetSpeed(0f);
            return;
        }
        MoveToward(roamTarget, roamSpeed);
    }

    void TickAttack()
    {
        if (!HasLiveTarget()) { state = State.Idle; return; }
        if (LeashedOut()) { state = State.Leash; return; }
        FaceToward(target.position);
        SetSpeed(0f);
        if (PlanarDist(target.position) > attackRange * 1.25f) { state = State.Chase; return; }
        if (!swinging && Time.time >= nextAttack) StartCoroutine(Swing());
    }

    IEnumerator Swing()
    {
        swinging = true;
        nextAttack = Time.time + attackCooldown;
        AttackWindup?.Invoke(attackWindup);
        if (netEnemy != null) netEnemy.FireAction(attackTrigger);    // replicate the swing to proxies
        else if (animator != null && !string.IsNullOrEmpty(attackTrigger)) animator.SetTrigger(attackTrigger);
        yield return new WaitForSeconds(attackWindup);
        if (health.IsAlive && targetHealth != null && targetHealth.IsAlive &&
            PlanarDist(target.position) <= attackRange * 1.3f)
        {
            CombatManager.ProcessAttack(health, targetHealth, attackMultiplier, damageType);
            AttackStrike?.Invoke();
        }
        swinging = false;
    }

    void TickLeash()
    {
        if (PlanarDist(spawnPos) <= 0.5f) { state = State.Idle; return; }
        MoveToward(spawnPos, moveSpeed);
    }

    bool HasLiveTarget() => target != null && targetHealth != null && targetHealth.IsAlive;
    bool LeashedOut() => (transform.position - spawnPos).sqrMagnitude > leashRadius * leashRadius;
    float PlanarDist(Vector3 p) { Vector3 d = p - transform.position; d.y = 0f; return d.magnitude; }

    void MoveToward(Vector3 dest, float speed)
    {
        Vector3 dir = dest - transform.position; dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) { SetSpeed(0f); return; }
        dir.Normalize();
        transform.position += dir * speed * tickDt;
        FaceDir(dir);
        SetSpeed(speed);
    }

    void FaceToward(Vector3 p) { Vector3 d = p - transform.position; d.y = 0f; if (d.sqrMagnitude > 0.0001f) FaceDir(d.normalized); }

    void FaceDir(Vector3 dir)
    {
        Quaternion look = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, faceYawOffset, 0f);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnSpeed * tickDt);
    }

    void StickToGround()
    {
        if (!TryGetGroundY(out float surface)) return;

        // Per-frame foot grounding: drop/raise the whole rig so the LOWEST foot bone (a planted paw)
        // rests on the terrain. This tracks the gait, so the bear never floats during the run's bob.
        if (footBones != null && footBones.Length > 0)
        {
            float lowest = float.MaxValue;
            for (int i = 0; i < footBones.Length; i++)
                if (footBones[i] != null && footBones[i].position.y < lowest) lowest = footBones[i].position.y;

            if (lowest < float.MaxValue)
            {
                float delta = (surface + footPad) - lowest;
                transform.position += Vector3.up * delta;
                return;
            }
        }

        // Fallback: fixed pivot offset.
        Vector3 p = transform.position;
        p.y = surface + groundOffset;
        transform.position = p;
    }

    // Surface height under the NPC: raycast onto floor geometry first (works in dungeons), else terrain.
    bool TryGetGroundY(out float y)
    {
        if (Physics.Raycast(transform.position + Vector3.up * 2f, Vector3.down, out var hit, 8f, groundMask, QueryTriggerInteraction.Ignore))
        { y = hit.point.y; return true; }
        if (terrain == null) terrain = Terrain.activeTerrain;
        if (terrain != null) { y = terrain.SampleHeight(transform.position) + terrain.transform.position.y; return true; }
        y = 0f; return false;
    }

    void SetSpeed(float v) { if (animator != null) animator.SetFloat(speedHash, v); }
}
