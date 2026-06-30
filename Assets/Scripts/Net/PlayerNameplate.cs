using UnityEngine;

// Billboard nameplate over OTHER players (your own is hidden). Shows NetworkPlayer.PlayerName.
public class PlayerNameplate : MonoBehaviour
{
    public float headMargin = 0.4f;
    public Color color = new Color(0.75f, 0.88f, 1f);

    NetworkPlayer net;
    TextMesh tm;
    Transform holder;
    Camera cam;
    Renderer[] rends;

    void Start()
    {
        net = GetComponent<NetworkPlayer>();
        cam = Camera.main;
        rends = GetComponentsInChildren<Renderer>();

        var go = new GameObject("Nameplate");
        holder = go.transform;
        tm = go.AddComponent<TextMesh>();
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font != null) { tm.font = font; go.GetComponent<MeshRenderer>().sharedMaterial = font.material; }
        tm.fontSize = 64; tm.characterSize = 0.045f; tm.fontStyle = FontStyle.Bold;
        tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center;
        tm.color = color;
    }

    void LateUpdate()
    {
        if (net == null || tm == null) return;
        if (cam == null) cam = Camera.main;

        bool show = !net.HasStateAuthority && net.Object != null && net.Object.IsValid;   // others only
        holder.gameObject.SetActive(show);
        if (!show) return;

        tm.text = net.PlayerName.ToString();

        Vector3 pos = transform.position + Vector3.up * 2f;
        if (rends != null && rends.Length > 0)
        {
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            pos = new Vector3(b.center.x, b.max.y + headMargin, b.center.z);
        }
        holder.position = pos;
        if (cam != null) holder.forward = cam.transform.forward;   // billboard
    }

    void OnDestroy() { if (holder != null) Destroy(holder.gameObject); }
}
