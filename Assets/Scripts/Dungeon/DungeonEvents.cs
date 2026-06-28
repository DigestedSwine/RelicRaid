// Global dungeon signals, mirroring CombatEvents / WorldEvents so BossController doesn't depend on
// DungeonManager. DungeonManager subscribes; later the Fusion layer can raise/listen server-side.
public static class DungeonEvents
{
    public static event System.Action<BossController> BossDefeated;
    public static void RaiseBossDefeated(BossController boss) => BossDefeated?.Invoke(boss);
}
