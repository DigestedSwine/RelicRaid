using System.Collections.Generic;

public static class UnitRegistry
{
    static readonly List<Unit> all = new();

    public static IReadOnlyList<Unit> All => all;

    public static void Register(Unit unit) => all.Add(unit);
    public static void Unregister(Unit unit) => all.Remove(unit);
}
