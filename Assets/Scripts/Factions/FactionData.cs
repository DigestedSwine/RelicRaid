using UnityEngine;

[CreateAssetMenu(fileName = "FactionData", menuName = "RelicRaid/Faction Data")]
public class FactionData : ScriptableObject
{
    public string factionName;
    public Color primaryColor;
    public UnitData[] units;
}
