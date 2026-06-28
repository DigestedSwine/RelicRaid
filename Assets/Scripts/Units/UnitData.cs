using UnityEngine;

[CreateAssetMenu(fileName = "UnitData", menuName = "RelicRaid/Unit Data")]
public class UnitData : ScriptableObject
{
    public string unitName;
    public int goldCost;
    public float trainTime;
    public int trainCap;

    public float maxHP;
    public float moveSpeed;
    public float attackDamage;
    public float attackRange;
    public float attackRate;

    public GameObject prefab;
}
