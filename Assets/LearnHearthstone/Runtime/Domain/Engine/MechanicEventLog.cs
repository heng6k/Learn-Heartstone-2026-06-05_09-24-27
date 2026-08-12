using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Domain.Engine
{
    public static class MechanicEventLog
    {
        public static MechanicEventRecord Append(
            MatchState state,
            string type,
            string source,
            IEnumerable<string> targets = null,
            string result = null,
            string requestId = null)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            state.MechanicEvents = state.MechanicEvents ?? new List<MechanicEventRecord>();
            var nextSequence = state.MechanicEvents.Count == 0
                ? 1
                : state.MechanicEvents.Where(item => item != null).Select(item => item.Sequence).DefaultIfEmpty(0).Max() + 1;
            var entry = new MechanicEventRecord
            {
                Sequence = Math.Max(1, nextSequence),
                Round = Math.Max(1, state.Round),
                Phase = state.Phase,
                Type = type,
                Source = source,
                Targets = (targets ?? Enumerable.Empty<string>()).Where(item => !string.IsNullOrWhiteSpace(item)).ToList(),
                Result = result,
                RequestId = requestId
            };
            state.MechanicEvents.Add(entry);
            return entry;
        }
    }
}
