using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Character + Inventory screen: left = level/XP, equipment doll, and live stats (from StatBlock);
// right = the inventory grid + Wellspring Crystal count. Gray-boxed — empty slots until loot lands.
public class CharacterScreen : UIScreen
{
    public CharacterScreen(MenuManager m) : base(m) { }
    public override string Title => "Character";

    Label levelLabel, crystalsLabel;
    VisualElement xpFill, equipHost, invHost;
    readonly Dictionary<string, Label> statValues = new Dictionary<string, Label>();

    static readonly (string key, string label)[] Stats =
    {
        ("hp","Health"),("mp","Mana"),("atk","Attack"),("matk","Magic Atk"),
        ("def","Defense"),("mdef","Magic Def"),("spd","Speed"),("crit","Crit"),("dodge","Dodge"),
    };

    public override void Build(VisualElement host)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.flexGrow = 1;
        host.Add(row);

        // ---------------- LEFT: equipment + stats ----------------
        var left = new VisualElement();
        left.style.width = Length.Percent(42f);
        left.style.marginRight = 14;
        row.Add(left);

        levelLabel = UIX.Make("Level 1", 16, UIX.Text, FontStyle.Bold);
        left.Add(levelLabel);

        var xpBg = new VisualElement();
        xpBg.style.height = 7; xpBg.style.marginTop = 3; xpBg.style.marginBottom = 10;
        xpBg.style.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
        UIX.Radius(xpBg, 3); UIX.Border(xpBg, UIX.Line);
        xpFill = new VisualElement();
        xpFill.style.height = Length.Percent(100f); xpFill.style.width = Length.Percent(0f);
        xpFill.style.backgroundColor = new Color(0.95f, 0.8f, 0.2f);
        UIX.Radius(xpFill, 3);
        xpBg.Add(xpFill); left.Add(xpBg);

        left.Add(SectionHeader("Equipment"));
        equipHost = new VisualElement();
        equipHost.style.flexDirection = FlexDirection.Row;
        equipHost.style.flexWrap = Wrap.Wrap;
        left.Add(equipHost);

        left.Add(SectionHeader("Stats"));
        foreach (var s in Stats)
        {
            var r = new VisualElement();
            r.style.flexDirection = FlexDirection.Row;
            r.style.justifyContent = Justify.SpaceBetween;
            r.style.marginBottom = 2;
            r.Add(UIX.Make(s.label, 13, UIX.TextDim));
            var val = UIX.Make("—", 13, UIX.Text, FontStyle.Bold);
            statValues[s.key] = val;
            r.Add(val);
            left.Add(r);
        }

        // ---------------- RIGHT: inventory ----------------
        var right = new VisualElement();
        right.style.flexGrow = 1;
        row.Add(right);

        var invHead = new VisualElement();
        invHead.style.flexDirection = FlexDirection.Row;
        invHead.style.justifyContent = Justify.SpaceBetween;
        invHead.style.alignItems = Align.Center;
        invHead.Add(SectionHeader("Inventory"));
        crystalsLabel = UIX.Make("◆ 0", 14, UIX.Accent, FontStyle.Bold);
        invHead.Add(crystalsLabel);
        right.Add(invHead);

        var scroll = new ScrollView();
        scroll.style.flexGrow = 1;
        invHost = new VisualElement();
        invHost.style.flexDirection = FlexDirection.Row;
        invHost.style.flexWrap = Wrap.Wrap;
        scroll.Add(invHost);
        right.Add(scroll);

        RebuildEquipment();
        RebuildInventory();
    }

    Label SectionHeader(string text)
    {
        var l = UIX.Make(text.ToUpper(), 11, UIX.TextDim, FontStyle.Bold);
        l.style.marginTop = 10; l.style.marginBottom = 5;
        l.style.letterSpacing = 1f;
        return l;
    }

    // ---- slot tiles ----
    VisualElement Tile(float size, ItemData item, string emptyLabel)
    {
        var t = new VisualElement();
        t.style.width = size; t.style.height = size;
        t.style.marginRight = 6; t.style.marginBottom = 6;
        t.style.backgroundColor = UIX.Slot;
        t.style.alignItems = Align.Center; t.style.justifyContent = Justify.Center;
        UIX.Radius(t, 6);
        UIX.Border(t, item != null ? item.RarityColor : UIX.Line, item != null ? 2f : 1f);

        var label = UIX.Make(item != null ? Abbrev(item.displayName) : emptyLabel, 10,
                             item != null ? item.RarityColor : UIX.TextDim);
        label.style.unityTextAlign = TextAnchor.MiddleCenter;
        label.style.whiteSpace = WhiteSpace.Normal;
        t.Add(label);
        return t;
    }

    void RebuildEquipment()
    {
        equipHost.Clear();
        if (menu.equipment == null) { equipHost.Add(UIX.Make("(no Equipment)", 11, UIX.TextDim)); return; }
        foreach (var slot in Equipment.SlotOrder)
            equipHost.Add(Tile(72, menu.equipment.Get(slot), slot.ToString()));
    }

    void RebuildInventory()
    {
        invHost.Clear();
        if (menu.inventory == null) { invHost.Add(UIX.Make("(no Inventory)", 11, UIX.TextDim)); return; }
        for (int i = 0; i < menu.inventory.slots.Count; i++)
        {
            var s = menu.inventory.slots[i];
            invHost.Add(Tile(56, s.IsEmpty ? null : s.item, ""));
        }
    }

    public override void OnShow()
    {
        if (menu.inventory != null) menu.inventory.OnChanged += RebuildInventory;
        if (menu.equipment != null) menu.equipment.OnChanged += RebuildEquipment;
        Refresh();
    }

    public override void OnHide()
    {
        if (menu.inventory != null) menu.inventory.OnChanged -= RebuildInventory;
        if (menu.equipment != null) menu.equipment.OnChanged -= RebuildEquipment;
    }

    public override void Refresh()
    {
        var p = menu.progress;
        if (p != null && levelLabel != null)
        {
            levelLabel.text = "Level " + p.level;
            int need = p.XpToNext();
            xpFill.style.width = Length.Percent(need > 0 ? Mathf.Clamp01((float)p.xp / need) * 100f : 0f);
            crystalsLabel.text = "◆ " + p.GetResource(ResourceType.WellspringCrystal);
        }

        var st = menu.player != null ? menu.player.stats : null;
        if (st != null)
        {
            statValues["hp"].text = Mathf.RoundToInt(st.maxHP).ToString();
            statValues["mp"].text = Mathf.RoundToInt(st.maxMP).ToString();
            statValues["atk"].text = Mathf.RoundToInt(st.atk).ToString();
            statValues["matk"].text = Mathf.RoundToInt(st.matk).ToString();
            statValues["def"].text = Mathf.RoundToInt(st.def).ToString();
            statValues["mdef"].text = Mathf.RoundToInt(st.mdef).ToString();
            statValues["spd"].text = st.spd.ToString("0.0");
            statValues["crit"].text = Mathf.RoundToInt(st.crit * 100f) + "%";
            statValues["dodge"].text = Mathf.RoundToInt(st.dodge * 100f) + "%";
        }
    }

    static string Abbrev(string s) => string.IsNullOrEmpty(s) ? "" : (s.Length <= 10 ? s : s.Substring(0, 10));
}
