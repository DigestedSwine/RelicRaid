// Shared combat enums. Damage schools match the GDD (Physical is the only non-magic school).
public enum DamageType { Physical, Holy, Nature, Frost, Storm, Spirit }

public enum StatusEffectType { None, Root, Slow, Stun }

// Minimal team split for the vertical slice (players vs hostiles). Faction lives on CharacterData later.
public enum Team { Players, Hostiles }
