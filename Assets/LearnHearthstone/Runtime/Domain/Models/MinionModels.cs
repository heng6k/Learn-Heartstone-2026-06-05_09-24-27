using System;
using System.Collections.Generic;

namespace LearnHearthstone.Domain.Models
{
    public static class MinionEffectIds
    {
        public const string AlwaysGoldenNoTripleReward = "minion.always-golden-no-triple-reward";
    }

    [Serializable]
    public sealed class GoldenMinionDefinition
    {
        public string CardId;
        public int DbfId;
        public int BaseAttack;
        public int BaseHealth;
        public List<Keyword> Keywords = new List<Keyword>();
        public List<Keyword> OfficialKeywords = new List<Keyword>();
        public string Text;
    }

    [Serializable]
    public sealed class MinionDefinition
    {
        public string Id;
        public string CardId;
        public string ResearchKey;
        public string RevisionId;
        public string EffectRevision;
        public string SourceLevel;
        public string ImplementationStatus;
        public int DbfId;
        public string Name;
        public int TavernTier;
        public int BaseAttack;
        public int BaseHealth;
        public List<Tribe> Tribes = new List<Tribe>();
        public List<Keyword> Keywords = new List<Keyword>();
        public List<Keyword> OfficialKeywords = new List<Keyword>();
        public string Text;
        public bool InPool;
        public int PoolCount;
        public GoldenMinionDefinition Golden;
        public string ImagePath;
        public string ImageSource;
        public List<string> EffectIds = new List<string>();
        public List<string> Tags = new List<string>();
        public List<RecruitActionDefinition> RecruitActions = new List<RecruitActionDefinition>();
        public string TokenId;
    }

    [Serializable]
    public sealed class Enchantment
    {
        public string Id;
        public string SourceId;
        public EnchantmentKind Kind = EnchantmentKind.Unspecified;
        public int AttackBonus;
        public int HealthBonus;
        public List<Keyword> AddedKeywords = new List<Keyword>();
        public string Duration = "PERMANENT";

        public Enchantment Clone()
        {
            return new Enchantment
            {
                Id = Id,
                SourceId = SourceId,
                Kind = Kind,
                AttackBonus = AttackBonus,
                HealthBonus = HealthBonus,
                AddedKeywords = AddedKeywords == null ? new List<Keyword>() : new List<Keyword>(AddedKeywords),
                Duration = Duration
            };
        }
    }

    [Serializable]
    public sealed class MinionInstance
    {
        public CardKind CardKind = CardKind.Minion;
        public string InstanceId;
        public string DefinitionId;
        public string CardId;
        public string Name;
        public string ZhName;
        public int Cost;
        public int BaseAttack;
        public int BaseHealth;
        public int Attack;
        public int Health;
        public int MaxHealth;
        public int TavernTier;
        public List<Tribe> Tribes = new List<Tribe>();
        public List<Keyword> Keywords = new List<Keyword>();
        public List<Keyword> OfficialKeywords = new List<Keyword>();
        public string Text;
        public string ZhText;
        public bool Golden;
        public BoardSide Owner;
        public List<Enchantment> Enchantments = new List<Enchantment>();
        public Dictionary<string, int> Counters = new Dictionary<string, int>();
        public bool CanAttack = true;
        public int AttacksThisCombat;
        public PoolSource OriginPoolSource;
        public bool CanReturnToPoolAfterAttach;
        public PoolSource PoolSource;
        public int PoolCopiesHeld;
        public string ImagePath;
        public List<string> EffectIds = new List<string>();
        public List<string> Tags = new List<string>();

        public MinionInstance Clone()
        {
            return new MinionInstance
            {
                CardKind = CardKind,
                InstanceId = InstanceId,
                DefinitionId = DefinitionId,
                CardId = CardId,
                Name = Name,
                ZhName = ZhName,
                Cost = Cost,
                BaseAttack = BaseAttack,
                BaseHealth = BaseHealth,
                Attack = Attack,
                Health = Health,
                MaxHealth = MaxHealth,
                TavernTier = TavernTier,
                Tribes = new List<Tribe>(Tribes),
                Keywords = new List<Keyword>(Keywords),
                OfficialKeywords = new List<Keyword>(OfficialKeywords),
                Text = Text,
                ZhText = ZhText,
                Golden = Golden,
                Owner = Owner,
                Enchantments = Enchantments == null
                    ? new List<Enchantment>()
                    : Enchantments.ConvertAll(enchantment => enchantment?.Clone()),
                Counters = new Dictionary<string, int>(Counters),
                CanAttack = CanAttack,
                AttacksThisCombat = AttacksThisCombat,
                OriginPoolSource = OriginPoolSource,
                CanReturnToPoolAfterAttach = CanReturnToPoolAfterAttach,
                PoolSource = PoolSource,
                PoolCopiesHeld = PoolCopiesHeld,
                ImagePath = ImagePath,
                EffectIds = new List<string>(EffectIds),
                Tags = new List<string>(Tags)
            };
        }
    }

