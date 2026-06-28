using UnityEngine;

// Category of a killable thing — drives how kills are tallied and (later) achievements / first-kills.
public enum EnemyKind { Normal, Elite, Unique, Boss, Player }

// Identity + reward metadata for anything that can be killed (NPCs now, enemy players later). Read on
// death to credit the kill + XP to the killer. Drop on any enemy prefab.
public class EnemyIdentity : MonoBehaviour
{
    public string displayName = "Enemy";   // tallied by name (e.g. "Bear", boss names, unique names)
    public EnemyKind kind = EnemyKind.Normal;
    public int xpReward = 25;
}
