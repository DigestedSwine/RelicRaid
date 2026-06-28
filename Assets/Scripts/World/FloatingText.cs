using UnityEngine;

// Tiny world-space popup (e.g. "+3 ◆") that rises, billboards to the camera, fades, and self-destroys.
public class FloatingText : MonoBehaviour
{
    public float duration = 1.2f;
    public float rise = 1.4f;

    TextMesh tm;
    Color baseColor;
    float t;

    public static void Spawn(Vector3 pos, string text, Color color)
    {
        var go = new GameObject("FloatingText");
        go.transform.position = pos;

        var tm = go.AddComponent<TextMesh>();
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font != null) { tm.font = font; go.GetComponent<MeshRenderer>().sharedMaterial = font.material; }
        tm.text = text;
        tm.color = color;
        tm.fontSize = 90;
        tm.characterSize = 0.06f;
        tm.fontStyle = FontStyle.Bold;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;

        var ft = go.AddComponent<FloatingText>();
        ft.tm = tm; ft.baseColor = color;
    }

    void Update()
    {
        t += Time.deltaTime;
        float f = t / duration;
        if (f >= 1f) { Destroy(gameObject); return; }
        transform.position += Vector3.up * rise * Time.deltaTime;
        if (Camera.main != null) transform.forward = Camera.main.transform.forward;   // billboard
        var c = baseColor; c.a = 1f - f;
        if (tm != null) tm.color = c;
    }
}
