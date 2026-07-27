using System;
using System.Collections.Generic;

namespace LearnHearthstone.Domain.Models
{
    public enum HeroPowerCategory
    {
        Economy,
        Buff,
        Combat,
        Minion,
        Discover,
        Health,
        Passive,
        HeroSwap,
        Other
    }

    public enum HeroPowerReplacementEligibility
    {
        DiscoverableAfterStart,
        InitialOnly,
        NonSelectable,
        Disabled
    }

    [Serializable]
    public sealed class HeroDefinition
    {
        public string HeroCardId;
        public int HeroDbfId;
        public string Name;
        public string ZhName;
        public int Health;
        public int Armor;
        public string ImagePath;
        public HeroPowerDefinition HeroPower;
        public HeroBuddyDefinition Buddy;
        public bool MissingBuddyMapping;
        public bool MissingHeroPowerMapping;
    }

    [Serializable]
    public sealed class HeroPowerDefinition
    {
        public string CardId;
        public int DbfId;
        public string Name;
        public string ZhName;
        public int Cost;
        public string Text;
        public string ZhText;
        public string ImagePath;
        public HeroPowerCategory PrimaryCategory;
        public List<string> Tags = new List<string>();
        public HeroPowerReplacementEligibility ReplacementEligibility;
    }

    [Serializable]
    public sealed class HeroBuddyDefinition
    {
        public string CardId;
        public int DbfId;
        public string Name;
        public string ZhName;
        public int TavernTier;
        public int Attack;
        public int Health;
        public string Text;
        public string ZhText;
        public string ImagePath;
        public List<Tribe> Tribes = new List<Tribe>();
        public List<Keyword> Keywords = new List<Keyword>();
        public bool ExcludedFromBuddyDiscover;
    }
}
