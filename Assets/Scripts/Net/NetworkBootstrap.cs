using Fusion;
using UnityEngine;

// Starts/joins a Fusion Shared-Mode session (Photon Cloud relay) on demand and spawns this client's player.
// Driven by the JoinScreen (name + session) rather than auto-starting, so devices get a real join flow.
public class NetworkBootstrap : MonoBehaviour
{
    public NetworkObject playerPrefab;
    public string sessionName = "RelicRaid-Dev";
    public Vector3 spawnPoint = new Vector3(-67f, 2f, -55f);
    public bool autoConnect = false;     // true = skip the join screen (quick dev testing)

    public bool Connecting { get; private set; }
    public bool Connected { get; private set; }
    public string PlayerName { get; private set; } = "Player";
    public NetworkRunner Runner { get; private set; }

    void Start() { if (autoConnect) Connect(sessionName, PlayerName); }

    public async void Connect(string session, string playerName)
    {
        if (Connecting || Connected) return;
        Connecting = true;
        if (!string.IsNullOrWhiteSpace(session)) sessionName = session.Trim();
        if (!string.IsNullOrWhiteSpace(playerName)) PlayerName = playerName.Trim();

        Runner = GetComponent<NetworkRunner>();
        if (Runner == null) Runner = gameObject.AddComponent<NetworkRunner>();
        Runner.ProvideInput = false;

        var result = await Runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Shared,
            SessionName = sessionName,
            PlayerCount = 5,
        });

        Connecting = false;
        if (!result.Ok) { Debug.LogError($"[Net] Join failed: {result.ShutdownReason}"); return; }

        Connected = true;
        var pos = spawnPoint + new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-2f, 2f));
        Runner.Spawn(playerPrefab, pos, Quaternion.identity, Runner.LocalPlayer);
        Debug.Log($"[Net] Connected to '{sessionName}' as '{PlayerName}'. LocalPlayer={Runner.LocalPlayer}.");
    }
}
