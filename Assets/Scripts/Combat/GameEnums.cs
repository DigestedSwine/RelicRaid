// Faction + class identity (GDD). Shared by CharacterData and SkillData. Defined now so SkillData can
// reference them; the full CharacterData ScriptableObject comes later.

public enum Faction { Crownsworn, Oakhaven, Ironfrost }

public enum CharacterClass
{
    // Crownsworn
    Ironvow, Gravesworn, Edictcaster, Chainwarden,
    // Oakhaven
    Thornguard, Thicketblade, Rotweaver, Grovekeeper,
    // Ironfrost
    Skaldbreaker, Bloodaxe, Runecaster, Spiritcaller
}
