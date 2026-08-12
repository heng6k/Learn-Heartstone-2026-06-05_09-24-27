using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Domain.Engine
{
    public delegate DelayedObjectResolution DelayedObjectResolver(DelayedObjectExecutionContext context);

    public sealed class DelayedObjectExecutionContext
    {
        public DelayedObjectExecutionContext(int round, DelayedObjectState delayedObject)
        {
            Round = round;
            DelayedObject = delayedObject?.Clone();
        }

        public int Round { get; }
        public DelayedObjectState DelayedObject { get; }
    }

    public sealed class DelayedObjectResolution
    {
        private DelayedObjectResolution(bool succeeded, string code, string message, Action<MatchState> commit)
        {
            Succeeded = succeeded;
            Code = code;
            Message = message;
            Commit = commit;
        }

        public bool Succeeded { get; }
        public string Code { get; }
        public string Message { get; }
        public Action<MatchState> Commit { get; }

        public static DelayedObjectResolution Success(Action<MatchState> commit = null)
        {
            return new DelayedObjectResolution(true, "delayed-object.opened", string.Empty, commit);
        }

        public static DelayedObjectResolution Failure(string code, string message)
        {
            return new DelayedObjectResolution(false, code, message, null);
        }
    }

    public sealed class DelayedObjectResolverRegistry
    {
        private readonly Dictionary<string, DelayedObjectResolver> resolvers =
            new Dictionary<string, DelayedObjectResolver>(StringComparer.Ordinal);

        public void Register(string resolverId, DelayedObjectResolver resolver)
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

        public bool TryGet(string resolverId, out DelayedObjectResolver resolver)
        {
            resolver = null;
            return !string.IsNullOrWhiteSpace(resolverId) &&
                   resolvers.TryGetValue(resolverId, out resolver);
        }
    }

    public sealed class DelayedObjectResult
    {
        public bool Succeeded;
        public string Code;
        public string Message;
        public int RemainingTurns;
        public bool Opened;
    }

    public static class DelayedObjectService
    {
        public static bool Add(MatchState state, DelayedObjectState delayedObject, string requestId = null)
        {
            if (state == null || delayedObject == null || string.IsNullOrWhiteSpace(delayedObject.InstanceId))
            {
                return false;
            }

            state.DelayedObjectStates = state.DelayedObjectStates ?? new List<DelayedObjectState>();
            if (state.DelayedObjectStates.Any(item =>
                item != null &&
                string.Equals(item.InstanceId, delayedObject.InstanceId, StringComparison.Ordinal)))
            {
                return false;
            }

            var added = delayedObject.Clone();
            added.CreatedRound = added.CreatedRound > 0 ? added.CreatedRound : Math.Max(1, state.Round);
            added.RemainingTurns = Math.Max(0, added.RemainingTurns);
            added.Opened = false;
            state.DelayedObjectStates.Add(added);
            MechanicEventLog.Append(
                state,
                "delayed-object.created",
                added.Source,
                new[] { added.InstanceId },
                "remaining=" + added.RemainingTurns,
                requestId);
            return true;
        }

        public static DelayedObjectResult Advance(
            MatchState state,
            string instanceId,
            int turns,
            DelayedObjectResolver resolver,
            string requestId = null,
            string eventSource = null,
            string eventType = null)
        {
            var delayedObject = Find(state, instanceId);
            if (delayedObject == null)
            {
                return Failure("delayed-object.not-found", "Delayed object does not exist.");
            }
            if (delayedObject.Opened)
            {
                return Failure("delayed-object.already-opened", "Delayed object has already opened.", delayedObject);
            }
            if (turns <= 0)
            {
                return Failure("delayed-object.advance.invalid", "Advance amount must be positive.", delayedObject);
            }
            if (WasRequestApplied(state, delayedObject.InstanceId, requestId))
            {
                return AlreadyApplied(delayedObject);
            }

            var previousRemainingTurns = delayedObject.RemainingTurns;
            var previousEventCount = state.MechanicEvents?.Count ?? 0;
            delayedObject.RemainingTurns = Math.Max(0, delayedObject.RemainingTurns - turns);
            MechanicEventLog.Append(
                state,
                string.IsNullOrWhiteSpace(eventType) ? "delayed-object.accelerated" : eventType,
                string.IsNullOrWhiteSpace(eventSource) ? delayedObject.Source : eventSource,
                new[] { delayedObject.InstanceId },
                "remaining=" + delayedObject.RemainingTurns,
                requestId);
            if (delayedObject.RemainingTurns > 0)
            {
                return Success(delayedObject);
            }

            var openResult = TryOpen(state, instanceId, resolver, requestId);
            if (openResult.Succeeded)
            {
                return openResult;
            }

            delayedObject.RemainingTurns = previousRemainingTurns;
            if (state.MechanicEvents != null && state.MechanicEvents.Count > previousEventCount)
            {
                state.MechanicEvents.RemoveRange(previousEventCount, state.MechanicEvents.Count - previousEventCount);
            }
            openResult.RemainingTurns = previousRemainingTurns;
            return openResult;
        }

        public static DelayedObjectResult TryOpen(
            MatchState state,
            string instanceId,
            DelayedObjectResolver resolver,
            string requestId = null)
        {
            var delayedObject = Find(state, instanceId);
            if (delayedObject == null)
            {
                return Failure("delayed-object.not-found", "Delayed object does not exist.");
            }
            if (delayedObject.Opened)
            {
                return Failure("delayed-object.already-opened", "Delayed object has already opened.", delayedObject);
            }
            if (delayedObject.RemainingTurns > 0)
            {
                return Failure("delayed-object.not-ready", "Delayed object is not ready to open.", delayedObject);
            }
            if (resolver == null)
            {
                return Failure("delayed-object.resolver.not-found", "Delayed object resolver is not registered: " + delayedObject.OpenResolverId, delayedObject);
            }

            DelayedObjectResolution resolution;
            try
            {
                resolution = resolver(new DelayedObjectExecutionContext(Math.Max(1, state.Round), delayedObject));
            }
            catch (Exception exception)
            {
                return Failure("delayed-object.resolver.failed", exception.Message, delayedObject);
            }
            if (resolution == null || !resolution.Succeeded)
            {
                return Failure(
                    resolution?.Code ?? "delayed-object.resolver.failed",
                    resolution?.Message ?? "Delayed object resolver did not return a result.",
                    delayedObject);
            }

            try
            {
                resolution.Commit?.Invoke(state);
            }
            catch (Exception exception)
            {
                return Failure("delayed-object.commit.failed", exception.Message, delayedObject);
            }
            delayedObject.Opened = true;
            delayedObject.RemainingTurns = 0;
            MechanicEventLog.Append(
                state,
                "delayed-object.opened",
                delayedObject.Source,
                new[] { delayedObject.InstanceId },
                resolution.Message,
                string.IsNullOrWhiteSpace(requestId) ? delayedObject.OpenResolverId : requestId);
            return Success(delayedObject);
        }

        private static bool WasRequestApplied(MatchState state, string instanceId, string requestId)
        {
            return !string.IsNullOrWhiteSpace(requestId) &&
                   state?.MechanicEvents != null &&
                   state.MechanicEvents.Any(item =>
                       item != null &&
                       string.Equals(item.RequestId, requestId, StringComparison.Ordinal) &&
                       (item.Targets ?? new List<string>()).Contains(instanceId));
        }

        private static DelayedObjectState Find(MatchState state, string instanceId)
        {
            return state?.DelayedObjectStates?.FirstOrDefault(item =>
                item != null &&
                string.Equals(item.InstanceId, instanceId, StringComparison.Ordinal));
        }

        private static DelayedObjectResult Success(DelayedObjectState delayedObject)
        {
            return new DelayedObjectResult
            {
                Succeeded = true,
                Code = delayedObject.Opened ? "delayed-object.opened" : "delayed-object.advanced",
                Message = string.Empty,
                RemainingTurns = delayedObject.RemainingTurns,
                Opened = delayedObject.Opened
            };
        }

        private static DelayedObjectResult AlreadyApplied(DelayedObjectState delayedObject)
        {
            return new DelayedObjectResult
            {
                Succeeded = true,
                Code = "delayed-object.already-applied",
                Message = string.Empty,
                RemainingTurns = delayedObject.RemainingTurns,
                Opened = delayedObject.Opened
            };
        }

        private static DelayedObjectResult Failure(string code, string message, DelayedObjectState delayedObject = null)
        {
            return new DelayedObjectResult
            {
                Succeeded = false,
                Code = code,
                Message = message,
                RemainingTurns = delayedObject?.RemainingTurns ?? 0,
                Opened = delayedObject?.Opened ?? false
            };
        }
    }
}
