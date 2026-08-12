using System;
using System.Collections.Generic;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Domain.Data
{
    public sealed class MinionEffectCatalog
    {
        private readonly Dictionary<string, MinionEffectDefinition> effects;

        public MinionEffectCatalog(IEnumerable<MinionEffectDefinition> definitions)
        {
            effects = new Dictionary<string, MinionEffectDefinition>(StringComparer.Ordinal);
            foreach (var definition in definitions)
            {
                effects[definition.Id] = definition;
            }
        }

        public static MinionEffectCatalog CreateDefault()
        {
            return new MinionEffectCatalog(new[]
            {
                new MinionEffectDefinition
                {
                    Id = "battlecry_self_buff_2_2",
                    Triggers = { new EffectTrigger { EventType = MechanicEventType.CardPlayed } },
                    Actions =
                    {
                        new TargetedMechanicAction
                        {
                            Target = new EffectTarget { Type = EffectTargetType.Source },
                            Action = new MechanicAction
                            {
                                Type = MechanicActionType.BuffStats,
                                Attack = 2,
                                Health = 2,
                                SourceId = "battlecry_self_buff_2_2"
                            }
                        }
                    }
                },
                new MinionEffectDefinition
                {
                    Id = "minion_sold_gain_gold_1",
                    Triggers = { new EffectTrigger { EventType = MechanicEventType.MinionSold } },
                    Actions =
                    {
                        new TargetedMechanicAction
                        {
                            Target = new EffectTarget { Type = EffectTargetType.Source },
                            Action = new MechanicAction
                            {
                                Type = MechanicActionType.GainGold,
                                Gold = 1,
                                SourceId = "minion_sold_gain_gold_1"
                            }
                        }
                    }
                },
                new MinionEffectDefinition
                {
                    Id = "card_played_buff_hand_1_1",
                    Triggers = { new EffectTrigger { EventType = MechanicEventType.CardPlayed } },
                    Actions =
                    {
                        new TargetedMechanicAction
                        {
                            Target = new EffectTarget { Type = EffectTargetType.AllFriendlyHand },
                            Action = new MechanicAction
                            {
                                Type = MechanicActionType.BuffStats,
                                Attack = 1,
                                Health = 1,
                                SourceId = "card_played_buff_hand_1_1"
                            }
                        }
                    }
                },
                new MinionEffectDefinition
                {
                    Id = "turn_ended_self_buff_1_1",
                    Triggers = { new EffectTrigger { EventType = MechanicEventType.TurnEnded } },
                    Actions =
                    {
                        new TargetedMechanicAction
                        {
                            Target = new EffectTarget { Type = EffectTargetType.Source },
                            Action = new MechanicAction
                            {
                                Type = MechanicActionType.BuffStats,
                                Attack = 1,
                                Health = 1,
                                SourceId = "turn_ended_self_buff_1_1"
                            }
                        }
                    }
                },
                new MinionEffectDefinition
                {
                    Id = "turn_started_self_buff_1_1",
                    Triggers = { new EffectTrigger { EventType = MechanicEventType.TurnStarted } },
                    Actions =
                    {
                        new TargetedMechanicAction
                        {
                            Target = new EffectTarget { Type = EffectTargetType.Source },
                            Action = new MechanicAction
                            {
                                Type = MechanicActionType.BuffStats,
                                Attack = 1,
                                Health = 1,
                                SourceId = "turn_started_self_buff_1_1"
                            }
                        }
                    }
                },
                new MinionEffectDefinition
                {
                    Id = "card_bought_buff_self_1_1",
                    Triggers = { new EffectTrigger { EventType = MechanicEventType.CardBought } },
                    Actions =
                    {
                        new TargetedMechanicAction
                        {
                            Target = new EffectTarget { Type = EffectTargetType.Source },
                            Action = new MechanicAction
                            {
                                Type = MechanicActionType.BuffStats,
                                Attack = 1,
                                Health = 1,
                                SourceId = "card_bought_buff_self_1_1"
                            }
                        }
                    }
                },
                new MinionEffectDefinition
                {
                    Id = "shop_refreshed_buff_shop_1_1",
                    Triggers = { new EffectTrigger { EventType = MechanicEventType.ShopRefreshed } },
                    Actions =
                    {
                        new TargetedMechanicAction
                        {
                            Target = new EffectTarget { Type = EffectTargetType.AllFriendlyShop },
                            Action = new MechanicAction
                            {
                                Type = MechanicActionType.BuffStats,
                                Attack = 1,
                                Health = 1,
                                SourceId = "shop_refreshed_buff_shop_1_1"
                            }
                        }
                    }
                },
                new MinionEffectDefinition
                {
                    Id = "tavern_spell_cast_buff_self_1_1",
                    Triggers = { new EffectTrigger { EventType = MechanicEventType.TavernSpellCast } },
                    Actions =
                    {
                        new TargetedMechanicAction
                        {
                            Target = new EffectTarget { Type = EffectTargetType.Source },
                            Action = new MechanicAction
                            {
                                Type = MechanicActionType.BuffStats,
                                Attack = 1,
                                Health = 1,
                                SourceId = "tavern_spell_cast_buff_self_1_1"
                            }
                        }
                    }
                },
                CreateBuffEffect("battlecry_random_friendly_buff_1_1", MechanicEventType.CardPlayed, EffectTargetType.RandomFriendlyBoard, 1, 1),
                CreateBuffEffect("deathrattle_buff_random_friendly_2_2", MechanicEventType.MinionDied, EffectTargetType.RandomFriendlyBoard, 2, 2),
                CreateBuffEffect("avenge_2_buff_self_2_2", MechanicEventType.MinionDied, EffectTargetType.Source, 2, 2),
                new MinionEffectDefinition
                {
                    Id = "future_shop_typed_buff_1_1",
                    Triggers = { new EffectTrigger { EventType = MechanicEventType.CardPlayed } },
                    Actions =
                    {
                        new TargetedMechanicAction
                        {
                            Target = new EffectTarget { Type = EffectTargetType.Source },
                            Action = new MechanicAction
                            {
                                Type = MechanicActionType.ModifyShopGrowth,
                                Scope = BuffScope.ShopGlobal,
                                Tribe = Tribe.Elemental,
                                Attack = 1,
                                Health = 1,
                                SourceId = "future_shop_typed_buff_1_1"
                            }
                        }
                    }
                },
                new MinionEffectDefinition
                {
                    Id = "battlecry_add_divine_shield_random_friendly",
                    Triggers = { new EffectTrigger { EventType = MechanicEventType.CardPlayed } },
                    Actions =
                    {
                        new TargetedMechanicAction
                        {
                            Target = new EffectTarget { Type = EffectTargetType.RandomFriendlyBoard },
                            Action = new MechanicAction
                            {
                                Type = MechanicActionType.AddKeyword,
                                Keyword = Keyword.DivineShield,
                                SourceId = "battlecry_add_divine_shield_random_friendly"
                            }
                        }
                    }
                },
                new MinionEffectDefinition
                {
                    Id = "battlecry_add_taunt_self",
                    Triggers = { new EffectTrigger { EventType = MechanicEventType.CardPlayed } },
                    Actions =
                    {
                        new TargetedMechanicAction
                        {
                            Target = new EffectTarget { Type = EffectTargetType.Source },
                            Action = new MechanicAction
                            {
                                Type = MechanicActionType.AddKeyword,
                                Keyword = Keyword.Taunt,
                                SourceId = "battlecry_add_taunt_self"
                            }
                        }
                    }
                },
                new MinionEffectDefinition
                {
                    Id = "combat_reborn_self",
                    Triggers = { new EffectTrigger { EventType = MechanicEventType.MinionDied } },
                    Actions =
                    {
                        new TargetedMechanicAction
                        {
                            Target = new EffectTarget { Type = EffectTargetType.Source },
                            Action = new MechanicAction
                            {
                                Type = MechanicActionType.AddKeyword,
                                Keyword = Keyword.Reborn,
                                SourceId = "combat_reborn_self"
                            }
                        }
                    }
                },
                CreateSummonEffect("deathrattle_summon_token_1"),
                CreateSummonEffect("summon_token_microbot"),
                CreateSummonEffect("deathrattle_summon_beetle_2_2", "beetle", 2, 2, Tribe.Beast),
                new MinionEffectDefinition
                {
                    Id = "card_bought_buff_shop_elemental_1_1",
                    Triggers = { new EffectTrigger { EventType = MechanicEventType.CardBought } },
                    Actions =
                    {
                        new TargetedMechanicAction
                        {
                            Target = new EffectTarget { Type = EffectTargetType.AllFriendlyShop, Tribe = Tribe.Elemental },
                            Action = new MechanicAction
                            {
                                Type = MechanicActionType.BuffStats,
                                Attack = 1,
                                Health = 1,
                                SourceId = "card_bought_buff_shop_elemental_1_1"
                            }
                        }
                    }
                },
                new MinionEffectDefinition
                {
                    Id = MinionEffectIds.AlwaysGoldenNoTripleReward
                }
            });
        }

        public MinionEffectDefinition Get(string id)
        {
            if (!effects.TryGetValue(id, out var effect))
            {
                throw new InvalidOperationException("Unknown effect id: " + id);
            }

            return effect;
        }

        private static MinionEffectDefinition CreateBuffEffect(string id, MechanicEventType eventType, EffectTargetType targetType, int attack, int health, Tribe tribe = Tribe.All)
        {
            return new MinionEffectDefinition
            {
                Id = id,
                Triggers = { new EffectTrigger { EventType = eventType } },
                Actions =
                {
                    new TargetedMechanicAction
                    {
                        Target = new EffectTarget { Type = targetType, Tribe = tribe },
                        Action = new MechanicAction
                        {
                            Type = MechanicActionType.BuffStats,
                            Attack = attack,
                            Health = health,
                            SourceId = id
                        }
                    }
                }
            };
        }

        private static MinionEffectDefinition CreateSummonEffect(string id, string tokenDefinitionId = "microbot", int attack = 1, int health = 1, Tribe tribe = Tribe.Mech)
        {
            return new MinionEffectDefinition
            {
                Id = id,
                Triggers = { new EffectTrigger { EventType = MechanicEventType.MinionDied } },
                Actions =
                {
                    new TargetedMechanicAction
                    {
                        Target = new EffectTarget { Type = EffectTargetType.FriendlyBoard },
                        Action = new MechanicAction
                        {
                            Type = MechanicActionType.SummonToken,
                            TokenDefinitionId = tokenDefinitionId,
                            Attack = attack,
                            Health = health,
                            Tribe = tribe,
                            SourceId = id
                        }
                    }
                }
            };
        }
    }
}