    public static class MinionFactory
    {
        public static MinionInstance Create(MinionDefinition definition, BoardSide owner, string suffix, bool golden = false, PoolSource source = PoolSource.Pool, int poolCopiesHeld = 1)
        {
            var alwaysGoldenNoTripleReward = definition.EffectIds != null &&
                definition.EffectIds.Contains(MinionEffectIds.AlwaysGoldenNoTripleReward);
            var createGolden = golden || alwaysGoldenNoTripleReward;
            var attack = createGolden && definition.Golden != null ? definition.Golden.BaseAttack : definition.BaseAttack;
            var health = createGolden && definition.Golden != null ? definition.Golden.BaseHealth : definition.BaseHealth;
            var keywords = createGolden && definition.Golden != null ? definition.Golden.Keywords : definition.Keywords;
            var officialKeywords = createGolden && definition.Golden != null ? definition.Golden.OfficialKeywords : definition.OfficialKeywords;
            var counters = new Dictionary<string, int>();
            if (alwaysGoldenNoTripleReward)
            {
                counters["triple-reward-granted"] = 1;
            }

            return new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = owner.ToString().ToLowerInvariant() + "-" + definition.Id + "-" + suffix,
                DefinitionId = definition.Id,
                CardId = definition.CardId,
                Name = definition.Name,
                Cost = 3,
                BaseAttack = attack,
                BaseHealth = health,
                Attack = attack,
                Health = health,
                MaxHealth = health,
                TavernTier = definition.TavernTier,
                Tribes = new List<Tribe>(definition.Tribes),
                Keywords = new List<Keyword>(keywords),
                OfficialKeywords = new List<Keyword>(officialKeywords),
                Text = createGolden && definition.Golden != null ? definition.Golden.Text : definition.Text,
                Golden = createGolden,
                Owner = owner,
                Enchantments = new List<Enchantment>(),
                Counters = counters,
                CanAttack = true,
                AttacksThisCombat = 0,
                OriginPoolSource = source,
                CanReturnToPoolAfterAttach = source == PoolSource.Pool && poolCopiesHeld > 0,
                PoolSource = source,
                PoolCopiesHeld = poolCopiesHeld,
                ImagePath = definition.ImagePath,
                EffectIds = new List<string>(definition.EffectIds),
                Tags = new List<string>(definition.Tags)
            };
        }

        public static MinionInstance Create(TavernSpellDefinition definition, BoardSide owner, string suffix)
        {
            return new MinionInstance
            {
                CardKind = CardKind.TavernSpell,
                InstanceId = owner.ToString().ToLowerInvariant() + "-" + definition.Id + "-" + suffix,
                DefinitionId = definition.Id,
                CardId = definition.CardNumber,
                Name = definition.Name,
                Cost = definition.Cost,
                BaseAttack = 0,
                BaseHealth = 0,
                Attack = 0,
                Health = 0,
                MaxHealth = 0,
                TavernTier = definition.TavernTier,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword> { Keyword.TavernSpell },
                Text = definition.Text,
                Golden = false,
                Owner = owner,
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                CanAttack = false,
                AttacksThisCombat = 0,
                OriginPoolSource = PoolSource.Copy,
                CanReturnToPoolAfterAttach = false,
                PoolSource = PoolSource.Copy,
                PoolCopiesHeld = 0,
                ImagePath = definition.ImagePath,
                EffectIds = definition.EffectIds == null ? new List<string>() : new List<string>(definition.EffectIds),
                Tags = definition.Tags == null ? new List<string>() : new List<string>(definition.Tags)
            };
        }

        public static MinionInstance Create(HeroPowerDefinition definition, BoardSide owner, string suffix)
        {
            var tags = definition.Tags == null ? new List<string>() : new List<string>(definition.Tags);
            var categoryTag = "category:" + definition.PrimaryCategory;
            var eligibilityTag = "eligibility:" + definition.ReplacementEligibility;
            if (!tags.Contains(categoryTag))
            {
                tags.Add(categoryTag);
            }

            if (!tags.Contains(eligibilityTag))
            {
                tags.Add(eligibilityTag);
            }

            return new MinionInstance
            {
                CardKind = CardKind.HeroPower,
                InstanceId = owner.ToString().ToLowerInvariant() + "-hero-power-" + suffix,
                DefinitionId = definition.CardId,
                CardId = definition.CardId,
                Name = definition.Name,
                ZhName = definition.ZhName,
                Cost = definition.Cost,
                BaseAttack = 0,
                BaseHealth = 0,
                Attack = 0,
                Health = 0,
                MaxHealth = 0,
                TavernTier = 0,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword>(),
                OfficialKeywords = new List<Keyword>(),
                Text = definition.Text,
                ZhText = definition.ZhText,
                Golden = false,
                Owner = owner,
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                CanAttack = false,
                AttacksThisCombat = 0,
                OriginPoolSource = PoolSource.Debug,
                CanReturnToPoolAfterAttach = false,
                PoolSource = PoolSource.Debug,
                PoolCopiesHeld = 0,
                ImagePath = definition.ImagePath,
                EffectIds = new List<string>(),
                Tags = tags
            };
        }

