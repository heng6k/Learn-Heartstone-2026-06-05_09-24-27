using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Domain.Engine
{
    public static class ChoiceQueueService
    {
        public static ChoiceQueueItem Enqueue(ChoiceQueueState state, ChoiceQueueItem request)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            Normalize(state);
            if (!string.IsNullOrWhiteSpace(request.RequestId))
            {
                if (IsCompleted(state, request.RequestId))
                {
                    return null;
                }

                var existing = FindExisting(state, request.RequestId);
                if (existing != null)
                {
                    return existing;
                }
            }

            if (string.IsNullOrWhiteSpace(request.Source))
            {
                throw new InvalidOperationException("Choice source is required.");
            }

            if (request.CreatedRound <= 0)
            {
                throw new InvalidOperationException("Choice created round must be positive.");
            }

            var queued = request.Clone();
            if (queued.Sequence <= 0)
            {
                queued.Sequence = state.NextSequence;
            }
            else if (HasSequence(state, queued.Sequence))
            {
                throw new InvalidOperationException("Choice sequence is already in use: " + queued.Sequence);
            }

            state.NextSequence = Math.Max(state.NextSequence, queued.Sequence + 1);
            if (string.IsNullOrWhiteSpace(queued.RequestId))
            {
                queued.RequestId = BuildRequestId(queued);
            }

            if (IsCompleted(state, queued.RequestId))
            {
                return null;
            }

            var duplicate = FindExisting(state, queued.RequestId);
            if (duplicate != null)
            {
                return duplicate;
            }

            if (state.ActiveChoice == null)
            {
                state.ActiveChoice = queued;
            }
            else
            {
                state.PendingChoices.Add(queued);
            }

            return queued;
        }

        public static bool CompleteActive(ChoiceQueueState state, string requestId)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            Normalize(state);
            if (state.ActiveChoice == null ||
                !string.Equals(state.ActiveChoice.RequestId, requestId, StringComparison.Ordinal))
            {
                return false;
            }

            if (!IsCompleted(state, requestId))
            {
                state.CompletedRequestIds.Add(requestId);
            }

            state.ActiveChoice = null;
            ActivateNext(state);
            return true;
        }

        public static bool Cancel(ChoiceQueueState state, string requestId)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            Normalize(state);
            if (string.IsNullOrWhiteSpace(requestId))
            {
                return false;
            }

            if (string.Equals(state.ActiveChoice?.RequestId, requestId, StringComparison.Ordinal))
            {
                state.ActiveChoice = null;
                ActivateNext(state);
                return true;
            }

            var pending = state.PendingChoices.FirstOrDefault(item =>
                string.Equals(item.RequestId, requestId, StringComparison.Ordinal));
            return pending != null && state.PendingChoices.Remove(pending);
        }

        public static ChoiceQueueItem ActivateNext(ChoiceQueueState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            Normalize(state);
            if (state.ActiveChoice != null || state.PendingChoices.Count == 0)
            {
                return state.ActiveChoice;
            }

            var next = state.PendingChoices
                .OrderBy(item => item.Priority)
                .ThenBy(item => item.Sequence)
                .First();
            state.PendingChoices.Remove(next);
            state.ActiveChoice = next;
            return next;
        }

        public static ChoiceQueueState Normalize(ChoiceQueueState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            state.PendingChoices = state.PendingChoices ?? new System.Collections.Generic.List<ChoiceQueueItem>();
            state.PendingChoices.RemoveAll(item => item == null);
            state.CompletedRequestIds = state.CompletedRequestIds ?? new System.Collections.Generic.List<string>();
            state.CompletedRequestIds = state.CompletedRequestIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            var maxSequence = state.PendingChoices
                .Select(item => item.Sequence)
                .Concat(state.ActiveChoice == null ? Enumerable.Empty<int>() : new[] { state.ActiveChoice.Sequence })
                .DefaultIfEmpty(0)
                .Max();
            state.NextSequence = Math.Max(Math.Max(1, state.NextSequence), maxSequence + 1);
            return state;
        }

        public static void SynchronizeDiscoverAdapter(ChoiceQueueState state, TavernState tavern, int createdRound)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (tavern == null)
            {
                return;
            }

            var hasLegacyDiscover = tavern.Discover != null || (tavern.DiscoverQueue?.Count ?? 0) > 0;
            var hasQueuedDiscover = state.ActiveChoice?.Kind == ChoiceRequestKind.Discover ||
                (state.PendingChoices?.Any(choice => choice?.Kind == ChoiceRequestKind.Discover) ?? false);
            if (!hasLegacyDiscover && !hasQueuedDiscover)
            {
                tavern.DiscoverQueue = tavern.DiscoverQueue ?? new List<DiscoverState>();
                return;
            }

            Normalize(state);
            var legacyDiscovers = new List<DiscoverState>();
            AddDiscoverReference(legacyDiscovers, tavern.Discover);
            foreach (var discover in tavern.DiscoverQueue ?? new List<DiscoverState>())
            {
                AddDiscoverReference(legacyDiscovers, discover);
            }

            var discoverChoices = GetDiscoverChoices(state).ToList();
            foreach (var discover in legacyDiscovers)
            {
                if (discoverChoices.Any(choice => ReferenceEquals(choice.Discover, discover)))
                {
                    continue;
                }

                var unbound = discoverChoices.FirstOrDefault(choice => choice.Discover == null);
                if (unbound != null)
                {
                    UpdateDiscoverChoice(unbound, discover);
                    continue;
                }

                var request = new ChoiceQueueItem
                {
                    Kind = ChoiceRequestKind.Discover,
                    CreatedRound = Math.Max(1, createdRound),
                    Priority = 100,
                    Blocking = true
                };
                UpdateDiscoverChoice(request, discover);
                var queued = Enqueue(state, request);
                if (queued != null)
                {
                    UpdateDiscoverChoice(queued, discover);
                    discoverChoices.Add(queued);
                }
            }

            RefreshDiscoverAdapter(state, tavern);
        }

        public static void UpdateDiscoverChoice(ChoiceQueueItem choice, DiscoverState discover)
        {
            if (choice == null)
            {
                throw new ArgumentNullException(nameof(choice));
            }

            if (discover == null)
            {
                throw new ArgumentNullException(nameof(discover));
            }

            choice.Kind = ChoiceRequestKind.Discover;
            choice.Source = string.IsNullOrWhiteSpace(discover.Source) ? "discover" : discover.Source;
            choice.RemainingPicks = Math.Max(1, discover.RemainingPicks);
            choice.Discover = discover;
            choice.Options = (discover.Options ?? new List<MinionInstance>())
                .Select((option, index) => new MechanicChoiceOption
                {
                    OptionId = option?.InstanceId ?? option?.CardId ?? "discover-option-" + index,
                    SourceId = option?.CardId,
                    DisplayName = option?.Name,
                    Text = option?.Text,
                    ImagePath = option?.ImagePath,
                    Tags = option?.Tags == null ? new List<string>() : new List<string>(option.Tags)
                })
                .ToList();
        }

        public static void RefreshDiscoverAdapter(ChoiceQueueState state, TavernState tavern)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (tavern == null)
            {
                return;
            }

            Normalize(state);
            tavern.Discover = state.ActiveChoice?.Kind == ChoiceRequestKind.Discover
                ? state.ActiveChoice.Discover
                : null;
            tavern.DiscoverQueue = state.PendingChoices
                .Where(choice => choice.Kind == ChoiceRequestKind.Discover && choice.Discover != null)
                .OrderBy(choice => choice.Priority)
                .ThenBy(choice => choice.Sequence)
                .Select(choice => choice.Discover)
                .ToList();
        }

        private static IEnumerable<ChoiceQueueItem> GetDiscoverChoices(ChoiceQueueState state)
        {
            if (state.ActiveChoice?.Kind == ChoiceRequestKind.Discover)
            {
                yield return state.ActiveChoice;
            }

            foreach (var choice in state.PendingChoices
                         .Where(item => item.Kind == ChoiceRequestKind.Discover)
                         .OrderBy(item => item.Priority)
                         .ThenBy(item => item.Sequence))
            {
                yield return choice;
            }
        }

        private static void AddDiscoverReference(List<DiscoverState> discovers, DiscoverState candidate)
        {
            if (candidate != null && !discovers.Any(discover => ReferenceEquals(discover, candidate)))
            {
                discovers.Add(candidate);
            }
        }

        private static string BuildRequestId(ChoiceQueueItem request)
        {
            return request.Kind.ToString().ToLowerInvariant() + ":" + request.Source.Trim() + ":" +
                request.CreatedRound.ToString(CultureInfo.InvariantCulture) + ":" +
                request.Sequence.ToString(CultureInfo.InvariantCulture);
        }

        private static ChoiceQueueItem FindExisting(ChoiceQueueState state, string requestId)
        {
            if (string.Equals(state.ActiveChoice?.RequestId, requestId, StringComparison.Ordinal))
            {
                return state.ActiveChoice;
            }

            return state.PendingChoices.FirstOrDefault(item =>
                string.Equals(item.RequestId, requestId, StringComparison.Ordinal));
        }

        private static bool IsCompleted(ChoiceQueueState state, string requestId)
        {
            return state.CompletedRequestIds.Any(id => string.Equals(id, requestId, StringComparison.Ordinal));
        }

        private static bool HasSequence(ChoiceQueueState state, int sequence)
        {
            return state.ActiveChoice?.Sequence == sequence ||
                state.PendingChoices.Any(item => item.Sequence == sequence);
        }
    }
}
