using UnityEngine;
using UnityEngine.UIElements;

// Wireframe login/join screen (UI Toolkit, touch-friendly). Enter a name + session and tap JOIN to start/join
// the Fusion session; hides itself once connected. Minimal placeholder for device co-op playtests.
[RequireComponent(typeof(UIDocument))]
public class JoinScreen : MonoBehaviour
{
    public NetworkBootstrap net;

    VisualElement root;
    TextField nameField, sessionField;
    Label status;
    bool joining;

    void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        if (root == null) return;
        root.Clear();
        Build(root);
    }

    void Build(VisualElement r)
    {
        r.style.position = Position.Absolute;
        r.style.left = 0; r.style.right = 0; r.style.top = 0; r.style.bottom = 0;
        r.style.backgroundColor = new Color(0.05f, 0.06f, 0.09f, 1f);
        r.style.alignItems = Align.Center;
        r.style.justifyContent = Justify.Center;

        var panel = new VisualElement();
        panel.style.width = 360; panel.style.maxWidth = Length.Percent(90f);
        panel.style.paddingLeft = 24; panel.style.paddingRight = 24;
        panel.style.paddingTop = 22; panel.style.paddingBottom = 24;
        panel.style.backgroundColor = new Color(0.10f, 0.12f, 0.16f, 1f);
        Round(panel, 14);
        r.Add(panel);

        var title = Lbl("RELIC RAID", 26, new Color(0.9f, 0.95f, 1f), FontStyle.Bold);
        title.style.unityTextAlign = TextAnchor.MiddleCenter; title.style.marginBottom = 4;
        panel.Add(title);
        var sub = Lbl("co-op playtest", 12, new Color(0.5f, 0.6f, 0.7f));
        sub.style.unityTextAlign = TextAnchor.MiddleCenter; sub.style.marginBottom = 18;
        panel.Add(sub);

        nameField = Field(panel, "Name", "Player" + Random.Range(10, 99));
        sessionField = Field(panel, "Session", net != null ? net.sessionName : "RelicRaid-Dev");

        var join = new Button(OnJoin) { text = "JOIN" };
        join.style.height = 46; join.style.marginTop = 18; join.style.fontSize = 18;
        join.style.color = Color.white;
        join.style.backgroundColor = new Color(0.20f, 0.50f, 0.85f);
        Round(join, 8);
        panel.Add(join);

        status = Lbl("", 12, new Color(0.7f, 0.8f, 0.95f));
        status.style.unityTextAlign = TextAnchor.MiddleCenter; status.style.marginTop = 12;
        panel.Add(status);
    }

    TextField Field(VisualElement parent, string label, string val)
    {
        var l = Lbl(label.ToUpper(), 11, new Color(0.55f, 0.62f, 0.72f));
        l.style.marginTop = 10; l.style.marginBottom = 3;
        parent.Add(l);
        var f = new TextField { value = val };
        f.style.height = 38; f.style.fontSize = 15;
        parent.Add(f);
        return f;
    }

    void OnJoin()
    {
        if (joining || net == null) return;
        joining = true;
        status.text = "Connecting…";
        net.Connect(sessionField.value, nameField.value);
    }

    void Update()
    {
        if (net == null) return;
        // Hide whenever connected — covers both the JOIN button and auto-connect.
        if (net.Connected) { if (root != null) root.style.display = DisplayStyle.None; joining = false; return; }
        if (joining && !net.Connecting) { status.text = "Connection failed — tap JOIN to retry"; joining = false; }
    }

    static Label Lbl(string t, float s, Color c, FontStyle fs = FontStyle.Normal)
    {
        var l = new Label(t); l.style.color = c; l.style.fontSize = s; l.style.unityFontStyleAndWeight = fs;
        return l;
    }
    static void Round(VisualElement e, float r)
    {
        e.style.borderTopLeftRadius = r; e.style.borderTopRightRadius = r;
        e.style.borderBottomLeftRadius = r; e.style.borderBottomRightRadius = r;
    }
}
