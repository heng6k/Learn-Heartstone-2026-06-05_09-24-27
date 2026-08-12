using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Domain.Engine
{
    public delegate RecruitActionResolution RecruitActionResolver(RecruitActionExecutionContext context);

    public sealed class RecruitActionExecutionContext
    {
        public RecruitActionExecutionContext(
            int round,
            int goldBefore,
            RecruitActionDefinition definition,
            RecruitActionRequest request,
            MinionInstance source,
            MinionInstance target)
        {
            Round = round;
            GoldBefore = goldBefore;
            Definition = definition?.Clone();
            Request = request?.Clone();
            Source = source?.Clone();
            Target = target?.Clone();
        }

        public int Round { get; }
        public int GoldBefore { get; }
        public RecruitActionDefinition Definition { get; }
        public RecruitActionRequest Request { get; }
        public MinionInstance Source { get; }
        public MinionInstance Target { get; }
    }

    public sealed class RecruitActionResolution
    {
        private RecruitActionResolution(
            bool succeeded,
            string code,
            string message,
            Action<MatchState> commit,
            IEnumerable<string> events)
        {
            Succeeded = succeeded;
            Code = code;
            Message = message;
            Commit = commit;
            Events = events == null ? new List<string>() : new List<string>(events);
        }

        public bool Succeeded { get; }
        public string Code { get; }
        public string Message { get; }
        public Action<MatchState> Commit { get; }
        public List<string> Events { get; }

        public static RecruitActionResolution Success(Action<MatchState> commit = null, IEnumerable<string> events = null)
        {
            return new RecruitActionResolution(true, "recruit-action.succeeded", string.Empty, commit, events);
        }

        public static RecruitActionResolution Failure(string code, string message)
        {
            return new RecruitActionResolution(false, code, message, null, null);
        }
    }

    public sealed class RecruitActionResolverRegistry
    {
        private readonly Dictionary<string, RecruitActionResolver> resolvers =
            new Dictionary<string, RecruitActionResolver>(StringComparer.Ordinal);

        public void Register(string resolverId, RecruitActionResolver resolver)
        {
            if (string.IsNullOrWhiteSpace(resolverId))
            {
                throw new ArgumentException("Resolver ID is required.", nameof(resolverId));
            }
            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            resolvers[resolverId] = resolver;
        }

        public bool TryGet(string resolverId, out RecruitActionResolver resolver)
        {
            resolver = null;
            return !string.IsNullOrWhiteSpace(resolverId) &&
                   resolvers.TryGetValue(resolverId, out resolver);
        }
    }

    public static class RecruitActionService
    {
        public static RecruitActionResult Execute(
            MatchState state,
            RecruitActionDefinition definition,
            RecruitActionRequest request,
            RecruitActionResolver resolver)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var tavern = state.Player?.Tavern;
            var goldBefore = tavern?.Gold ?? 0;
            if (request == null)
            {
                return Failure(state, request, "recruit-action.request.missing", "Recruit action request is required.", goldBefore);
            }

            var allowedPhase = definition?.AllowedPhase ?? MatchPhase.Tavern;
            if (state.Phase != allowedPhase)
            {
                return Failure(
                    state,
                    request,
                    "recruit-action.phase.invalid",
                    "Recruit action is not allowed during " + state.Phase + " phase.",
                    goldBefore);
            }

            var source = state.Player?.Board?.FirstOrDefault(item =>
                item != null &&
                string.Equals(item.InstanceId, request.SourceInstanceId, StringComparison.Ordinal));
            if (source == null)
            {
                return Failure(state, request, "recruit-action.source.missing", "Recruit action source is no longer on the player board.", goldBefore);
            }

            if (definition == null ||
                string.IsNullOrWhiteSpace(definition.ActionId) ||
                !string.Equals(definition.ActionId, request.ActionId, StringComparison.Ordinal))
            {
                return Failure(state, request, "recruit-action.definition.not-found", "Recruit action definition was not found for the source.", goldBefore);
            }
            if (string.IsNullOrWhiteSpace(definition.ResolverId) || definition.UsesPerTurn <= 0)
            {
                return Failure(state, request, "recruit-action.definition.invalid", "Recruit action definition is invalid.", goldBefore);
            }

            var actionStates = state.RecruitActionStates ?? new List<RecruitActionState>();
            var actionState = actionStates.FirstOrDefault(item =>
                item != null &&
                string.Equals(item.SourceInstanceId, source.InstanceId, StringComparison.Ordinal));
            var usesThisTurn = actionState != null && actionState.LastUsedRound == state.Round
                ? Math.Max(0, actionState.UsesThisTurn)
                : 0;
            if (!string.IsNullOrWhiteSpace(actionState?.LockedReason))
            {
                return Failure(state, request, "recruit-action.locked", actionState.LockedReason, goldBefore, usesThisTurn);
            }
            if ((actionState?.Cooldown ?? 0) > 0)
            {
                return Failure(state, request, "recruit-action.cooldown", "Recruit action is on cooldown.", goldBefore, usesThisTurn);
            }
            if (usesThisTurn >= definition.UsesPerTurn)
            {
                return Failure(state, request, "recruit-action.uses.exhausted", "Recruit action has no uses remaining this turn.", goldBefore, usesThisTurn);
            }

            var goldCost = definition.CostSpec?.Gold ?? 0;
            if (goldCost < 0)
            {
                return Failure(state, request, "recruit-action.definition.invalid", "Recruit action gold cost cannot be negative.", goldBefore, usesThisTurn);
            }
            if (tavern == null || goldBefore < goldCost)
            {
                return Failure(state, request, "recruit-action.cost.insufficient-gold", "Not enough Gold for this recruit action.", goldBefore, usesThisTurn);
            }

            if (!TryResolveTarget(state, source, definition.TargetSpec, request, out var target))
            {
                return Failure(state, request, "recruit-action.target.invalid", "Recruit action target is invalid.", goldBefore, usesThisTurn);
            }
            if (resolver == null)
            {
                return Failure(state, request, "recruit-action.resolver.not-found", "Recruit action resolver is not registered: " + definition.ResolverId, goldBefore, usesThisTurn);
            }

            RecruitActionResolution resolution;
            try
            {
                resolution = resolver(new RecruitActionExecutionContext(
                    state.Round,
                    goldBefore,
                    definition,
                    request,
                    source,
                    target));
            }
            catch (Exception exception)
            {
                return Failure(state, request, "recruit-action.resolver.failed", exception.Message, goldBefore, usesThisTurn);
            }
            if (resolution == null || !resolution.Succeeded)
            {
                return Failure(
                    state,
                    request,
                    resolution?.Code ?? "recruit-action.resolver.failed",
                    resolution?.Message ?? "Recruit action resolver did not return a result.",
                    goldBefore,
                    usesThisTurn);
            }

            var eventTargets = string.IsNullOrWhiteSpace(request.TargetInstanceId)
                ? null
                : new[] { request.TargetInstanceId };
            MechanicEventLog.Append(
                state,
                "recruit-action.validated",
                request.SourceInstanceId,
                eventTargets,
                "validated",
                request.ActionId);
            tavern.Gold = goldBefore - goldCost;
            MechanicEventLog.Append(
                state,
                "recruit-action.paid",
                request.SourceInstanceId,
                eventTargets,
                "gold=" + goldCost,
                request.ActionId);
            try
            {
                resolution.Commit?.Invoke(state);
            }
            catch
            {
                tavern.Gold = goldBefore;
                throw;
            }

            if (actionState == null)
            {
                actionState = new RecruitActionState { SourceInstanceId = source.InstanceId };
                state.RecruitActionStates = state.RecruitActionStates ?? new List<RecruitActionState>();
                state.RecruitActionStates.Add(actionState);
            }
            actionState.UsesThisTurn = usesThisTurn + 1;
            actionState.LastUsedRound = state.Round;

            MechanicEventLog.Append(
                state,
                "recruit-action.resolved",
                request.SourceInstanceId,
                eventTargets,
                resolution.Message,
                request.ActionId);

            return new RecruitActionResult
            {
                Succeeded = true,
                Code = "recruit-action.succeeded",
                Message = resolution.Message,
                GoldBefore = goldBefore,
                GoldAfter = tavern.Gold,
                GoldSpent = goldCost,
                UsesThisTurn = actionState.UsesThisTurn,
                Events = new List<string>(resolution.Events ?? new List<string>())
            };
        }

        private static bool TryResolveTarget(
            MatchState state,
            MinionInstance source,
            RecruitActionTargetSpec targetSpec,
            RecruitActionRequest request,
            out MinionInstance target)
        {
            target = null;
            if (targetSpec == RecruitActionTargetSpec.None)
            {
                return string.IsNullOrWhiteSpace(request.TargetInstanceId) &&
                    request.TargetIndex < 0 &&
                    request.TargetZone == TargetZone.Unspecified;
            }

            List<MinionInstance> candidates;
            if (targetSpec == RecruitActionTargetSpec.TavernMinion)
            {
                if (request.TargetZone != TargetZone.Unspecified && request.TargetZone != TargetZone.TavernShop)
                {
                    return false;
                }
                candidates = state.Player?.Tavern?.Shop;
            }
            else
            {
                if (request.TargetZone != TargetZone.Unspecified && request.TargetZone != TargetZone.FriendlyBoard)
                {
                    return false;
                }
                candidates = state.Player?.Board;
            }

            if (candidates == null)
            {
                return false;
            }
            target = !string.IsNullOrWhiteSpace(request.TargetInstanceId)
                ? candidates.FirstOrDefault(item => item != null && string.Equals(item.InstanceId, request.TargetInstanceId, StringComparison.Ordinal))
                : request.TargetIndex >= 0 && request.TargetIndex < candidates.Count
                    ? candidates[request.TargetIndex]
                    : null;
            return target != null &&
                (targetSpec != RecruitActionTargetSpec.OtherFriendlyBoardMinion ||
                 !string.Equals(target.InstanceId, source.InstanceId, StringComparison.Ordinal));
        }

        private static RecruitActionResult Failure(
            MatchState state,
            RecruitActionRequest request,
            string code,
            string message,
            int gold,
            int usesThisTurn = 0)
        {
            var result = new RecruitActionResult
            {
                Succeeded = false,
                Code = code,
                Message = message,
                GoldBefore = gold,
                GoldAfter = gold,
                GoldSpent = 0,
                UsesThisTurn = usesThisTurn,
                Events = new List<string>()
            };
            MechanicEventLog.Append(
                state,
                "recruit-action.rejected",
                request?.SourceInstanceId,
                string.IsNullOrWhiteSpace(request?.TargetInstanceId) ? null : new[] { request.TargetInstanceId },
                code,
                request?.ActionId);
            return result;
        }
    }
}
