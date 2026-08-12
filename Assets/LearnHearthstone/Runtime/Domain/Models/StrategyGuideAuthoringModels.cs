using System;
using System.Collections.Generic;

namespace LearnHearthstone.Domain.Models
{
    [Serializable]
    public sealed class StrategyGuideAuthoringDraft
    {
        public int SchemaVersion = 1;
        public string DraftId;
        public StrategyGuideDefinition Guide = new StrategyGuideDefinition();
    }

    public sealed class StrategyGuideAuthoringFreezeResult
    {
        public StrategyGuideDefinition Guide;
        public string ContentHash;
        public List<string> Diagnostics = new List<string>();

        public bool Succeeded => Guide != null && Diagnostics.Count == 0;
    }
}
