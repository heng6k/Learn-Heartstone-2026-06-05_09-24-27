using System;
using LearnHearthstone.Domain.Data;

namespace LearnHearthstone.Application.Content
{
    public sealed class GameCatalogSet
    {
        public GameCatalogSet(
            MinionCatalog minions,
            SpellCatalog spells,
            HeroCatalog heroes,
            TrinketCatalog trinkets,
            QuestCatalog quests,
            TimewarpedTavernCatalog timewarpedTavern,
            AnomalyCatalog anomalies,
            DarkmoonPrizeCatalog darkmoonPrizes)
        {
            Minions = minions ?? throw new ArgumentNullException(nameof(minions));
            Spells = spells ?? throw new ArgumentNullException(nameof(spells));
            Heroes = heroes ?? throw new ArgumentNullException(nameof(heroes));
            Trinkets = trinkets ?? throw new ArgumentNullException(nameof(trinkets));
            Quests = quests ?? throw new ArgumentNullException(nameof(quests));
            TimewarpedTavern = timewarpedTavern ?? throw new ArgumentNullException(nameof(timewarpedTavern));
            Anomalies = anomalies ?? throw new ArgumentNullException(nameof(anomalies));
            DarkmoonPrizes = darkmoonPrizes ?? throw new ArgumentNullException(nameof(darkmoonPrizes));
        }

        public MinionCatalog Minions { get; }
        public SpellCatalog Spells { get; }
        public HeroCatalog Heroes { get; }
        public TrinketCatalog Trinkets { get; }
        public QuestCatalog Quests { get; }
        public TimewarpedTavernCatalog TimewarpedTavern { get; }
        public AnomalyCatalog Anomalies { get; }
        public DarkmoonPrizeCatalog DarkmoonPrizes { get; }
    }
}
