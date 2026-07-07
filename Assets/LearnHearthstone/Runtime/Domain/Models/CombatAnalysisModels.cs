using System;
using System.Collections.Generic;

namespace LearnHearthstone.Domain.Models
{
    [Serializable]
    public sealed class CombatExplanation
    {
        public CombatWinner Winner;
        public string Summary;
        public List<CombatExplanationItem> MainFactors = new List<CombatExplanationItem>();
        public List<CombatExplanationItem> VariableSignals = new List<CombatExplanationItem>();
        public List<CombatExplanationItem> TriggerSignals = new List<CombatExplanationItem>();
        public List<CombatContribution> TopContributors = new List<CombatContribution>();
        public List<string> KeySwingCandidates = new List<string>();
    }

    [Serializable]
    public sealed class CombatExplanationItem
    {
        public string Title;
        public string Detail;
        public int Count;
        public BoardSide Side;
        public LogSeverity Severity;
    }

    [Serializable]
    public sealed class CombatContribution
    {
        public string EntityId;
        public BoardSide Side;
        public int DamageEvents;
        public int TriggerEvents;
        public int Summons;
        public string Note;
    }

    [Serializable]
    public sealed class MechanicCoverageReport
    {
        public string Version = "design-validation-v1";
        public List<MechanicCoverageRow> Rows = new List<MechanicCoverageRow>();
    }

    [Serializable]
    public sealed class MechanicCoverageRow
    {
        public string System;
        public bool Configurable;
        public bool CombatConsumed;
        public bool UiVisible;
        public bool TestCovered;
        public string DesignConfidence;
        public string Notes;
    }
}
