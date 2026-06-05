using System;
using System.Collections.Generic;

namespace LearnHearthstone.Domain.Models
{
    [Serializable]
    public sealed class GoldenMinionDefinition
    {
        public string CardId;
        public int DbfId;
        public int BaseAttack;
        public int BaseHealth;
        public List<Keyword> Keywords = new List<Keyword>();
        public string Text;
    }

    [Serializable]
    public sealed class MinionDefinition
    {
        public string Id;
        public string CardId;
        public int DbfId;
        public string Name;
        public int TavernTier;
        public int BaseAttack;
        public int BaseHealth;
        public List<Tribe> Tribes = new List<Tribe>();
        public List<Keyword> Keywords = new List<Keyword>();
        public string Text;
        public bool InPool;
        public int PoolCount;
        public GoldenMinionDefinition Golden;
        public string ImagePath;
        public List<string> EffectIds = new List<string>();
        public string TokenId;
    }

    [Serializable]
    public sealed class Enchantment
    {
        public string Id;
        public string SourceId;
        public int AttackBonus;
        public int HealthBonus;
        public List<Keyword> AddedKeywords = new List<Keyword>();
        public string Duration = "PERMANENT";
    }

    [Serializable]
    public sealed class MinionInstance
    {
        public string InstanceId;
        public string DefinitionId;
        public string CardId;
        public string Name;
        public int Attack;
        public int Health;
        public int MaxHealth;
        public int TavernTier;
        public List<Tribe> Tribes = new List<Tribe>();
        public List<Keyword> Keywords = new List<Keyword>();
        public string Text;
        public bool Golden;
        public BoardSide Owner;
        public List<Enchantment> Enchantments = new List<Enchantment>();
        public Dictionary<string, int> Counters = new Dictionary<string, int>();
        public bool CanAttack = true;
        public int AttacksThisCombat;
        public PoolSource PoolSource;
        public int PoolCopiesHeld;

        public MinionInstance Clone()
        {
            return new MinionInstance
            {
                InstanceId = InstanceId,
                DefinitionId = DefinitionId,
                CardId = CardId,
                Name = Name,
                Attack = Attack,
                Health = Health,
                MaxHealth = MaxHealth,
                TavernTier = TavernTier,
                Tribes = new List<Tribe>(Tribes),
                Keywords = new List<Keyword>(Keywords),
                Text = Text,
                Golden = Golden,
                Owner = Owner,
                Enchantments = new List<Enchantment>(Enchantments),
                Counters = new Dictionary<string, int>(Counters),
                CanAttack = CanAttack,
                AttacksThisCombat = AttacksThisCombat,
                PoolSource = PoolSource,
                PoolCopiesHeld = PoolCopiesHeld
            };
        }
    }

    public static class MinionFactory
    {
        public static MinionInstance Create(MinionDefinition definition, BoardSide owner, string suffix, bool golden = false, PoolSource source = PoolSource.Pool, int poolCopiesHeld = 1)
        {
            var attack = golden && definition.Golden != null ? definition.Golden.BaseAttack : definition.BaseAttack;
            var health = golden && definition.Golden != null ? definition.Golden.BaseHealth : definition.BaseHealth;
            var keywords = golden && definition.Golden != null ? definition.Golden.Keywords : definition.Keywords;

            return new MinionInstance
            {
                InstanceId = owner.ToString().ToLowerInvariant() + "-" + definition.Id + "-" + suffix,
                DefinitionId = definition.Id,
                CardId = definition.CardId,
                Name = definition.Name,
                Attack = attack,
                Health = health,
                MaxHealth = health,
                TavernTier = definition.TavernTier,
                Tribes = new List<Tribe>(definition.Tribes),
                Keywords = new List<Keyword>(keywords),
                Text = golden && definition.Golden != null ? definition.Golden.Text : definition.Text,
                Golden = golden,
                Owner = owner,
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                CanAttack = true,
                AttacksThisCombat = 0,
                PoolSource = source,
                PoolCopiesHeld = poolCopiesHeld
            };
        }
    }
}
