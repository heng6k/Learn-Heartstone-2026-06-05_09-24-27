using System;
using System.Collections.Generic;

namespace LearnHearthstone.Domain.Models
{
    public enum DarkmoonPrizeImplementationStatus
    {
        Implemented,
        Proxy
    }

    [Serializable]
    public sealed class DarkmoonPrizeDefinition
    {
        public string CardId;
        public int DbfId;
        public string SourceName;
        public string Name;
        public string Text;
        public int Tier;
        public string ImagePath;
        public string ImageUrl;
        public DarkmoonPrizeImplementationStatus ImplementationStatus;
        public List<Keyword> Keywords = new List<Keyword>();
        public List<string> EffectIds = new List<string>();
        public List<string> Tags = new List<string>();
        public string SourcePool;
    }
}
