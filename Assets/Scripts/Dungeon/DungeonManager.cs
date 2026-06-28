using System.Collections;
using UnityEngine;

// The dungeon "instance core": owns the run lifecycle (Active → BossDefeated → Closed). Runs locally now;
// the hooks (OnRunStarted/OnBossDefeated/OnDungeonClosed) are where Photon Fusion (room lifecycle) and
// Nakama (persist results) plug in later. This same core is reused by Mode 2's PVP match loop.
public class DungeonManager : MonoBehaviour
{
    public enum Phase { Active, BossDefeated, Closed }

    [Header("Setup")]
    public Transform entryPoint;                 // where players spawn / are placed
    public float autoCloseMinutesAfterBoss = 30f;

    public Phase CurrentPhase { get; private set; } = Phase.Active;

    public System.Action OnRunStarted;
    public System.Action<BossController> OnBossDefeated;
    public System.Action OnDungeonClosed;

    Coroutine autoClose;

    void OnEnable() { DungeonEvents.BossDefeated += HandleBossDefeated; }
    void OnDisable() { DungeonEvents.BossDefeated -= HandleBossDefeated; }

    void Start() { OnRunStarted?.Invoke(); }

    void HandleBossDefeated(BossController boss)
    {
        if (CurrentPhase != Phase.Active) return;
        CurrentPhase = Phase.BossDefeated;
        OnBossDefeated?.Invoke(boss);
        autoClose = StartCoroutine(AutoCloseTimer());
    }

    IEnumerator AutoCloseTimer()
    {
        yield return new WaitForSeconds(autoCloseMinutesAfterBoss * 60f);
        Close();
    }

    // Called when all players have exited (via portals) or the auto-close timer fires.
    public void Close()
    {
        if (CurrentPhase == Phase.Closed) return;
        if (autoClose != null) StopCoroutine(autoClose);
        CurrentPhase = Phase.Closed;
        OnDungeonClosed?.Invoke();
        // Local stub: later this returns players to the lobby (Fusion) and persists results (Nakama).
        Debug.Log("[DungeonManager] Dungeon closed.");
    }
}
