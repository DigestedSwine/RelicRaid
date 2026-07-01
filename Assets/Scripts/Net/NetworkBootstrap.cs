using Fusion;
using System.Threading.Tasks;
using UnityEngine;

// Starts/joins a Fusion Shared-Mode session (Photon Cloud relay) and spawns this client's player.
// Driven by the JoinScreen. Mobile-hardened: keeps the screen awake during play, and on device
// lock / app-backgrounding it recovers — if Fusion can't auto-heal the suspended socket, it rejoins
// the same session and respawns the local player (with a "Reconnecting…" overlay).
public class NetworkBootstrap : MonoBehaviour
{
    public NetworkObject playerPrefab;
    public string sessionName = "RelicRaid-Dev";
    public Vector3 spawnPoint = new Vector3(-67f, 2f, -55f);
    public bool autoConnect = false;     // true = skip the join screen (quick dev testing)

    [Header("Reconnect")]
    public float autoHealGrace = 1.2f;   // seconds to let Fusion recover a briefly-suspended socket before rejoining
    public int maxReconnectTries = 12;
    public float reconnectInterval = 2f;

    public bool Connecting { get; private set; }
    public bool Connected { get; private set; }
    public string PlayerName { get; private set; } = "Player";
    public NetworkRunner Runner { get; private set; }

    GameObject runnerGO;
    bool everConnected;
    bool recovering;
    bool showReconnectUI;

    void Awake()
    {
        // The single most important mobile fix: don't let the device sleep/lock during a session.
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Application.runInBackground = true;
    }

    void Start() { if (autoConnect) Connect(sessionName, PlayerName); }

    public async void Connect(string session, string playerName)
    {
        if (Connecting || Connected) return;
        if (!string.IsNullOrWhiteSpace(session)) sessionName = session.Trim();
        if (!string.IsNullOrWhiteSpace(playerName)) PlayerName = playerName.Trim();
        await StartSession();
    }

    // Spins up a fresh runner (on its own child GameObject so reconnects can tear it down cleanly),
    // joins the session, and spawns the local player. Reused for both first-join and reconnect.
    async Task StartSession()
    {
        Connecting = true;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        if (Runner != null) { try { await Runner.Shutdown(); } catch { } }
        if (runnerGO != null) Destroy(runnerGO);
        runnerGO = new GameObject("NetworkRunner");
        runnerGO.transform.SetParent(transform, false);
        Runner = runnerGO.AddComponent<NetworkRunner>();
        Runner.ProvideInput = false;

        var result = await Runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Shared,
            SessionName = sessionName,
            PlayerCount = 5,
        });

        Connecting = false;
        if (!result.Ok)
        {
            Connected = false;
            Debug.LogError($"[Net] Join failed: {result.ShutdownReason}");
            return;
        }

        Connected = true;
        everConnected = true;
        SpawnLocalPlayer();
        Debug.Log($"[Net] Connected to '{sessionName}' as '{PlayerName}'. LocalPlayer={Runner.LocalPlayer}.");
    }

    void SpawnLocalPlayer()
    {
        var pos = spawnPoint + new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-2f, 2f));
        Runner.Spawn(playerPrefab, pos, Quaternion.identity, Runner.LocalPlayer);
        // NetworkPlayer.Spawned() re-points the camera/HUD/menu at this fresh local player, so respawn re-wires itself.
    }

    // ---- Device lock / backgrounding recovery ----
    void OnApplicationPause(bool paused) { if (!paused) BeginRecover(); }
    void OnApplicationFocus(bool focused) { if (focused) BeginRecover(); }

    async void BeginRecover()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;   // iOS can reset this on resume
        if (!everConnected || recovering || Connecting) return;
        recovering = true;

        // Let Fusion attempt to recover a briefly-suspended socket before we hard-rejoin.
        await Task.Delay(Mathf.RoundToInt(autoHealGrace * 1000f));
        if (IsHealthy()) { recovering = false; return; }

        // Session was lost while locked — rejoin + respawn, retrying with backoff.
        showReconnectUI = true;
        Connected = false;
        for (int attempt = 1; attempt <= maxReconnectTries && !Connected; attempt++)
        {
            Debug.Log($"[Net] Reconnect attempt {attempt}/{maxReconnectTries}…");
            await StartSession();
            if (!Connected) await Task.Delay(Mathf.RoundToInt(reconnectInterval * 1000f));
        }
        showReconnectUI = false;
        recovering = false;
        if (!Connected) Debug.LogError("[Net] Reconnect gave up — rejoin from the menu.");
    }

    // We're "in the game" only if the runner is live AND our own player object still exists.
    bool IsHealthy()
    {
        if (Runner == null || !Runner.IsRunning) return false;
        foreach (var np in Object.FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None))
            if (np != null && np.Object != null && np.Object.IsValid && np.HasStateAuthority) return true;
        return false;
    }

    void OnGUI()
    {
        if (!showReconnectUI) return;
        float w = 340f, h = 96f;
        var rect = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
        GUI.color = new Color(0f, 0f, 0f, 0.82f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = Color.white;
        var style = new GUIStyle(GUI.skin.label)
        { alignment = TextAnchor.MiddleCenter, fontSize = 22, fontStyle = FontStyle.Bold };
        GUI.Label(rect, "Reconnecting…", style);
    }
}
