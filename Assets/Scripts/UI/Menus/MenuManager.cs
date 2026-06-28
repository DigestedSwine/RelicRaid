using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

// In-game menu/screen-stack manager (UI Toolkit). Owns a dim backdrop + a centered panel with a title
// bar and close button; screens build their content into the host. Open/close via keys (C / I / Tab,
// Esc to back out) or MenuManager.Open(...) from a HUD button (mobile). Non-pausing overlay (co-op).
[RequireComponent(typeof(UIDocument))]
public class MenuManager : MonoBehaviour
{
    [Header("Player references (for the screens to read)")]
    public HealthComponent player;
    public PlayerProgress progress;
    public Inventory inventory;
    public Equipment equipment;

    VisualElement root, backdrop, panel, host;
    Label titleLabel;
    readonly Stack<UIScreen> stack = new Stack<UIScreen>();

    // Lazily-built singleton screens.
    CharacterScreen character;

    public bool IsOpen => stack.Count > 0;

    void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        if (root == null) return;
        root.Clear();
        BuildChrome();
        SetVisible(false);
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.cKey.wasPressedThisFrame || kb.iKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame)
                Toggle(Character());
            if (kb.escapeKey.wasPressedThisFrame && IsOpen) Close();
        }
        if (IsOpen) stack.Peek().Refresh();
    }

    // ---- screen factory (one instance each, reused) ----
    CharacterScreen Character() => character ??= new CharacterScreen(this);

    // ---- stack control ----
    public void Toggle(UIScreen screen)
    {
        if (IsOpen && stack.Peek() == screen) { Close(); return; }
        Open(screen);
    }

    public void Open(UIScreen screen)
    {
        if (IsOpen && stack.Peek() == screen) return;
        stack.Push(screen);
        host.Clear();
        screen.Build(host);
        titleLabel.text = screen.Title;
        screen.OnShow();
        SetVisible(true);
    }

    public void Close()
    {
        if (!IsOpen) return;
        stack.Pop().OnHide();
        if (IsOpen)
        {
            var top = stack.Peek();
            host.Clear(); top.Build(host); titleLabel.text = top.Title; top.OnShow();
        }
        else SetVisible(false);
    }

    void SetVisible(bool on) => root.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;

    // ---- chrome ----
    void BuildChrome()
    {
        root.style.position = Position.Absolute;
        root.style.left = 0; root.style.right = 0; root.style.top = 0; root.style.bottom = 0;

        backdrop = new VisualElement();
        backdrop.style.position = Position.Absolute;
        backdrop.style.left = 0; backdrop.style.right = 0; backdrop.style.top = 0; backdrop.style.bottom = 0;
        backdrop.style.backgroundColor = new Color(0f, 0f, 0f, 0.45f);
        backdrop.style.alignItems = Align.Center;
        backdrop.style.justifyContent = Justify.Center;
        backdrop.RegisterCallback<ClickEvent>(e => { if (e.target == backdrop) Close(); });   // click-out closes
        root.Add(backdrop);

        panel = new VisualElement();
        panel.style.width = 760; panel.style.maxWidth = Length.Percent(94f);
        panel.style.height = 480; panel.style.maxHeight = Length.Percent(92f);
        panel.style.backgroundColor = UIX.Panel;
        UIX.Radius(panel, 12); UIX.Border(panel, UIX.Line);
        backdrop.Add(panel);

        // title bar
        var bar = new VisualElement();
        bar.style.flexDirection = FlexDirection.Row;
        bar.style.alignItems = Align.Center;
        bar.style.justifyContent = Justify.SpaceBetween;
        bar.style.paddingLeft = 16; bar.style.paddingRight = 10;
        bar.style.height = 44;
        bar.style.borderBottomColor = UIX.Line; bar.style.borderBottomWidth = 1;
        panel.Add(bar);

        titleLabel = UIX.Make("Menu", 18, UIX.Text, FontStyle.Bold);
        bar.Add(titleLabel);

        var close = new Button(Close) { text = "✕" };
        close.style.width = 30; close.style.height = 30; close.style.fontSize = 16;
        close.style.color = UIX.Text; close.style.backgroundColor = UIX.Slot;
        UIX.Radius(close, 6); UIX.Border(close, UIX.Line);
        bar.Add(close);

        // content host
        host = new VisualElement();
        host.style.flexGrow = 1;
        host.style.paddingLeft = 16; host.style.paddingRight = 16;
        host.style.paddingTop = 14; host.style.paddingBottom = 16;
        panel.Add(host);
    }
}

// Base for a menu screen. Screens build VisualElements into the host and read player data via `menu`.
public abstract class UIScreen
{
    protected readonly MenuManager menu;
    protected UIScreen(MenuManager m) { menu = m; }

    public abstract string Title { get; }
    public abstract void Build(VisualElement host);
    public virtual void Refresh() { }
    public virtual void OnShow() { Refresh(); }
    public virtual void OnHide() { }
}
