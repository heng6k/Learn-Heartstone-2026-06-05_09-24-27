using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Domain.Engine
{
    public enum DarkGiftResolutionPhase
    {
        Acquire,
        Trigger
    }

    public delegate DarkGiftResolution DarkGiftResolver(DarkGiftResolutionContext context);

    public sealed class DarkGiftResolutionContext
    {
        public DarkGiftResolutionContext(
            DarkGiftResolutionPhase phase,
            int round,
            MechanicEventType eventType,
            DarkGiftDefinition definition,
            PlayerDarkGiftInstance instance,
            MinionInstance target,
            string requestId)
        {
            Phase = phase;
            Round = round;
            EventType = eventType;
            Definition = definition?.Clone();
            Instance = instance?.Clone();
            Target = target?.Clone();
            RequestId = requestId;
        }

        public DarkGiftResolutionPhase Phase { get; }
        public int Round { get; }
        public MechanicEventType EventType { get; }
        public DarkGiftDefinition Definition { get; }
        public PlayerDarkGiftInstance Instance { get; }
        public MinionInstance Target { get; }
        public string RequestId { get; }
    }

    public sealed class DarkGiftResolution
    {
        private DarkGiftResolution(
            bool succeeded,
            string code,
            string message,
            string result,
            Action<MatchState, MinionInstance> commit)
        {
            Succeeded = succeeded;
            Code = code;
            Message = message;
            Result = result;
            Commit = commit;
        }

        public bool Succeeded { get; }
        public string Code { get; }
        public string Message { get; }
        public string Result { get; }
        public Action<MatchState, MinionInstance> Commit { get; }

        public static DarkGiftResolution Success(
            string result = null,
            Action<MatchState, MinionInstance> commit = null)
        {
            return new DarkGiftResolution(true, "dark-gift.resolved", string.Empty, result, commit);
        }

        public static DarkGiftResolution Failure(string code, string message)
        {
            return new DarkGiftResolution(false, code, message, null, null);
        }
    }

    public sealed class DarkGiftResolverRegistry
    {
        private readonly Dictionary<string, DarkGiftResolver> resolvers =
            new Dictionary<string, DarkGiftResolver>(StringComparer.Ordinal);

        public void Register(string effectRevision, DarkGiftResolver resolver)
        {
            if (string.IsNullOrWhiteSpace(effectRevision))
            {
                throw new ArgumentException("Effect revision is required.", nameof(effectRevision));
            }
            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            resolvers[effectRevision] = resolver;
        }

        public void RegisterIfMissing(string effectRevision, DarkGiftResolver resolver)
        {
            if (string.IsNullOrWhiteSpace(effectRevision))
            {
                throw new ArgumentException("Effect revision is required.", nameof(effectRevision));
            }
            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            if (!resolvers.ContainsKey(effectRevision))
            {
                resolvers.Add(effectRevision, resolver);
            }
        }

        public bool TryGet(string effectRevision, out DarkGiftResolver resolver)
        {
            resolver = null;
            return !string.IsNullOrWhiteSpace(effectRevision) &&
                   resolvers.TryGetValue(effectRevision, out resolver);
        }
    }

    public static class Season14DarkGiftResolvers
    {
        public const string SunkenPersistenceRevision = "dark-gift-effect-dg-r01@preview-v1";
        public const string SunkenPersistenceMarker = "dark-gift.dg-r01";
        public const string HarpysTalonsRevision = "dark-gift-effect-dg-r02@preview-v1";
        public const string FortitudeRevision = "dark-gift-effect-dg-r04@preview-v1";
        public const string SharpenedSwordRevision = "dark-gift-effect-dg-r06@preview-v1";
        public const string ToughenedShieldRevision = "dark-gift-effect-dg-r07@preview-v1";
        public const string SteadyGrowthRevision = "dark-gift-effect-dg-r08@preview-v1";
        public const string TimeTurningRevision = "dark-gift-effect-dg-r09@preview-v1";
        public const string AffinityRevision = "dark-gift-effect-dg-r05@preview-v1";
        public const string ReplicationRevision = "dark-gift-effect-dg-r13@preview-v1";
        public const string ConsanguinityRevision = "dark-gift-effect-dg-r11@preview-v1";
        public const string FreshPerspectiveRevision = "dark-gift-effect-dg-r12@preview-v1";
        public const string MysticEssenceRevision = "dark-gift-effect-dg-r23@preview-v1";
        public const string DemonologyRevision = "dark-gift-effect-dg-r21@preview-v1";
        public const string PolarizationRevision = "dark-gift-effect-dg-r22@preview-v1";
        public const string EchoingVoiceRevision = "dark-gift-effect-dg-r27@preview-v1";
        public const string OffensiveSacrificeRevision = "dark-gift-effect-dg-r28@preview-v1";
        public const string DefensiveSacrificeRevision = "dark-gift-effect-dg-r29@preview-v1";
        public const string CharismaRevision = "dark-gift-effect-dg-r36@preview-v1";
        public const string GolemancyRevision = "dark-gift-effect-dg-r40@preview-v1";
        public const string JawsOfDeathRevision = "dark-gift-effect-dg-r03@preview-v1";
        public const string TranscendenceRevision = "dark-gift-effect-dg-r30@preview-v1";
        public const string AdmirationRevision = "dark-gift-effect-dg-r34@preview-v1";
        public const string ResistanceRevision = "dark-gift-effect-dg-r37@preview-v1";
        public const string HostilityRevision = "dark-gift-effect-dg-r38@preview-v1";
        public const string TorethsBlessingRevision = "dark-gift-effect-dg-r19@preview-v1";
        public const string TarecgosasBlessingRevision = "dark-gift-effect-dg-r24@preview-v1";
        public const string PersistingHorrorRevision = "dark-gift-effect-dg-r41@preview-v1";
        public const string InvulnerabilityRevision = "dark-gift-effect-dg-r43@preview-v1";
        public const string FurtivenessRevision = "dark-gift-effect-dg-r10@preview-v1";
        public const string BattleScarsLowRevision = "dark-gift-effect-dg-r14@preview-v1";
        public const string DeathsEmbraceLowRevision = "dark-gift-effect-dg-r15@preview-v1";
        public const string SpellSiphonLowRevision = "dark-gift-effect-dg-r16@preview-v1";
        public const string GildingRevision = "dark-gift-effect-dg-r17@preview-v1";
        public const string DoubleVisionRevision = "dark-gift-effect-dg-r18@preview-v1";
        public const string AmalgamationRevision = "dark-gift-effect-dg-r20@preview-v1";
        public const string DexterityLowRevision = "dark-gift-effect-dg-r25@preview-v1";
        public const string IncubationRevision = "dark-gift-effect-dg-r26@preview-v1";
        public const string BattleScarsHighRevision = "dark-gift-effect-dg-r31@preview-v1";
        public const string DeathsEmbraceHighRevision = "dark-gift-effect-dg-r32@preview-v1";
        public const string SpellSiphonHighRevision = "dark-gift-effect-dg-r33@preview-v1";
        public const string ToxicityRevision = "dark-gift-effect-dg-r35@preview-v1";
        public const string DexterityHighRevision = "dark-gift-effect-dg-r39@preview-v1";
        public const string TitanicStrengthRevision = "dark-gift-effect-dg-r42@preview-v1";

        public static void RegisterDefaults(DarkGiftResolverRegistry registry, MinionCatalog minions = null)
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry));
            }

            registry.RegisterIfMissing(SunkenPersistenceRevision, context => AttachMarker(context, SunkenPersistenceMarker));
            registry.RegisterIfMissing(HarpysTalonsRevision, context => AddKeywords(
                context,
                "dark-gift.dg-r02",
                Keyword.DivineShield,
                Keyword.Windfury));
            registry.RegisterIfMissing(FortitudeRevision, context => BuffStats(context, "dark-gift.dg-r04", 4, 4));
            registry.RegisterIfMissing(SharpenedSwordRevision, context => ResolveCardPlayedBuff(context, "dark-gift.dg-r06", 2, 0));
            registry.RegisterIfMissing(ToughenedShieldRevision, context => ResolveCardPlayedBuff(context, "dark-gift.dg-r07", 0, 2));
            registry.RegisterIfMissing(SteadyGrowthRevision, ResolveSteadyGrowth);
            registry.RegisterIfMissing(TimeTurningRevision, context => AttachTriggeredMarker(context, "dark-gift.dg-r09"));
            registry.RegisterIfMissing(AffinityRevision, context => ResolveAffinity(context, minions));
            registry.RegisterIfMissing(ReplicationRevision, context => ResolveReplication(context, minions));
            registry.RegisterIfMissing(ConsanguinityRevision, context => AttachTriggeredKeyword(context, "dark-gift.dg-r11", Keyword.Rally));
            registry.RegisterIfMissing(FreshPerspectiveRevision, context => AttachTriggeredKeyword(context, "dark-gift.dg-r12", Keyword.Deathrattle));
            registry.RegisterIfMissing(DemonologyRevision, context => AttachTriggeredKeyword(context, "dark-gift.dg-r21", Keyword.Rally));
            registry.RegisterIfMissing(PolarizationRevision, context => AttachTriggeredMarker(context, "dark-gift.dg-r22"));
            registry.RegisterIfMissing(MysticEssenceRevision, context => AttachTriggeredKeyword(context, "dark-gift.dg-r23", Keyword.Deathrattle));
            registry.RegisterIfMissing(EchoingVoiceRevision, context => AttachTriggeredMarker(context, "dark-gift.dg-r27"));
            registry.RegisterIfMissing(OffensiveSacrificeRevision, context => AttachTriggeredKeyword(context, "dark-gift.dg-r28", Keyword.Deathrattle));
            registry.RegisterIfMissing(DefensiveSacrificeRevision, context => AttachTriggeredKeyword(context, "dark-gift.dg-r29", Keyword.Deathrattle));
            registry.RegisterIfMissing(CharismaRevision, context => AttachTriggeredKeyword(context, "dark-gift.dg-r36", Keyword.Rally));
            registry.RegisterIfMissing(GolemancyRevision, context => AttachTriggeredKeyword(context, "dark-gift.dg-r40", Keyword.Deathrattle));
            registry.RegisterIfMissing(JawsOfDeathRevision, context => AttachTriggeredMarker(context, "dark-gift.dg-r03"));
            registry.RegisterIfMissing(TranscendenceRevision, context => AttachTriggeredMarker(context, "dark-gift.dg-r30"));
            registry.RegisterIfMissing(AdmirationRevision, context => AttachTriggeredMarker(context, "dark-gift.dg-r34"));
            registry.RegisterIfMissing(ResistanceRevision, context => AttachTriggeredMarker(context, "dark-gift.dg-r37"));
            registry.RegisterIfMissing(HostilityRevision, context => AttachTriggeredMarker(context, "dark-gift.dg-r38"));
            registry.RegisterIfMissing(TorethsBlessingRevision, context => AttachMarker(context, "dark-gift.dg-r19"));
            registry.RegisterIfMissing(TarecgosasBlessingRevision, context => AttachMarker(context, "dark-gift.dg-r24"));
            registry.RegisterIfMissing(PersistingHorrorRevision, context => AddKeywords(context, "dark-gift.dg-r41", Keyword.Reborn));
            registry.RegisterIfMissing(InvulnerabilityRevision, context => AttachMarker(context, "dark-gift.dg-r43"));
            registry.RegisterIfMissing(FurtivenessRevision, context => AddKeywords(context, "dark-gift.dg-r10", Keyword.Stealth));
            registry.RegisterIfMissing(BattleScarsLowRevision, context => ResolveHistoricalBuff(context, "dark-gift.dg-r14", tavern => tavern.BattlecriesTriggeredThisGame, 2));
            registry.RegisterIfMissing(DeathsEmbraceLowRevision, context => ResolveHistoricalBuff(context, "dark-gift.dg-r15", tavern => tavern.DeathrattlesTriggeredThisGame, 1));
            registry.RegisterIfMissing(SpellSiphonLowRevision, context => ResolveHistoricalBuff(context, "dark-gift.dg-r16", tavern => tavern.TavernSpellsCastThisGame, 2));
            registry.RegisterIfMissing(GildingRevision, context => ResolveGilding(context, minions));
            registry.RegisterIfMissing(DoubleVisionRevision, ResolveDoubleVision);
            registry.RegisterIfMissing(AmalgamationRevision, ApplyAllMinionTypes);
            registry.RegisterIfMissing(DexterityLowRevision, context => ResolveCardPlayedBuff(context, "dark-gift.dg-r25", 2, 2));
            registry.RegisterIfMissing(IncubationRevision, ResolveIncubation);
            registry.RegisterIfMissing(BattleScarsHighRevision, context => ResolveHistoricalBuff(context, "dark-gift.dg-r31", tavern => tavern.BattlecriesTriggeredThisGame, 3));
            registry.RegisterIfMissing(DeathsEmbraceHighRevision, context => ResolveHistoricalBuff(context, "dark-gift.dg-r32", tavern => tavern.DeathrattlesTriggeredThisGame, 2));
            registry.RegisterIfMissing(SpellSiphonHighRevision, context => ResolveHistoricalBuff(context, "dark-gift.dg-r33", tavern => tavern.TavernSpellsCastThisGame, 3));
            registry.RegisterIfMissing(ToxicityRevision, context => AddKeywords(context, "dark-gift.dg-r35", Keyword.Venomous));
            registry.RegisterIfMissing(DexterityHighRevision, context => ResolveCardPlayedBuff(context, "dark-gift.dg-r39", 4, 4));
            registry.RegisterIfMissing(TitanicStrengthRevision, context => BuffStats(context, "dark-gift.dg-r42", 1000, 0));
        }

        private static DarkGiftResolution AddKeywords(
            DarkGiftResolutionContext context,
            string effectId,
            params Keyword[] keywords)
        {
            if (context?.Phase != DarkGiftResolutionPhase.Acquire)
            {
                return DarkGiftResolution.Failure("dark-gift.phase.invalid", "This Dark Gift resolves when acquired.");
            }

            return DarkGiftResolution.Success(effectId, (state, target) =>
            {
                foreach (var keyword in keywords ?? Array.Empty<Keyword>())
                {
                    MechanicEngine.ApplyToMinion(target, new MechanicAction
                    {
                        Type = MechanicActionType.AddKeyword,
                        Keyword = keyword,
                        SourceId = context.Definition.RevisionId
                    });
                }

                AddEffectMarker(target, effectId);
            });
        }

        private static DarkGiftResolution BuffStats(
            DarkGiftResolutionContext context,
            string effectId,
            int attack,
            int health)
        {
            if (context?.Phase != DarkGiftResolutionPhase.Acquire)
            {
                return DarkGiftResolution.Failure("dark-gift.phase.invalid", "This Dark Gift resolves when acquired.");
            }

            return DarkGiftResolution.Success(effectId, (state, target) =>
            {
                MechanicEngine.ApplyToMinion(target, new MechanicAction
                {
                    Type = MechanicActionType.BuffStats,
                    Scope = BuffScope.Instance,
                    Attack = attack,
                    Health = health,
                    EnchantmentKind = EnchantmentKind.StatBuff,
                    SourceId = context.Definition.RevisionId
                });
                AddEffectMarker(target, effectId);
            });
        }

        private static DarkGiftResolution ApplyAllMinionTypes(DarkGiftResolutionContext context)
        {
            if (context?.Phase != DarkGiftResolutionPhase.Acquire)
            {
                return DarkGiftResolution.Failure("dark-gift.phase.invalid", "This Dark Gift resolves when acquired.");
            }

            return DarkGiftResolution.Success("dark-gift.dg-r20", (state, target) =>
            {
                target.Tribes = target.Tribes ?? new List<Tribe>();
                target.Tribes.Clear();
                target.Tribes.Add(Tribe.All);
                AddEffectMarker(target, "dark-gift.dg-r20");
            });
        }

        private static DarkGiftResolution ResolveCardPlayedBuff(
            DarkGiftResolutionContext context,
            string effectId,
            int attack,
            int health)
        {
            if (context == null)
            {
                return DarkGiftResolution.Failure("dark-gift.context.missing", "Dark Gift context is required.");
            }
            if (context.Phase == DarkGiftResolutionPhase.Acquire)
            {
                return DarkGiftResolution.Success(effectId, (state, target) => AddEffectMarker(target, effectId));
            }
            if (context.EventType != MechanicEventType.CardPlayed)
            {
                return DarkGiftResolution.Failure("dark-gift.event.invalid", "This Dark Gift triggers when a card is played.");
            }

            return DarkGiftResolution.Success(effectId, (state, target) =>
            {
                MechanicEngine.ApplyToMinion(target, new MechanicAction
                {
                    Type = MechanicActionType.BuffStats,
                    Scope = BuffScope.Instance,
                    Attack = attack,
                    Health = health,
                    EnchantmentKind = EnchantmentKind.StatBuff,
                    SourceId = context.Definition.RevisionId
                });
            });
        }

        private static DarkGiftResolution ResolveSteadyGrowth(DarkGiftResolutionContext context)
        {
            const string effectId = "dark-gift.dg-r08";
            if (context == null)
            {
                return DarkGiftResolution.Failure("dark-gift.context.missing", "Dark Gift context is required.");
            }
            if (context.Phase == DarkGiftResolutionPhase.Acquire)
            {
                return DarkGiftResolution.Success(effectId, (state, target) => AddEffectMarker(target, effectId));
            }
            if (context.EventType != MechanicEventType.TurnEnded)
            {
                return DarkGiftResolution.Failure("dark-gift.event.invalid", "Steady Growth triggers at the end of the turn.");
            }

            var acquiredRound = Math.Max(3, context.Instance?.AcquiredRound ?? 3);
            var attack = Math.Min(4, acquiredRound - 2);
            var health = acquiredRound == 3 ? 2 : attack;
            return DarkGiftResolution.Success(effectId, (state, target) =>
            {
                MechanicEngine.ApplyToMinion(target, new MechanicAction
                {
                    Type = MechanicActionType.BuffStats,
                    Scope = BuffScope.Instance,
                    Attack = attack,
                    Health = health,
                    EnchantmentKind = EnchantmentKind.StatBuff,
                    SourceId = context.Definition.RevisionId
                });
            });
        }

        private static DarkGiftResolution ResolveHistoricalBuff(
            DarkGiftResolutionContext context,
            string effectId,
            Func<TavernState, int> counter,
            int statsPerTrigger)
        {
            if (context?.Phase != DarkGiftResolutionPhase.Acquire)
            {
                return DarkGiftResolution.Failure("dark-gift.phase.invalid", "This Dark Gift resolves when acquired.");
            }

            return DarkGiftResolution.Success(effectId, (state, target) =>
            {
                var tavern = state?.Player?.Tavern;
                var count = tavern == null ? 0 : Math.Max(0, counter(tavern));
                var amount = StatMath.SaturatingMultiply(count, statsPerTrigger, 0, StatMath.MaxStat);
                if (amount > 0)
                {
                    MechanicEngine.ApplyToMinion(target, new MechanicAction
                    {
                        Type = MechanicActionType.BuffStats,
                        Scope = BuffScope.Instance,
                        Attack = amount,
                        Health = amount,
                        EnchantmentKind = EnchantmentKind.StatBuff,
                        SourceId = context.Definition.RevisionId
                    });
                }
                AddEffectMarker(target, effectId);
            });
        }

        private static DarkGiftResolution ResolveIncubation(DarkGiftResolutionContext context)
        {
            const string effectId = "dark-gift.dg-r26";
            if (context == null)
            {
                return DarkGiftResolution.Failure("dark-gift.context.missing", "Dark Gift context is required.");
            }
            if (context.Phase == DarkGiftResolutionPhase.Acquire)
            {
                return DarkGiftResolution.Success(effectId, (state, target) =>
                {
                    MechanicEngine.ApplyToMinion(target, new MechanicAction
                    {
                        Type = MechanicActionType.BuffStats,
                        Scope = BuffScope.Instance,
                        Attack = 2,
                        Health = 2,
                        EnchantmentKind = EnchantmentKind.StatBuff,
                        SourceId = context.Definition.RevisionId
                    });
                    AddEffectMarker(target, effectId);
                });
            }
            if (context.EventType != MechanicEventType.TurnStarted)
            {
                return DarkGiftResolution.Failure("dark-gift.event.invalid", "Incubation resolves at the start of its scheduled turn.");
            }

            return DarkGiftResolution.Success(effectId, (state, target) => StatMath.DoubleCurrentStats(target, false));
        }

        private static DarkGiftResolution ResolveDoubleVision(DarkGiftResolutionContext context)
        {
            const string effectId = "dark-gift.dg-r18";
            if (context?.Phase != DarkGiftResolutionPhase.Acquire)
            {
                return DarkGiftResolution.Failure("dark-gift.phase.invalid", "Double Vision resolves when acquired.");
            }

            return DarkGiftResolution.Success(effectId, (state, target) =>
            {
                var tavern = state?.Player?.Tavern;
                if (tavern?.Hand != null && tavern.Hand.Count < 10)
                {
                    var copy = target.Clone();
                    copy.InstanceId = "dark-gift-double-vision-" + target.InstanceId + "-" + state.Round + "-" + tavern.Hand.Count;
                    copy.Owner = BoardSide.Player;
                    copy.PoolSource = PoolSource.Copy;
                    copy.OriginPoolSource = PoolSource.Copy;
                    copy.PoolCopiesHeld = 0;
                    copy.CanReturnToPoolAfterAttach = false;
                    copy.AttacksThisCombat = 0;
                    copy.Tags = copy.Tags ?? new List<string>();
                    if (!copy.Tags.Contains("generated_copy"))
                    {
                        copy.Tags.Add("generated_copy");
                    }
                    tavern.Hand.Add(copy);
                }

                AddEffectMarker(target, effectId);
            });
        }

        private static DarkGiftResolution ResolveGilding(
            DarkGiftResolutionContext context,
            MinionCatalog minions)
        {
            const string effectId = "dark-gift.dg-r17";
            const string tripleRewardGrantedCounter = "triple-reward-granted";
            if (context?.Phase != DarkGiftResolutionPhase.Acquire)
            {
                return DarkGiftResolution.Failure("dark-gift.phase.invalid", "Gilding resolves when acquired.");
            }

            return DarkGiftResolution.Success(effectId, (state, target) =>
            {
                GoldenMinionTransformer.MakeGoldenInPlace(target, minions);

                target.Counters = target.Counters ?? new Dictionary<string, int>();
                target.Counters[tripleRewardGrantedCounter] = 1;
                AddEffectMarker(target, effectId);
            });
        }

        private static void AddEffectMarker(MinionInstance target, string effectId)
        {
            target.Tags = target.Tags ?? new List<string>();
            if (!string.IsNullOrWhiteSpace(effectId) && !target.Tags.Contains(effectId))
            {
                target.Tags.Add(effectId);
            }
        }

        private static DarkGiftResolution ResolveAffinity(DarkGiftResolutionContext context, MinionCatalog minions)
        {
            const string effectId = "dark-gift.dg-r05";
            if (context.Phase == DarkGiftResolutionPhase.Acquire)
            {
                return AttachMarker(context, effectId);
            }

            return DarkGiftResolution.Success(effectId, (state, target) =>
            {
                var tavern = state.Player.Tavern;
                if (minions == null || tavern.Hand.Count >= 10)
                {
                    return;
                }

                var targetTribes = (target.Tribes ?? new List<Tribe>())
                    .Where(tribe => tribe != Tribe.None && tribe != Tribe.All)
                    .Distinct()
                    .ToList();
                var enabled = state.EnabledMinionCardIds ?? new List<string>();
                var candidates = minions.All
                    .Where(definition =>
                        definition != null &&
                        definition.InPool &&
                        definition.TavernTier <= Math.Max(1, tavern.Tier) &&
                        (enabled.Count == 0 || enabled.Contains(definition.CardId)) &&
                        definition.Tribes != null &&
                        definition.Tribes.Any(tribe => tribe == Tribe.All || targetTribes.Contains(tribe)))
                    .OrderBy(definition => definition.CardId, StringComparer.Ordinal)
                    .ToList();
                if (candidates.Count == 0)
                {
                    return;
                }

                var rng = new SeededRng(state.Seed + state.Round * 4051 + tavern.Hand.Count);
                var definition = rng.Pick(candidates);
                var generated = MinionFactory.Create(
                    definition,
                    BoardSide.Player,
                    "dark-gift-r05-" + state.Round + "-" + tavern.Hand.Count,
                    false,
                    PoolSource.Copy,
                    0);
                generated.CanReturnToPoolAfterAttach = false;
                AddEffectMarker(generated, "generated_copy");
                tavern.Hand.Add(generated);
            });
        }

        private static DarkGiftResolution ResolveReplication(DarkGiftResolutionContext context, MinionCatalog minions)
        {
            const string effectId = "dark-gift.dg-r13";
            if (context.Phase == DarkGiftResolutionPhase.Acquire)
            {
                return AttachMarker(context, effectId);
            }

            return DarkGiftResolution.Success(effectId, (state, target) =>
            {
                var tavern = state.Player.Tavern;
                if (tavern.Hand.Count >= 10)
                {
                    return;
                }

                MinionInstance copy;
                if (minions != null &&
                    (minions.TryGetById(target.DefinitionId, out var definition) ||
                     minions.TryGetByCardId(target.CardId, out definition)))
                {
                    copy = MinionFactory.Create(
                        definition,
                        BoardSide.Player,
                        "dark-gift-r13-" + state.Round + "-" + tavern.Hand.Count,
                        false,
                        PoolSource.Copy,
                        0);
                }
                else
                {
                    copy = target.Clone();
                    copy.InstanceId = "player-dark-gift-r13-" + state.Round + "-" + tavern.Hand.Count;
                    copy.Golden = false;
                    copy.Attack = Math.Max(0, copy.BaseAttack);
                    copy.Health = Math.Max(1, copy.BaseHealth);
                    copy.MaxHealth = copy.Health;
                    copy.Keywords = copy.OfficialKeywords != null && copy.OfficialKeywords.Count > 0
                        ? new List<Keyword>(copy.OfficialKeywords)
                        : new List<Keyword>(copy.Keywords ?? new List<Keyword>());
                    copy.Enchantments = new List<Enchantment>();
                    copy.Counters = new Dictionary<string, int>();
                    copy.Tags = (copy.Tags ?? new List<string>())
                        .Where(tag => !tag.StartsWith("dark-gift.", StringComparison.Ordinal))
                        .ToList();
                    copy.PoolSource = PoolSource.Copy;
                    copy.OriginPoolSource = PoolSource.Copy;
                    copy.PoolCopiesHeld = 0;
                    copy.CanReturnToPoolAfterAttach = false;
                }

                AddEffectMarker(copy, "generated_copy");
                AddEffectMarker(copy, "plain_copy");
                tavern.Hand.Add(copy);
            });
        }

        private static DarkGiftResolution AttachMarker(DarkGiftResolutionContext context, string effectId)
        {
            return DarkGiftResolution.Success(effectId, (state, target) => AddEffectMarker(target, effectId));
        }

        private static DarkGiftResolution AttachTriggeredMarker(DarkGiftResolutionContext context, string effectId)
        {
            return context.Phase == DarkGiftResolutionPhase.Acquire
                ? AttachMarker(context, effectId)
                : DarkGiftResolution.Success(effectId);
        }

        private static DarkGiftResolution AttachTriggeredKeyword(
            DarkGiftResolutionContext context,
            string effectId,
            Keyword keyword)
        {
            return context.Phase == DarkGiftResolutionPhase.Acquire
                ? AddKeywords(context, effectId, keyword)
                : DarkGiftResolution.Success(effectId);
        }
    }

    public static class DarkGiftStateMachine
    {
        private const string AdvanceCounterPrefix = "dark-gift.advance:";

        public static DarkGiftStateMachineResult Acquire(
            MatchState state,
            MinionInstance target,
            DarkGiftDefinition definition,
            string source,
            string requestId,
            DarkGiftResolverRegistry registry)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            Normalize(state);
            var liveTarget = FindPlayerMinion(state, target?.InstanceId);
            if (liveTarget == null)
            {
                return Failure(state, target?.InstanceId, source, requestId, "dark-gift.target.missing", "Dark Gift target is no longer available.");
            }
            if (definition == null ||
                string.IsNullOrWhiteSpace(definition.RevisionId) ||
                string.IsNullOrWhiteSpace(definition.EffectRevision))
            {
                return Failure(state, liveTarget.InstanceId, source, requestId, "dark-gift.definition.invalid", "Dark Gift definition is invalid.");
            }

            var alreadyApplied = FindEvent(state.PlayerDarkGifts.TriggerHistory, requestId, "dark-gift.applied");
            if (alreadyApplied != null)
            {
                return Success(
                    "dark-gift.acquire.already-applied",
                    "Dark Gift request was already applied.",
                    FindInstance(state, liveTarget.InstanceId, definition.RevisionId));
            }

            var existing = FindInstance(state, liveTarget.InstanceId, definition.RevisionId);
            var activeOnTarget = state.PlayerDarkGifts.AcquiredGiftInstances
                .Where(item => item != null &&
                               item.Active &&
                               !item.Expired &&
                               string.Equals(item.InstanceId, liveTarget.InstanceId, StringComparison.Ordinal))
                .ToList();
            var stackPolicy = definition.StackPolicy ?? DarkGiftStackPolicies.Reject;
            if (existing != null && existing.Active && !existing.Expired)
            {
                if (!string.Equals(stackPolicy, DarkGiftStackPolicies.Stack, StringComparison.OrdinalIgnoreCase))
                {
                    return Failure(state, liveTarget.InstanceId, source, requestId, "dark-gift.duplicate", "Dark Gift is already active on this minion.");
                }
                if (definition.MaxStacks > 0 && existing.StackCount >= definition.MaxStacks)
                {
                    return Failure(state, liveTarget.InstanceId, source, requestId, "dark-gift.stack-limit", "Dark Gift has reached its stack limit.");
                }
            }
            if (registry == null || !registry.TryGet(definition.EffectRevision, out var resolver))
            {
                return Failure(state, liveTarget.InstanceId, source, requestId, "dark-gift.resolver.not-found", "Dark Gift resolver is not registered: " + definition.EffectRevision);
            }

            var prospective = existing?.Clone() ?? new PlayerDarkGiftInstance
            {
                InstanceId = liveTarget.InstanceId,
                DefinitionRevisionId = definition.RevisionId,
                AcquiredRound = Math.Max(1, state.Round),
                Source = source,
                StackCount = 1,
                RemainingUses = Math.Max(0, definition.InitialUses),
                Cooldown = 0,
                NextTriggerRound = Math.Max(1, state.Round) + Math.Max(0, definition.TriggerDelayRounds),
                Active = true
            };
            if (existing != null)
            {
                prospective.StackCount = Math.Max(1, existing.StackCount) + 1;
            }

            var resolution = Resolve(
                resolver,
                new DarkGiftResolutionContext(
                    DarkGiftResolutionPhase.Acquire,
                    state.Round,
                    default,
                    definition,
                    prospective,
                    liveTarget,
                    requestId));
            if (resolution == null || !resolution.Succeeded)
            {
                return Failure(
                    state,
                    liveTarget.InstanceId,
                    source,
                    requestId,
                    resolution?.Code ?? "dark-gift.resolver.failed",
                    resolution?.Message ?? "Dark Gift resolver did not return a result.");
            }

            resolution.Commit?.Invoke(state, liveTarget);
            if (existing != null)
            {
                CopyState(prospective, existing);
            }
            else
            {
                state.PlayerDarkGifts.AcquiredGiftInstances.Add(prospective);
                existing = prospective;
            }

            Record(state, "dark-gift.acquired", source, liveTarget.InstanceId, definition.RevisionId, requestId);
            if (string.Equals(stackPolicy, DarkGiftStackPolicies.Replace, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var replaced in activeOnTarget.Where(item => !ReferenceEquals(item, existing)))
                {
                    replaced.Active = false;
                    replaced.Expired = true;
                    Record(state, "dark-gift.replaced", source, liveTarget.InstanceId, replaced.DefinitionRevisionId, requestId);
                }
            }
            Record(state, "dark-gift.applied", source, liveTarget.InstanceId, resolution.Result, requestId);
            return Success("dark-gift.acquire.succeeded", resolution.Message, existing);
        }

        public static DarkGiftStateMachineResult Trigger(
            MatchState state,
            DarkGiftDefinition definition,
            DarkGiftTriggerRequest request,
            DarkGiftResolverRegistry registry)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            Normalize(state);
            if (request == null)
            {
                return Failure(state, null, null, null, "dark-gift.trigger.request.missing", "Dark Gift trigger request is required.", true);
            }

            var alreadyResolved = FindEvent(state.PlayerDarkGifts.TriggerHistory, request.RequestId, "dark-gift.resolved");
            if (alreadyResolved != null)
            {
                return Success(
                    "dark-gift.trigger.already-resolved",
                    "Dark Gift trigger was already resolved.",
                    FindInstance(state, request.TargetInstanceId, request.DefinitionRevisionId));
            }

            var instance = FindInstance(state, request.TargetInstanceId, request.DefinitionRevisionId);
            if (instance == null)
            {
                return Failure(state, request.TargetInstanceId, definition?.RevisionId, request.RequestId, "dark-gift.instance.missing", "Dark Gift instance was not found.", true);
            }
            if (definition == null ||
                !string.Equals(definition.RevisionId, instance.DefinitionRevisionId, StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(definition.EffectRevision))
            {
                return Failure(state, request.TargetInstanceId, instance.Source, request.RequestId, "dark-gift.definition.mismatch", "Dark Gift definition does not match the active instance.", true);
            }
            if (!instance.Active || instance.Expired)
            {
                return Failure(state, request.TargetInstanceId, instance.Source, request.RequestId, "dark-gift.expired", "Dark Gift is expired.", true);
            }
            if (instance.Suppressed)
            {
                return Failure(state, request.TargetInstanceId, instance.Source, request.RequestId, "dark-gift.suppressed", "Dark Gift is suppressed.", true);
            }
            if (instance.Cooldown > 0)
            {
                return Failure(state, request.TargetInstanceId, instance.Source, request.RequestId, "dark-gift.cooldown", "Dark Gift is on cooldown.", true);
            }
            if (instance.NextTriggerRound > state.Round)
            {
                return Failure(state, request.TargetInstanceId, instance.Source, request.RequestId, "dark-gift.trigger.not-ready", "Dark Gift has not reached its scheduled trigger round.", true);
            }
            if (!MatchesTrigger(definition.TriggerSpec, request.EventType))
            {
                return Failure(state, request.TargetInstanceId, instance.Source, request.RequestId, "dark-gift.trigger.not-matched", "Game event does not match this Dark Gift trigger.", true);
            }

            var liveTarget = FindPlayerMinion(state, request.TargetInstanceId);
            if (liveTarget == null)
            {
                return Failure(state, request.TargetInstanceId, instance.Source, request.RequestId, "dark-gift.target.missing", "Dark Gift target is no longer available.", true);
            }
            if (registry == null || !registry.TryGet(definition.EffectRevision, out var resolver))
            {
                return Failure(state, request.TargetInstanceId, instance.Source, request.RequestId, "dark-gift.resolver.not-found", "Dark Gift resolver is not registered: " + definition.EffectRevision, true);
            }

            var resolution = Resolve(
                resolver,
                new DarkGiftResolutionContext(
                    DarkGiftResolutionPhase.Trigger,
                    state.Round,
                    request.EventType,
                    definition,
                    instance,
                    liveTarget,
                    request.RequestId));
            if (resolution == null || !resolution.Succeeded)
            {
                return Failure(
                    state,
                    request.TargetInstanceId,
                    instance.Source,
                    request.RequestId,
                    resolution?.Code ?? "dark-gift.resolver.failed",
                    resolution?.Message ?? "Dark Gift resolver did not return a result.",
                    true);
            }

            resolution.Commit?.Invoke(state, liveTarget);
            Record(state, "dark-gift.triggered", instance.Source, liveTarget.InstanceId, request.EventType.ToString(), request.RequestId);
            if (definition.InitialUses > 0)
            {
                instance.RemainingUses = Math.Max(0, instance.RemainingUses - 1);
                if (instance.RemainingUses == 0)
                {
                    instance.Active = false;
                    instance.Expired = true;
                }
            }
            instance.Cooldown = Math.Max(0, definition.CooldownRounds);
            Record(state, "dark-gift.resolved", instance.Source, liveTarget.InstanceId, resolution.Result, request.RequestId);
            return Success("dark-gift.trigger.succeeded", resolution.Message, instance);
        }

        public static bool HandlesEvent(DarkGiftDefinition definition, MechanicEventType eventType)
        {
            return definition != null &&
                   !string.IsNullOrWhiteSpace(definition.TriggerSpec) &&
                   MatchesTrigger(definition.TriggerSpec, eventType);
        }

        public static void AdvanceRound(MatchState state, IEnumerable<DarkGiftDefinition> definitions = null)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            Normalize(state);
            var byRevision = (definitions ?? Enumerable.Empty<DarkGiftDefinition>())
                .Where(item => item != null && !string.IsNullOrWhiteSpace(item.RevisionId))
                .GroupBy(item => item.RevisionId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            foreach (var instance in state.PlayerDarkGifts.AcquiredGiftInstances.Where(item => item != null))
            {
                var counterKey = AdvanceCounterPrefix + instance.InstanceId + "|" + instance.DefinitionRevisionId;
                state.PlayerDarkGifts.Counters.TryGetValue(counterKey, out var lastAdvancedRound);
                if (lastAdvancedRound >= state.Round)
                {
                    continue;
                }

                var elapsed = lastAdvancedRound > 0 ? Math.Max(1, state.Round - lastAdvancedRound) : 1;
                instance.Cooldown = Math.Max(0, instance.Cooldown - elapsed);
                state.PlayerDarkGifts.Counters[counterKey] = Math.Max(1, state.Round);
                if (!instance.Active || instance.Expired ||
                    !byRevision.TryGetValue(instance.DefinitionRevisionId, out var definition) ||
                    !string.Equals(definition.DurationPolicy, DarkGiftDurationPolicies.Rounds, StringComparison.OrdinalIgnoreCase) ||
                    definition.DurationRounds <= 0 ||
                    state.Round < instance.AcquiredRound + definition.DurationRounds)
                {
                    continue;
                }

                instance.Active = false;
                instance.Expired = true;
                Record(state, "dark-gift.expired", instance.Source, instance.InstanceId, instance.DefinitionRevisionId, null);
            }
        }

        public static bool SetSuppressed(
            MatchState state,
            string targetInstanceId,
            string definitionRevisionId,
            bool suppressed,
            string source,
            string requestId)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            Normalize(state);
            var instance = FindInstance(state, targetInstanceId, definitionRevisionId);
            if (instance == null || instance.Expired || instance.Suppressed == suppressed)
            {
                return false;
            }

            instance.Suppressed = suppressed;
            Record(
                state,
                suppressed ? "dark-gift.suppressed" : "dark-gift.unsuppressed",
                source,
                targetInstanceId,
                definitionRevisionId,
                requestId);
            return true;
        }

        private static DarkGiftResolution Resolve(DarkGiftResolver resolver, DarkGiftResolutionContext context)
        {
            try
            {
                return resolver(context);
            }
            catch (Exception exception)
            {
                return DarkGiftResolution.Failure("dark-gift.resolver.failed", exception.Message);
            }
        }

        private static bool MatchesTrigger(string triggerSpec, MechanicEventType eventType)
        {
            return string.IsNullOrWhiteSpace(triggerSpec) ||
                   string.Equals(triggerSpec, "any", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(triggerSpec, eventType.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        private static void Normalize(MatchState state)
        {
            state.PlayerDarkGifts = state.PlayerDarkGifts ?? new PlayerDarkGiftState();
            state.PlayerDarkGifts.AcquiredGiftInstances = state.PlayerDarkGifts.AcquiredGiftInstances ?? new List<PlayerDarkGiftInstance>();
            state.PlayerDarkGifts.Counters = state.PlayerDarkGifts.Counters ?? new Dictionary<string, int>();
            state.PlayerDarkGifts.Cooldowns = state.PlayerDarkGifts.Cooldowns ?? new Dictionary<string, int>();
            state.PlayerDarkGifts.TriggerHistory = state.PlayerDarkGifts.TriggerHistory ?? new DarkGiftTriggerHistory();
            state.PlayerDarkGifts.TriggerHistory.Events = state.PlayerDarkGifts.TriggerHistory.Events ?? new List<MechanicEventRecord>();
        }

        private static MinionInstance FindPlayerMinion(MatchState state, string instanceId)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                return null;
            }

            return (state.Player?.Board ?? new List<MinionInstance>())
                .Concat(state.Player?.Tavern?.Hand ?? new List<MinionInstance>())
                .Concat(state.Player?.Tavern?.Shop ?? new List<MinionInstance>())
                .FirstOrDefault(item => item != null &&
                                        item.CardKind == CardKind.Minion &&
                                        string.Equals(item.InstanceId, instanceId, StringComparison.Ordinal));
        }

        private static PlayerDarkGiftInstance FindInstance(MatchState state, string instanceId, string revisionId)
        {
            return state.PlayerDarkGifts.AcquiredGiftInstances.FirstOrDefault(item =>
                item != null &&
                string.Equals(item.InstanceId, instanceId, StringComparison.Ordinal) &&
                string.Equals(item.DefinitionRevisionId, revisionId, StringComparison.Ordinal));
        }

        private static MechanicEventRecord FindEvent(DarkGiftTriggerHistory history, string requestId, string type)
        {
            if (string.IsNullOrWhiteSpace(requestId))
            {
                return null;
            }

            return (history?.Events ?? new List<MechanicEventRecord>()).FirstOrDefault(item =>
                item != null &&
                string.Equals(item.RequestId, requestId, StringComparison.Ordinal) &&
                string.Equals(item.Type, type, StringComparison.Ordinal));
        }

        private static void CopyState(PlayerDarkGiftInstance source, PlayerDarkGiftInstance target)
        {
            target.InstanceId = source.InstanceId;
            target.DefinitionRevisionId = source.DefinitionRevisionId;
            target.AcquiredRound = source.AcquiredRound;
            target.Source = source.Source;
            target.StackCount = source.StackCount;
            target.RemainingUses = source.RemainingUses;
            target.Cooldown = source.Cooldown;
            target.NextTriggerRound = source.NextTriggerRound;
            target.Active = source.Active;
            target.Suppressed = source.Suppressed;
            target.Expired = source.Expired;
        }

        private static void Record(
            MatchState state,
            string type,
            string source,
            string targetInstanceId,
            string result,
            string requestId)
        {
            var record = MechanicEventLog.Append(
                state,
                type,
                source,
                string.IsNullOrWhiteSpace(targetInstanceId) ? null : new[] { targetInstanceId },
                result,
                requestId);
            state.PlayerDarkGifts.TriggerHistory.Events.Add(record.Clone());
        }

        private static DarkGiftStateMachineResult Failure(
            MatchState state,
            string targetInstanceId,
            string source,
            string requestId,
            string code,
            string message,
            bool trigger = false)
        {
            Record(
                state,
                trigger ? "dark-gift.trigger-rejected" : "dark-gift.rejected",
                source,
                targetInstanceId,
                code,
                requestId);
            return new DarkGiftStateMachineResult
            {
                Succeeded = false,
                Code = code,
                Message = message
            };
        }

        private static DarkGiftStateMachineResult Success(
            string code,
            string message,
            PlayerDarkGiftInstance instance)
        {
            return new DarkGiftStateMachineResult
            {
                Succeeded = true,
                Code = code,
                Message = message,
                Instance = instance?.Clone()
            };
        }
    }
}
