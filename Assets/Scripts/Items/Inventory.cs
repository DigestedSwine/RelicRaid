using System;
using System.Collections.Generic;
using UnityEngine;

// Pure data model for the player's bag — a fixed grid of slots. The UI binds to OnChanged and renders;
// loot drops call AddItem; persistence (local save / Nakama) serializes `slots` later. No UI here.
public class Inventory : MonoBehaviour
{
    public int capacity = 30;

    [Serializable]
    public class Slot
    {
        public ItemData item;
        public int count;
        public bool IsEmpty => item == null || count <= 0;
        public void Clear() { item = null; count = 0; }
    }

    public List<Slot> slots = new List<Slot>();
    public event Action OnChanged;

    void Awake()
    {
        while (slots.Count < capacity) slots.Add(new Slot());
    }

    public Slot At(int i) => (i >= 0 && i < slots.Count) ? slots[i] : null;

    // Adds to an existing stack first, then the first empty slot. Returns leftover that didn't fit.
    public int AddItem(ItemData item, int count = 1)
    {
        if (item == null || count <= 0) return count;

        if (item.maxStack > 1)
            foreach (var s in slots)
                if (s.item == item && s.count < item.maxStack)
                {
                    int room = item.maxStack - s.count;
                    int moved = Mathf.Min(room, count);
                    s.count += moved; count -= moved;
                    if (count <= 0) { OnChanged?.Invoke(); return 0; }
                }

        foreach (var s in slots)
            if (s.IsEmpty)
            {
                int moved = Mathf.Min(item.maxStack, count);
                s.item = item; s.count = moved; count -= moved;
                if (count <= 0) { OnChanged?.Invoke(); return 0; }
            }

        OnChanged?.Invoke();
        return count;   // bag full — caller can drop the remainder back in the world
    }

    public void RemoveAt(int index, int count = 1)
    {
        var s = At(index);
        if (s == null || s.IsEmpty) return;
        s.count -= count;
        if (s.count <= 0) s.Clear();
        OnChanged?.Invoke();
    }

    public bool IsFull()
    {
        foreach (var s in slots) if (s.IsEmpty) return false;
        return true;
    }
}
