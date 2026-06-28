using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] FactionData playerFaction;

    public float Gold { get; private set; }

    // Gold trickle starts at 2.3g/s and ramps — matches GDD table
    float goldRate = 2.3f;
    float matchTime;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        matchTime += Time.deltaTime;
        goldRate = Mathf.Lerp(2.3f, 6f, matchTime / 720f); // ramp over 12 minutes
        Gold += goldRate * Time.deltaTime;
    }

    public bool TrySpendGold(float amount)
    {
        if (Gold < amount) return false;
        Gold -= amount;
        return true;
    }
}