        public static MinionInstance Create(HeroDefinition definition, BoardSide owner, string suffix)
        {
            var tags = new List<string>
            {
                "hero",
                "armor:" + Math.Max(0, definition.Armor)
            };
            if (definition.HeroPower != null)
            {
                tags.Add("hero_power:" + definition.HeroPower.Name);
                tags.Add("hero_power_card:" + definition.HeroPower.CardId);
            }

            return new MinionInstance
            {
                CardKind = CardKind.Hero,
                InstanceId = owner.ToString().ToLowerInvariant() + "-hero-" + suffix,
                DefinitionId = definition.HeroCardId,
                CardId = definition.HeroCardId,
                Name = definition.Name,
                ZhName = definition.ZhName,
                Cost = 0,
                BaseAttack = Math.Max(0, definition.Armor),
                BaseHealth = definition.Health > 0 ? definition.Health : 30,
                Attack = Math.Max(0, definition.Armor),
                Health = definition.Health > 0 ? definition.Health : 30,
                MaxHealth = definition.Health > 0 ? definition.Health : 30,
                TavernTier = 0,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword>(),
                OfficialKeywords = new List<Keyword>(),
                Text = definition.HeroPower == null ? string.Empty : definition.HeroPower.Text,
                ZhText = definition.HeroPower == null ? string.Empty : definition.HeroPower.ZhText,
                Golden = false,
                Owner = owner,
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                CanAttack = false,
                AttacksThisCombat = 0,
                OriginPoolSource = PoolSource.Debug,
                CanReturnToPoolAfterAttach = false,
                PoolSource = PoolSource.Debug,
                PoolCopiesHeld = 0,
                ImagePath = definition.ImagePath,
                EffectIds = new List<string>(),
                Tags = tags
            };
        }

        public static MinionInstance Create(HeroBuddyDefinition definition, BoardSide owner, string suffix, PoolSource source = PoolSource.Debug, int poolCopiesHeld = 0)
        {
            var keywords = new List<Keyword>(definition.Keywords);
            if (!string.IsNullOrWhiteSpace(definition.Text) &&
                definition.Text.IndexOf("Magnetic", StringComparison.OrdinalIgnoreCase) >= 0 &&
                !keywords.Contains(Keyword.Magnetic))
            {
                keywords.Add(Keyword.Magnetic);
            }

            if (!string.IsNullOrWhiteSpace(definition.Text) &&
                definition.Text.IndexOf("Rally", StringComparison.OrdinalIgnoreCase) >= 0 &&
                !keywords.Contains(Keyword.Rally))
            {
                keywords.Add(Keyword.Rally);
            }

            return new MinionInstance
            {
                CardKind = CardKind.HeroBuddy,
                InstanceId = owner.ToString().ToLowerInvariant() + "-hero-buddy-" + suffix,
                DefinitionId = definition.CardId,
                CardId = definition.CardId,
                Name = definition.Name,
                ZhName = definition.ZhName,
                Cost = 3,
                BaseAttack = definition.Attack,
                BaseHealth = definition.Health,
                Attack = definition.Attack,
                Health = definition.Health,
                MaxHealth = definition.Health,
                TavernTier = definition.TavernTier,
                Tribes = new List<Tribe>(definition.Tribes),
                Keywords = keywords,
                OfficialKeywords = new List<Keyword>(keywords),
                Text = definition.Text,
                ZhText = definition.ZhText,
                Golden = false,
                Owner = owner,
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                CanAttack = true,
                AttacksThisCombat = 0,
                OriginPoolSource = source,
                CanReturnToPoolAfterAttach = (source == PoolSource.Pool || source == PoolSource.Buddy) && poolCopiesHeld > 0,
                PoolSource = source,
                PoolCopiesHeld = poolCopiesHeld,
                ImagePath = definition.ImagePath,
                EffectIds = new List<string>(),
                Tags = new List<string> { "hero_buddy" }
            };
        }
    }
}
