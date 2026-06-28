using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

// Front-end flow (UI Toolkit, code-built): Splash → Main Menu → Join → Level Select → load dungeon.
// Screens are panels swapped by one state machine (no scene loads between menu steps). The Join screen is
// a Fusion-ready stub (solo for now). Robust if MainMenu is played directly (no Boot): falls back to
// SceneManager when the persistent SceneLoader/GameSession aren't present.
[RequireComponent(typeof(UIDocument))]
public class MenuFlow : MonoBehaviour
{
    public DungeonInfo[] dungeons;
    public float splashSeconds = 2.5f;
    public string fallbackDungeonScene = "SampleScene";

    VisualElement root, splash, mainMenu, join, levelSelect;

    void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        if (root == null) return;
        root.Clear();
        splash = BuildSplash();
        mainMenu = BuildMainMenu();
        join = BuildJoin();
        levelSelect = BuildLevelSelect();
        ShowOnly(splash);
        StartCoroutine(SplashRoutine());
    }

    IEnumerator SplashRoutine()
    {
        float t = 0f;
        while (t < splashSeconds) { t += Time.unscaledDeltaTime; yield return null; }
        if (IsShown(splash)) ShowOnly(mainMenu);
    }

    bool IsShown(VisualElement e) => e.style.display == DisplayStyle.Flex;
    void ShowOnly(VisualElement panel)
    {
        foreach (var p in new[] { splash, mainMenu, join, levelSelect })
            p.style.display = (p == panel) ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // ---------------- screens ----------------
    VisualElement BuildSplash()
    {
        var p = Panel(new Color(0.03f, 0.04f, 0.07f, 1f));
        p.Add(Title("RELIC RAID", 54));
        var sub = Text("Three factions. One Wellspring.", 16, new Color(0.7f, 0.8f, 0.95f)); sub.style.marginTop = 6;
        p.Add(sub);
        var tap = Text("tap to continue", 13, new Color(1, 1, 1, 0.5f)); tap.style.marginTop = 44;
        p.Add(tap);
        p.RegisterCallback<ClickEvent>(_ => { if (IsShown(splash)) ShowOnly(mainMenu); });
        return p;
    }

    VisualElement BuildMainMenu()
    {
        var p = Panel(new Color(0.05f, 0.06f, 0.09f, 0.98f));
        p.Add(Title("RELIC RAID", 40));
        var col = Column(); p.Add(col);
        col.Add(Btn("Play", () => ShowOnly(join)));
        col.Add(Btn("Settings", () => Toast("Settings — coming soon")));
        col.Add(Btn("Quit", () => Application.Quit()));
        return p;
    }

    VisualElement BuildJoin()
    {
        var p = Panel(new Color(0.05f, 0.06f, 0.09f, 0.98f));
        p.Add(Title("Find a Group", 30));
        var note = Text("Online parties arrive with multiplayer. For now, dive in solo.", 14, new Color(0.7f, 0.75f, 0.85f));
        note.style.maxWidth = 360; note.style.whiteSpace = WhiteSpace.Normal;
        note.style.unityTextAlign = TextAnchor.MiddleCenter; note.style.marginTop = 8; note.style.marginBottom = 6;
        p.Add(note);
        var col = Column(); p.Add(col);
        col.Add(Btn("Play Solo", () => ShowOnly(levelSelect)));
        col.Add(Btn("Find Group (soon)", () => Toast("Matchmaking needs multiplayer — later")));
        col.Add(Btn("Back", () => ShowOnly(mainMenu)));
        return p;
    }

    VisualElement BuildLevelSelect()
    {
        var p = Panel(new Color(0.05f, 0.06f, 0.09f, 0.98f));
        p.Add(Title("Select a Dungeon", 30));
        var col = Column(); p.Add(col);
        if (dungeons != null && dungeons.Length > 0)
            foreach (var d in dungeons) { var dd = d; col.Add(DungeonCard(dd)); }
        else
            col.Add(Btn("Enter Dungeon", () => EnterDungeon(null)));
        col.Add(Btn("Back", () => ShowOnly(join)));
        return p;
    }

    VisualElement DungeonCard(DungeonInfo d)
    {
        var card = new Button(() => EnterDungeon(d));
        StyleButton(card); card.style.height = 66; card.style.width = 330;
        card.style.flexDirection = FlexDirection.Column; card.style.alignItems = Align.FlexStart;
        card.style.paddingLeft = 14; card.style.paddingTop = 8;
        var name = Text(d.displayName, 18, Color.white); name.style.unityFontStyleAndWeight = FontStyle.Bold;
        var meta = Text("Lv " + d.recommendedLevel + "  ·  " + d.description, 11, new Color(0.72f, 0.77f, 0.88f));
        meta.style.whiteSpace = WhiteSpace.Normal; meta.style.maxWidth = 300;
        card.Add(name); card.Add(meta);
        return card;
    }

    void EnterDungeon(DungeonInfo d)
    {
        string scene = (d != null && !string.IsNullOrEmpty(d.sceneName)) ? d.sceneName : fallbackDungeonScene;
        if (GameSession.Instance != null) GameSession.Instance.SelectedDungeon = d;
        if (SceneLoader.Instance != null) SceneLoader.Instance.Load(scene);
        else SceneManager.LoadScene(scene);
    }

    // ---------------- helpers (flat, consistent with the HUD) ----------------
    VisualElement Panel(Color bg)
    {
        var p = new VisualElement();
        p.style.position = Position.Absolute; p.style.left = 0; p.style.right = 0; p.style.top = 0; p.style.bottom = 0;
        p.style.alignItems = Align.Center; p.style.justifyContent = Justify.Center;
        p.style.backgroundColor = bg;
        root.Add(p);
        return p;
    }

    VisualElement Column() { var c = new VisualElement(); c.style.alignItems = Align.Center; c.style.marginTop = 24; return c; }

    Label Title(string t, int size)
    {
        var l = new Label(t);
        l.style.fontSize = size; l.style.color = Color.white; l.style.unityFontStyleAndWeight = FontStyle.Bold;
        return l;
    }

    Label Text(string t, int size, Color c)
    {
        var l = new Label(t); l.style.fontSize = size; l.style.color = c; return l;
    }

    Button Btn(string text, System.Action onClick) { var b = new Button(onClick) { text = text }; StyleButton(b); return b; }

    void StyleButton(Button b)
    {
        b.style.width = 240; b.style.height = 48; b.style.marginTop = 8;
        b.style.fontSize = 18; b.style.color = Color.white; b.style.unityFontStyleAndWeight = FontStyle.Bold;
        b.style.backgroundColor = new Color(0.14f, 0.16f, 0.22f, 1f);
        b.style.unityTextAlign = TextAnchor.MiddleCenter;
        Round(b, 8); BorderC(b, new Color(0.4f, 0.55f, 0.85f, 0.7f));
    }

    void Toast(string msg) { Debug.Log("[Menu] " + msg); }   // later: on-screen toast

    void Round(VisualElement e, float r)
    { e.style.borderTopLeftRadius = r; e.style.borderTopRightRadius = r; e.style.borderBottomLeftRadius = r; e.style.borderBottomRightRadius = r; }
    void BorderC(VisualElement e, Color c)
    {
        e.style.borderTopColor = c; e.style.borderBottomColor = c; e.style.borderLeftColor = c; e.style.borderRightColor = c;
        e.style.borderTopWidth = 1; e.style.borderBottomWidth = 1; e.style.borderLeftWidth = 1; e.style.borderRightWidth = 1;
    }
}
