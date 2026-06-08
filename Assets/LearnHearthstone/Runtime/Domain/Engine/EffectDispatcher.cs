using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Domain.Engine
{
    public sealed class EffectDispatchContext
    {
        public MechanicEventType EventType;
        public MinionInstance Source;
        public TavernState Tavern;
        public List<MinionInstance> FriendlyBoard = new List<MinionInstance>();
        public List<MinionInstance> FriendlyHand = new List<MinionInstance>();
        public List<MinionInstance> FriendlyShop = new List<MinionInstance>();
    }

    public sealed class EffectDispatcher
    {
        private readonly MinionEffectCatalog catalog;
        private readonly SeededRng rng;

        public EffectDispatcher(MinionEffectCatalog catalog, SeededRng rng)
        {
            this.catalog = catalog;
            this.rng = rng;
        }

        public void Dispatch(EffectDispatchContext context)
        {
            if (context.Source == null || context.Source.EffectIds == null)
            {
                return;
            }

            foreach (var effectId in context.Source.EffectIds)
            {
                var effect = catalog.Get(effectId);
                if (!effect.Triggers.Any(trigger => trigger.EventType == context.EventType))
                {
                    continue;
                }

                foreach (var action in effect.Actions)
                {
                    if (action.Action.Type == MechanicActionType.GainGold ||
                        action.Action.Type == MechanicActionType.ModifyShopGrowth ||
                        action.Action.Type == MechanicActionType.ModifyGeneratedCardBuff)
                    {
                        MechanicEngine.ApplyToTavern(context.Tavern, action.Action);
                        continue;
                    }

                    if (action.Action.Type == MechanicActionType.SummonToken)
                    {
                        SummonToken(context, action.Action);
                        continue;
                    }

                    foreach (var target in ResolveTargets(context, action.Target))
                    {
                        MechanicEngine.ApplyToMinion(target, action.Action);
                    }
                }
            }
        }

        private IEnumerable<MinionInstance> ResolveTargets(EffectDispatchContext context, EffectTarget target)
        {
            switch (target.Type)
            {
                case EffectTargetType.Source:
                    return context.Source == null ? Enumerable.Empty<MinionInstance>() : new[] { context.Source };
                case EffectTargetType.FriendlyBoard:
                    return context.FriendlyBoard.Where(minion => MatchesTribe(minion, target.Tribe)).Take(target.Count);
                case EffectTargetType.FriendlyHand:
                    return context.FriendlyHand.Where(minion => MatchesTribe(minion, target.Tribe)).Take(target.Count);
                case EffectTargetType.FriendlyShop:
                    return context.FriendlyShop.Where(minion => minion.CardKind == CardKind.Minion && MatchesTribe(minion, target.Tribe)).Take(target.Count);
                case EffectTargetType.RandomFriendlyBoard:
                    return PickRandom(context.FriendlyBoard.Where(minion => minion != context.Source && MatchesTribe(minion, target.Tribe)).ToList(), target.Count);
                case EffectTargetType.AdjacentFriendlyBoard:
                    return AdjacentFriendlyBoard(context, target.Tribe);
                case EffectTargetType.AllFriendlyBoard:
                    return context.FriendlyBoard.Where(minion => MatchesTribe(minion, target.Tribe));
                case EffectTargetType.AllFriendlyHand:
                    return context.FriendlyHand.Where(minion => MatchesTribe(minion, target.Tribe));
                case EffectTargetType.AllFriendlyShop:
                    return context.FriendlyShop.Where(minion => minion.CardKind == CardKind.Minion && MatchesTribe(minion, target.Tribe));
                default:
                    return Enumerable.Empty<MinionInstance>();
            }
        }

        private IEnumerable<MinionInstance> PickRandom(List<MinionInstance> candidates, int count)
        {
            var selected = new List<MinionInstance>();
            while (selected.Count < count && candidates.Count > 0)
            {
                var index = rng.NextInt(candidates.Count);
                selected.Add(candidates[index]);
                candidates.RemoveAt(index);
            }

            return selected;
        }

        private static IEnumerable<MinionInstance> AdjacentFriendlyBoard(EffectDispatchContext context, Tribe tribe)
        {
            var sourceIndex = context.FriendlyBoard.IndexOf(context.Source);
            if (sourceIndex < 0)
            {
                return Enumerable.Empty<MinionInstance>();
            }

            var result = new List<MinionInstance>();
            if (sourceIndex > 0 && MatchesTribe(context.FriendlyBoard[sourceIndex - 1], tribe))
            {
                result.Add(context.FriendlyBoard[sourceIndex - 1]);
            }

            if (sourceIndex + 1 < context.FriendlyBoard.Count && MatchesTribe(context.FriendlyBoard[sourceIndex + 1], tribe))
            {
                result.Add(context.FriendlyBoard[sourceIndex + 1]);
            }

            return result;
        }

        private static bool MatchesTribe(MinionInstance minion, Tribe tribe)
        {
            return tribe == Tribe.All || minion.Tribes.Contains(tribe) || minion.Tribes.Contains(Tribe.All);
        }

        private static void SummonToken(EffectDispatchContext context, MechanicAction action)
        {
            if (context.FriendlyBoard == null || context.FriendlyBoard.Count >= 7)
            {
                return;
            }

            context.FriendlyBoard.Add(new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = "token-" + action.TokenDefinitionId + "-" + context.FriendlyBoard.Count,
                DefinitionId = action.TokenDefinitionId,
                CardId = action.TokenDefinitionId.ToUpperInvariant(),
                Name = action.TokenDefinitionId,
                BaseAttack = 1,
                BaseHealth = 1,
                Attack = 1,
                Health = 1,
                MaxHealth = 1,
                Tribes = new List<Tribe> { Tribe.Mech },
                Keywords = new List<Keyword>(),
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                PoolSource = PoolSource.Summon,
                PoolCopiesHeld = 0
            });
        }
    }
}
