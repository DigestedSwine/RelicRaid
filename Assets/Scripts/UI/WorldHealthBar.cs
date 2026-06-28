using UnityEngine;

// World-space billboard health bar for NPCs. Built from two unlit quads (no Canvas / UI Toolkit) so it's
// cheap with many enemies. Lives as an independent world-scale object that tracks the entity each frame,
// so it is NOT affected by the entity's transform scale (e.g. the bear's 0.024).
public class WorldHealthBar : MonoBehaviour
{
    public HealthComponent health;
    public Vector2 size = new Vector2(1.3f, 0.18f);
    public float margin = 0.35f;                 // gap above the entity's head
    public Color fillColor = new Color(0.85f, 0.2f, 0.15f);
    public Color backColor = new Color(0.04f, 0.04f, 0.04f, 1f);
    public bool hideWhenFull = true;

    Transform holder, fillAnchor;
    Camera cam;
    Renderer[] entityRenderers;

    void Awake() { if (health == null) health = GetComponent<HealthComponent>(); }

    void Start()
    {
        cam = Camera.main;
        if (health == null) { enabled = false; return; }
        entityRenderers = health.GetComponentsInChildren<Renderer>();
        Build();
        health.OnDamaged += (a, t) => Refresh();
        health.OnHealed += a => Refresh();
        health.OnDeath += HandleDeath;
        health.OnRevived += HandleRevive;
        Refresh();
    }

    Material MakeMat(Color c)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f);   // double-sided — visible from any camera angle
        m.renderQueue = 4000;                    // draw after opaque geometry
        return m;
    }

    GameObject Quad(string n, Transform parent, Color c)
    {
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = n;
        var col = q.GetComponent<Collider>(); if (col) Destroy(col);
        q.transform.SetParent(parent, false);
        q.GetComponent<Renderer>().sharedMaterial = MakeMat(c);
        return q;
    }

    void Build()
    {
        holder = new GameObject("HealthBar_" + health.name).transform;

        var bg = Quad("BG", holder, backColor);
        bg.transform.localPosition = new Vector3(0f, 0f, 0.002f);
        bg.transform.localScale = new Vector3(size.x, size.y, 1f);

        fillAnchor = new GameObject("FillAnchor").transform;   // pivots from the LEFT edge
        fillAnchor.SetParent(holder, false);
        fillAnchor.localPosition = new Vector3(-size.x * 0.5f, 0f, 0f);

        var fill = Quad("Fill", fillAnchor, fillColor);
        fill.transform.localPosition = new Vector3(size.x * 0.5f, 0f, 0f);
        fill.transform.localScale = new Vector3(size.x, size.y * 0.78f, 1f);
    }

    void Refresh()
    {
        if (fillAnchor == null) return;
        float f = Mathf.Clamp01(health.HPFraction);
        fillAnchor.localScale = new Vector3(f, 1f, 1f);
        bool show = health.IsAlive && !(hideWhenFull && f >= 0.999f);
        if (holder) holder.gameObject.SetActive(show);
    }

    void HandleDeath() { if (holder) holder.gameObject.SetActive(false); }   // hide, don't destroy (bear respawns)
    void HandleRevive() { if (holder) { holder.gameObject.SetActive(true); Refresh(); } }

    void LateUpdate()
    {
        if (holder == null || health == null) return;
        Refresh();                          // track passive HP regen (which fires no event)
        if (cam == null) cam = Camera.main;

        // Place centered above the entity's visual bounds (animation-safe).
        Vector3 pos = health.transform.position + Vector3.up * 2f;
        if (entityRenderers != null && entityRenderers.Length > 0)
        {
            Bounds b = entityRenderers[0].bounds;
            for (int i = 1; i < entityRenderers.Length; i++) b.Encapsulate(entityRenderers[i].bounds);
            pos = new Vector3(b.center.x, b.max.y + margin, b.center.z);
        }
        holder.position = pos;
        if (cam != null) holder.rotation = cam.transform.rotation;   // billboard
    }
}
