using System.Collections.Generic;

namespace LearnHearthstone.Domain.Models
{
    public enum SpellCardTemplate
    {
        Auto,
        Unknown,
        Spell,
        TavernSpell,
        Spellcraft,
        BloodGem,
        DarkmoonPrize,
        Generated
    }

    public enum SpellTargetTemplate
    {
        Auto,
        Unknown,
        None,
        FriendlyMinion,
        TavernShop,
        Hand,
        Board,
        Discover,
        AnyMinion
    }

    public enum SpellEffectTemplate
    {
        Auto,
        Unknown,
        BuffStats,
        GrantKeyword,
        Economy,
        Discover,
        GenerateCard,
        Refresh,
        ShopInteraction,
        Transform,
        Summon,
        Copy,
        Utility
    }

    public sealed class TavernSpellDefinition
    {
        public string Id { get; set; }
        public int SourceId { get; set; }
        public string CardNumber { get; set; }
        public string Name { get; set; }
        public string EnglishName { get; set; }
        public string Type { get; set; }
        public string SpecialType { get; set; }
        public string Category { get; set; }
        public string Faction { get; set; }
        public List<string> AvailableModes { get; set; }
        public int Cost { get; set; }
        public int TavernTier { get; set; }
        public bool InPool { get; set; }
        public List<string> Keywords { get; set; }
        public string Text { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string ImagePath { get; set; }
        public List<string> EffectIds { get; set; }
        public List<string> Tags { get; set; }
        public SpellCardTemplate CardTemplate { get; set; }
        public SpellTargetTemplate TargetTemplate { get; set; }
        public SpellEffectTemplate EffectTemplate { get; set; }
        public string ImplementationStatus { get; set; }
        public string Notes { get; set; }
    }
}
