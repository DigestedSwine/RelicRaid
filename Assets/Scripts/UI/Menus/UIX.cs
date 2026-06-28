using UnityEngine;
using UnityEngine.UIElements;

// Shared UI Toolkit styling helpers for the menu screens (mirrors the inline helpers PlayerHUD uses,
// hoisted here so every screen styles consistently). Pure presentation — no logic.
public static class UIX
{
    public static readonly Color Panel   = new Color(0.08f, 0.09f, 0.12f, 0.98f);
    public static readonly Color Slot    = new Color(0.13f, 0.14f, 0.18f, 1f);
    public static readonly Color Line     = new Color(1f, 1f, 1f, 0.22f);
    public static readonly Color Text     = new Color(0.92f, 0.94f, 0.97f);
    public static readonly Color TextDim  = new Color(0.62f, 0.66f, 0.74f);
    public static readonly Color Accent   = new Color(0.4f, 0.85f, 1f);

    public static void Radius(VisualElement e, float r)
    {
        e.style.borderTopLeftRadius = r; e.style.borderTopRightRadius = r;
        e.style.borderBottomLeftRadius = r; e.style.borderBottomRightRadius = r;
    }

    public static void Border(VisualElement e, Color c, float w = 1f)
    {
        e.style.borderTopColor = c; e.style.borderBottomColor = c;
        e.style.borderLeftColor = c; e.style.borderRightColor = c;
        e.style.borderTopWidth = w; e.style.borderBottomWidth = w;
        e.style.borderLeftWidth = w; e.style.borderRightWidth = w;
    }

    public static Label Make(string text, float size, Color color, FontStyle style = FontStyle.Normal)
    {
        var l = new Label(text);
        l.style.color = color; l.style.fontSize = size; l.style.unityFontStyleAndWeight = style;
        return l;
    }
}
