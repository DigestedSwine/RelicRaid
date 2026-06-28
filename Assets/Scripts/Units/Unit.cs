using UnityEngine;

public class Unit : MonoBehaviour
{
    public UnitData Data { get; private set; }
    public UnitGroup Group { get; set; }

    float currentHP;

    void Awake() => UnitRegistry.Register(this);

    void OnDestroy()
    {
        UnitRegistry.Unregister(this);
        Group?.RemoveUnit(this);
    }

    public void Initialize(UnitData data)
    {
        Data = data;
        currentHP = data.maxHP;
    }

    public void TakeDamage(float amount)
    {
        currentHP -= amount;
        if (currentHP <= 0f)
            Destroy(gameObject);
    }

    // Called by UnitGroup — steering/NavMesh integration goes here later
    public void MoveTo(Vector3 destination)
    {
        // stub: direct translate for prototype, replace with steering agent
        StopAllCoroutines();
        StartCoroutine(MoveRoutine(destination));
    }

    System.Collections.IEnumerator MoveRoutine(Vector3 destination)
    {
        while (Vector3.Distance(transform.position, destination) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position, destination, Data.moveSpeed * Time.deltaTime);
            yield return null;
        }
    }
}
