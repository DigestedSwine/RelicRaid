using UnityEngine;
using UnityEngine.UIElements;

// Screen-space player HUD (UI Toolkit), built in code: HP + MP bars (top-left) and an ability bar
// (bottom-center) with cooldown sweeps, MP affordability, keybinds, and clickable slots (which also
// serve as the future mobile on-screen ability buttons via InputReader.PressAbility).
[RequireComponent(typeof(UIDocument))]
public class PlayerHUD : MonoBehaviour
{
    public HealthComponent player;
    public SkillCaster caster;
    public Miner miner;
    public PlayerProgress progress;
    public PlayerTargeting targeting;
    public CameraFollow cameraFollow;

    VisualElement hpFill, mpFill;
    Label hpLabel, mpLabel;

    VisualElement miningRoot, miningFill;
    Label miningLabel;

    Label levelLabel, crystalsLabel, killsLabel;
    VisualElement xpFill;
    VisualElement optionsPopup;

    VisualElement targetRoot, targetHpFill;
    Label targetName, targetHpLabel;

    int slotCount;
    VisualElement[] slotRoot;
    VisualElement[] cdOverlay;
    Label[] cdLabel;
    Label[] nameLabel;

    void OnEnable() => Rebuild();

    // Public so the networked local player can re-point the HUD at itself once it spawns at runtime.
    public void Rebuild()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        if (root == null) return;
        root.Clear();
        root.pickingMode = PickingMode.Ignore;     // let world clicks (attack) pass through
        Build(root);
        Refresh();
    }

    void Update() => Refresh();

    // ---------- build ----------
    void Build(VisualElement root)
    {
        // HP / MP (top-left, below the minimap)
        var bars = new VisualElement();
        bars.style.position = Position.Absolute;
        bars.style.left = 18; bars.style.top = 196; bars.style.width = 300;
        bars.pickingMode = PickingMode.Ignore;
        root.Add(bars);
        MakeBar(bars, "HP", new Color(0.82f, 0.16f, 0.13f), out hpFill, out hpLabel);
        MakeBar(bars, "MP", new Color(0.20f, 0.45f, 0.85f), out mpFill, out mpLabel);

        // Ability bar (bottom-center)
        var skills = caster != null ? caster.skills : null;
        slotCount = skills != null ? skills.Length : 4;
        slotRoot = new VisualElement[slotCount];
        cdOverlay = new VisualElement[slotCount];
        cdLabel = new Label[slotCount];
        nameLabel = new Label[slotCount];

        var barRow = new VisualElement();
        barRow.style.position = Position.Absolute;
        barRow.style.left = 0; barRow.style.right = 0; barRow.style.bottom = 26;
        barRow.style.flexDirection = FlexDirection.Row;
        barRow.style.justifyContent = Justify.Center;
        barRow.pickingMode = PickingMode.Ignore;
        root.Add(barRow);

        for (int i = 0; i < slotCount; i++) MakeSlot(barRow, i, skills != null ? skills[i] : null);

        BuildMiningBar(root);
        BuildStatsPanel(root);
        BuildTargetFrame(root);
        BuildOptionsMenu(root);
    }

    void BuildOptionsMenu(VisualElement root)
    {
        // Gear button (top-right corner) toggles the popup.
        var gear = new Button(ToggleOptions) { text = "⚙" };
        gear.style.position = Position.Absolute;
        gear.style.right = 16; gear.style.top = 14;
        gear.style.width = 58; gear.style.height = 58; gear.style.fontSize = 30;   // bigger touch target
        gear.style.color = Color.white;
        gear.style.backgroundColor = new Color(0.12f, 0.13f, 0.17f, 0.92f);
        gear.style.unityTextAlign = TextAnchor.MiddleCenter;
        Radius(gear, 8); Border(gear, new Color(1, 1, 1, 0.3f));
        gear.pickingMode = PickingMode.Position;
        root.Add(gear);

        // Popup (hidden until the gear is clicked).
        optionsPopup = new VisualElement();
        optionsPopup.style.position = Position.Absolute;
        optionsPopup.style.right = 16; optionsPopup.style.top = 80; optionsPopup.style.minWidth = 220;
        optionsPopup.style.paddingLeft = 12; optionsPopup.style.paddingRight = 12;
        optionsPopup.style.paddingTop = 10; optionsPopup.style.paddingBottom = 12;
        optionsPopup.style.backgroundColor = new Color(0.08f, 0.09f, 0.12f, 0.97f);
        Radius(optionsPopup, 10); Border(optionsPopup, new Color(1, 1, 1, 0.25f));
        optionsPopup.style.display = DisplayStyle.None;
        optionsPopup.pickingMode = PickingMode.Position;   // catch clicks instead of passing through

        var title = new Label("Options");
        title.style.color = Color.white; title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.fontSize = 14; title.style.marginBottom = 8;
        optionsPopup.Add(title);

        if (cameraFollow != null)
        {
            var zl = new Label("Camera Zoom");
            zl.style.color = new Color(0.85f, 0.88f, 0.95f); zl.style.fontSize = 12; zl.style.marginBottom = 2;
            optionsPopup.Add(zl);
            var slider = new Slider(0f, 1f) { value = cameraFollow.GetZoomNormalized() };
            slider.style.width = 176; slider.pickingMode = PickingMode.Position;
            slider.RegisterValueChangedCallback(e => { if (cameraFollow != null) cameraFollow.SetZoomNormalized(e.newValue); });
            optionsPopup.Add(slider);
        }

        // Indestructible (godmode) — playtest toggle: take damage but never die.
        if (player != null)
        {
            var god = new Toggle("Indestructible") { value = player.indestructible };
            god.style.marginTop = 12;
            var gl = god.Q<Label>();
            if (gl != null) { gl.style.color = new Color(0.85f, 0.88f, 0.95f); gl.style.fontSize = 12; gl.style.minWidth = 110; }
            god.RegisterValueChangedCallback(e => { if (player != null) player.indestructible = e.newValue; });
            optionsPopup.Add(god);

            // Refill HP/MP — playtest convenience.
            var refill = new Button(() => { if (player != null) player.RefillToFull(); }) { text = "Refill HP / MP" };
            refill.style.marginTop = 8; refill.style.height = 28; refill.style.fontSize = 12;
            refill.style.color = Color.white;
            refill.style.backgroundColor = new Color(0.18f, 0.42f, 0.30f, 1f);
            Radius(refill, 6); Border(refill, new Color(1, 1, 1, 0.3f));
            optionsPopup.Add(refill);
        }

        root.Add(optionsPopup);
    }

    void ToggleOptions()
    {
        if (optionsPopup == null) return;
        optionsPopup.style.display = optionsPopup.style.display.value == DisplayStyle.None ? DisplayStyle.Flex : DisplayStyle.None;
    }

    void BuildTargetFrame(VisualElement root)
    {
        targetRoot = new VisualElement();
        targetRoot.style.position = Position.Absolute;
        targetRoot.style.left = 0; targetRoot.style.right = 0; targetRoot.style.top = 14;
        targetRoot.style.alignItems = Align.Center;
        targetRoot.pickingMode = PickingMode.Ignore;
        targetRoot.style.display = DisplayStyle.None;   // shown only when a target exists

        targetName = new Label("");
        targetName.style.color = Color.white; targetName.style.unityFontStyleAndWeight = FontStyle.Bold;
        targetName.style.fontSize = 15; targetName.style.marginBottom = 3;
        targetRoot.Add(targetName);

        var bg = new VisualElement();
        bg.style.width = 220; bg.style.height = 14;
        bg.style.backgroundColor = new Color(0f, 0f, 0f, 0.6f);
        Radius(bg, 4); Border(bg, new Color(1, 1, 1, 0.3f));
        targetHpFill = new VisualElement();
        targetHpFill.style.height = Length.Percent(100f);
        targetHpFill.style.width = Length.Percent(100f);
        targetHpFill.style.backgroundColor = new Color(0.82f, 0.2f, 0.16f);   // enemy red
        Radius(targetHpFill, 4);
        bg.Add(targetHpFill);
        targetRoot.Add(bg);

        targetHpLabel = new Label("");
        targetHpLabel.style.color = Color.white; targetHpLabel.style.fontSize = 10; targetHpLabel.style.marginTop = 2;
        targetRoot.Add(targetHpLabel);

        root.Add(targetRoot);
    }

    void BuildStatsPanel(VisualElement root)
    {
        var panel = new VisualElement();
        panel.style.position = Position.Absolute;
        panel.style.right = 18; panel.style.top = 82; panel.style.minWidth = 150;   // below the bigger gear button
        panel.style.alignItems = Align.FlexEnd;
        panel.pickingMode = PickingMode.Ignore;

        levelLabel = new Label("Lv 1");
        levelLabel.style.color = Color.white;
        levelLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        levelLabel.style.fontSize = 16;
        panel.Add(levelLabel);

        var xpBg = new VisualElement();
        xpBg.style.width = 150; xpBg.style.height = 8;
        xpBg.style.marginTop = 2; xpBg.style.marginBottom = 6;
        xpBg.style.backgroundColor = new Color(0f, 0f, 0f, 0.55f);
        Radius(xpBg, 3); Border(xpBg, new Color(1, 1, 1, 0.25f));
        xpFill = new VisualElement();
        xpFill.style.height = Length.Percent(100f);
        xpFill.style.width = Length.Percent(0f);
        xpFill.style.backgroundColor = new Color(0.95f, 0.8f, 0.2f);
        Radius(xpFill, 3);
        xpBg.Add(xpFill);
        panel.Add(xpBg);

        crystalsLabel = new Label("Crystals: 0");
        crystalsLabel.style.color = new Color(0.6f, 0.9f, 1f);
        crystalsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        panel.Add(crystalsLabel);

        killsLabel = new Label("Bears: 0");
        killsLabel.style.color = new Color(0.9f, 0.9f, 0.9f);
        panel.Add(killsLabel);

        root.Add(panel);
    }

    void BuildMiningBar(VisualElement root)
    {
        miningRoot = new VisualElement();
        miningRoot.style.position = Position.Absolute;
        miningRoot.style.left = 0; miningRoot.style.right = 0; miningRoot.style.bottom = 122;
        miningRoot.style.alignItems = Align.Center;
        miningRoot.pickingMode = PickingMode.Ignore;
        miningRoot.style.display = DisplayStyle.None;   // shown only while channeling

        miningLabel = new Label("Mining…");
        miningLabel.style.color = Color.white;
        miningLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        miningLabel.style.marginBottom = 4;
        miningRoot.Add(miningLabel);

        var bg = new VisualElement();
        bg.style.width = 240; bg.style.height = 16;
        bg.style.backgroundColor = new Color(0f, 0f, 0f, 0.6f);
        Radius(bg, 4); Border(bg, new Color(1, 1, 1, 0.3f));
        miningFill = new VisualElement();
        miningFill.style.height = Length.Percent(100f);
        miningFill.style.width = Length.Percent(0f);
        miningFill.style.backgroundColor = new Color(0.4f, 0.85f, 1f);   // wellspring cyan
        Radius(miningFill, 4);
        bg.Add(miningFill);
        miningRoot.Add(bg);
        root.Add(miningRoot);
    }

    void MakeBar(VisualElement parent, string tag, Color color, out VisualElement fill, out Label valueLabel)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.style.marginBottom = 6;
        row.pickingMode = PickingMode.Ignore;

        var label = new Label(tag);
        label.style.width = 30; label.style.color = Color.white;
        label.style.unityFontStyleAndWeight = FontStyle.Bold;
        row.Add(label);

        var bg = new VisualElement();
        bg.style.width = 200; bg.style.height = 18;
        bg.style.backgroundColor = new Color(0f, 0f, 0f, 0.55f);
        Radius(bg, 4); Border(bg, new Color(1, 1, 1, 0.25f));

        fill = new VisualElement();
        fill.style.height = Length.Percent(100f);
        fill.style.width = Length.Percent(100f);
        fill.style.backgroundColor = color;
        Radius(fill, 4);
        bg.Add(fill);
        row.Add(bg);

        valueLabel = new Label("");
        valueLabel.style.marginLeft = 8; valueLabel.style.color = Color.white; valueLabel.style.minWidth = 64;
        row.Add(valueLabel);

        parent.Add(row);
    }

    void MakeSlot(VisualElement parent, int index, SkillData skill)
    {
        var slot = new VisualElement();
        slot.style.width = 58; slot.style.height = 58;
        slot.style.marginLeft = 5; slot.style.marginRight = 5;
        slot.style.backgroundColor = new Color(0.10f, 0.11f, 0.14f, 0.92f);
        slot.style.overflow = Overflow.Hidden;
        Radius(slot, 7); Border(slot, new Color(1, 1, 1, 0.3f));
        slot.pickingMode = PickingMode.Position;     // clickable (mobile button + desktop)
        slotRoot[index] = slot;

        var name = new Label(skill != null ? Abbrev(skill.skillName) : "");
        name.style.flexGrow = 1;
        name.style.unityTextAlign = TextAnchor.MiddleCenter;
        name.style.whiteSpace = WhiteSpace.Normal;
        name.style.fontSize = 11; name.style.color = new Color(0.9f, 0.92f, 0.95f);
        nameLabel[index] = name;
        slot.Add(name);

        var overlay = new VisualElement();        // cooldown cover, drains top→down
        overlay.style.position = Position.Absolute;
        overlay.style.left = 0; overlay.style.right = 0; overlay.style.top = 0;
        overlay.style.height = Length.Percent(0f);
        overlay.style.backgroundColor = new Color(0f, 0f, 0f, 0.62f);
        overlay.pickingMode = PickingMode.Ignore;
        cdOverlay[index] = overlay;
        slot.Add(overlay);

        var cd = new Label("");
        cd.style.position = Position.Absolute;
        cd.style.left = 0; cd.style.right = 0; cd.style.top = 0; cd.style.bottom = 0;
        cd.style.unityTextAlign = TextAnchor.MiddleCenter;
        cd.style.fontSize = 20; cd.style.color = Color.white;
        cd.style.unityFontStyleAndWeight = FontStyle.Bold;
        cd.pickingMode = PickingMode.Ignore;
        cdLabel[index] = cd;
        slot.Add(cd);

        var key = new Label((index + 1).ToString());   // keybind badge
        key.style.position = Position.Absolute;
        key.style.left = 4; key.style.top = 2;
        key.style.fontSize = 11; key.style.color = new Color(1f, 1f, 1f, 0.7f);
        key.style.unityFontStyleAndWeight = FontStyle.Bold;
        key.pickingMode = PickingMode.Ignore;
        slot.Add(key);

        int slotIndex = index;
        slot.RegisterCallback<ClickEvent>(_ => { if (caster != null && caster.input != null) caster.input.PressAbility(slotIndex); });

        parent.Add(slot);
    }

    // ---------- update ----------
    void Refresh()
    {
        if (player != null && hpFill != null)
        {
            float hp = Mathf.Clamp01(player.HPFraction);
            hpFill.style.width = Length.Percent(hp * 100f);
            hpLabel.text = Mathf.CeilToInt(player.CurrentHP) + " / " + Mathf.CeilToInt(player.MaxHP);

            float mp = player.MaxMP > 0f ? Mathf.Clamp01(player.CurrentMP / player.MaxMP) : 0f;
            mpFill.style.width = Length.Percent(mp * 100f);
            mpLabel.text = Mathf.CeilToInt(player.CurrentMP) + " / " + Mathf.CeilToInt(player.MaxMP);
        }

        if (miningRoot != null)
        {
            bool show = miner != null && miner.IsMining;
            miningRoot.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (show)
            {
                miningFill.style.width = Length.Percent(Mathf.Clamp01(miner.Progress) * 100f);
                miningLabel.text = "Mining " + (miner.CurrentNode != null ? miner.CurrentNode.resourceType.ToString() : "");
            }
        }

        if (progress != null && levelLabel != null)
        {
            levelLabel.text = "Lv " + progress.level;
            int need = progress.XpToNext();
            xpFill.style.width = Length.Percent(need > 0 ? Mathf.Clamp01((float)progress.xp / need) * 100f : 0f);
            crystalsLabel.text = "Crystals: " + progress.GetResource(ResourceType.WellspringCrystal);
            killsLabel.text = "Bears: " + progress.GetKills("Bear");
        }

        if (targetRoot != null)
        {
            var t = targeting != null ? targeting.CurrentTarget : null;
            bool show = t != null && t.IsAlive;
            targetRoot.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (show)
            {
                var id = t.GetComponent<EnemyIdentity>();
                targetName.text = id != null ? id.displayName : "Enemy";
                targetHpFill.style.width = Length.Percent(Mathf.Clamp01(t.HPFraction) * 100f);
                targetHpLabel.text = Mathf.CeilToInt(t.CurrentHP) + " / " + Mathf.CeilToInt(t.MaxHP);
            }
        }

        if (caster == null || slotRoot == null) return;
        for (int i = 0; i < slotCount; i++)
        {
            var skill = i < caster.skills.Length ? caster.skills[i] : null;
            if (skill == null)
            {
                slotRoot[i].style.opacity = 0.3f;
                cdOverlay[i].style.height = Length.Percent(0f);
                cdLabel[i].text = "";
                continue;
            }

            float total = Mathf.Max(0.01f, skill.cooldown);
            float rem = caster.CooldownRemaining(i);
            float frac = Mathf.Clamp01(rem / total);
            cdOverlay[i].style.height = Length.Percent(frac * 100f);
            cdLabel[i].text = rem > 0.05f ? Mathf.CeilToInt(rem).ToString() : "";

            bool affordable = player == null || player.CurrentMP >= skill.mpCost;
            bool ready = rem <= 0.05f && affordable;
            slotRoot[i].style.opacity = ready ? 1f : (affordable ? 0.9f : 0.5f);
        }
    }

    // ---------- helpers ----------
    static string Abbrev(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Length <= 9 ? s : s.Substring(0, 9);
    }

    static void Radius(VisualElement e, float r)
    {
        e.style.borderTopLeftRadius = r; e.style.borderTopRightRadius = r;
        e.style.borderBottomLeftRadius = r; e.style.borderBottomRightRadius = r;
    }

    static void Border(VisualElement e, Color c)
    {
        e.style.borderTopColor = c; e.style.borderBottomColor = c;
        e.style.borderLeftColor = c; e.style.borderRightColor = c;
        e.style.borderTopWidth = 1; e.style.borderBottomWidth = 1;
        e.style.borderLeftWidth = 1; e.style.borderRightWidth = 1;
    }
}
