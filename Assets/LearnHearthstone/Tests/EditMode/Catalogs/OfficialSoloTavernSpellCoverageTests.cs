using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Adapters.Data;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class OfficialSoloTavernSpellCoverageTests
    {
        private static readonly string[] OfficialSoloTavernSpellIds =
        {
            "100596", "100601", "100899", "100910", "100911", "103779", "103785", "103791",
            "103793", "103796", "104029", "104436", "104445", "104446", "104448", "104472",
            "104494", "104502", "104559", "104560", "104601", "105264", "105265", "105267",
            "105271", "105276", "105664", "105665", "105667", "105669", "105752", "105903",
            "109230", "109232", "110400", "110401", "110406", "110407", "110412", "110642",
            "113901", "113902", "117573", "117670", "119599", "119718", "120900", "122182",
            "122183", "122184", "122185", "122186", "122862", "122864", "122899", "126676",
            "126909", "126957", "127288", "127503", "127506", "130310", "130311", "130312",
            "130527", "130713", "131152", "131153", "131218"
        };

        private static readonly string[] BoardOnlyTargetSpellIds =
        {
            "100601", "100899", "104445", "110407", "110412", "119603", "122862", "130311"
        };

        private static readonly string[] BoardOrTavernTargetSpellIds =
        {
            "100596", "100911", "103791", "103796", "104472", "104601", "105664", "105667",
            "105752", "110642", "113901", "120900", "130310", "130312", "131153", "131218"
        };

        [Test]
        public void SpellCatalog_ContainsEveryOfficialSoloTavernSpellAndDocumentsLegacyExtras()
        {
            var official = OfficialSoloTavernSpellIds.OrderBy(id => id).ToList();
            var local = SpellCatalogLoader.LoadFromResources().All
                .Where(spell => spell.InPool && spell.Category == "TavernSpell")
                .Select(spell => spell.CardNumber)
                .OrderBy(id => id)
                .ToList();

            CollectionAssert.IsSubsetOf(official, local);
            Assert.AreEqual(69, official.Count);
            Assert.AreEqual(73, local.Count);
            Assert.AreEqual(local.Count, local.Distinct().Count());
            Assert.AreEqual(new[] { "119603", "122489", "123553", "127642" }, local.Except(official).OrderBy(id => id).ToArray());

            var byId = SpellCatalogLoader.LoadFromResources().All.ToDictionary(spell => spell.CardNumber);
            Assert.AreEqual(3, byId["105276"].TavernTier, "Pinned build 247416: Staff of Enrichment is Tier 3.");
            Assert.AreEqual(1, byId["120900"].Cost, "Pinned build 247416: Shifting Tide costs 1 Gold.");
        }

        [Test]
        public void OfficialSoloTavernSpells_AllResolveWithoutDefaultFallback()
        {
            var spellDefinitions = SpellCatalogLoader.LoadFromResources().All
                .Where(spell => spell.InPool && spell.Category == "TavernSpell")
                .OrderBy(spell => spell.CardNumber)
                .ToList();

            foreach (var spell in spellDefinitions)
            {
                var spellId = spell.CardNumber;
                var service = PreparedService(9700 + Math.Abs(spellId.GetHashCode()));
                service.Apply(new GameCommand(GameCommandType.AddCardToHand, spellId, CardKind.TavernSpell));
                var handIndex = service.State.Player.Tavern.Hand.Count - 1;
                var handCard = service.State.Player.Tavern.Hand[handIndex];
                var command = BuildPlayCommand(service, handIndex, handCard);

                Assert.DoesNotThrow(
                    () => service.Apply(command),
                    spellId + " should resolve in single-player training mode.");
                var message = service.State.Player.Tavern.RecruitLog.Last().Message ?? string.Empty;
                Assert.Less(
                    message.IndexOf("effect is not implemented yet", StringComparison.OrdinalIgnoreCase),
                    0,
                    spellId);
                Assert.IsFalse(service.State.Player.Tavern.RecruitLog.Last().Message.Contains("暂未实现"), spellId);
            }
        }

        [Test]
        public void SpellCatalog_TargetPolicyClassifiesAllSeventyThreeSpells()
        {
            var boardOnly = new HashSet<string>(BoardOnlyTargetSpellIds, StringComparer.OrdinalIgnoreCase);
            var boardOrTavern = new HashSet<string>(BoardOrTavernTargetSpellIds, StringComparer.OrdinalIgnoreCase);
            Assert.AreEqual(0, boardOnly.Intersect(boardOrTavern).Count());
            Assert.AreEqual(24, boardOnly.Count + boardOrTavern.Count);

            var definitions = SpellCatalogLoader.LoadFromResources().All
                .Where(spell => spell.InPool && spell.Category == "TavernSpell")
                .OrderBy(spell => spell.CardNumber)
                .ToList();
            Assert.AreEqual(73, definitions.Count);

            foreach (var definition in definitions)
            {
                var spell = new MinionInstance
                {
                    CardId = definition.CardNumber,
                    CardKind = CardKind.TavernSpell,
                    Name = definition.Name,
                    Tags = definition.Tags == null ? new List<string>() : definition.Tags.ToList()
                };
                var expectsTarget = boardOnly.Contains(definition.CardNumber) || boardOrTavern.Contains(definition.CardNumber);

                Assert.AreEqual(expectsTarget, TavernSpellEngine.TargetsFriendlyMinion(spell), definition.CardNumber);
                Assert.AreEqual(boardOrTavern.Contains(definition.CardNumber), TavernSpellEngine.CanTargetTavernMinion(spell), definition.CardNumber);
            }
        }

        [Test]
        public void SpellCatalog_ExplicitTargetZonesMatchPinnedWording()
        {
            foreach (var spellId in BoardOnlyTargetSpellIds)
            {
                var service = PreparedService(9800 + int.Parse(spellId) % 1000);
                service.Apply(new GameCommand(GameCommandType.AddCardToHand, spellId, CardKind.TavernSpell));
                var spell = service.State.Player.Tavern.Hand.Last();
                var command = BuildShopPlayCommand(service, service.State.Player.Tavern.Hand.Count - 1, spell);

                Assert.Throws<InvalidOperationException>(() => service.Apply(command), spellId);
                Assert.AreSame(spell, service.State.Player.Tavern.Hand.Last(), spellId);
            }

            foreach (var spellId in BoardOrTavernTargetSpellIds)
            {
                var service = PreparedService(9900 + int.Parse(spellId) % 1000);
                service.Apply(new GameCommand(GameCommandType.AddCardToHand, spellId, CardKind.TavernSpell));
                var handIndex = service.State.Player.Tavern.Hand.Count - 1;
                var command = BuildShopPlayCommand(service, handIndex, service.State.Player.Tavern.Hand[handIndex]);

                Assert.DoesNotThrow(() => service.Apply(command), spellId);
            }
        }

        private static GameCommand BuildPlayCommand(MatchService service, int handIndex, MinionInstance spell)
        {
            var boardTarget = service.State.Player.Board
                .Select((card, index) => new { Card = card, Index = index })
                .FirstOrDefault(item => item.Card != null &&
                                        item.Card.CardKind == CardKind.Minion &&
                                        TavernSpellEngine.IsLegalFriendlyMinionTarget(spell, item.Card) &&
                                        (TavernSpellEngine.TargetsFriendlyMinion(spell) || TavernSpellEngine.CanTargetTavernMinion(spell)));
            if (boardTarget != null)
            {
                return new GameCommand(
                    GameCommandType.PlayMinion,
                    handIndex,
                    boardTarget.Index,
                    TargetZone.FriendlyBoard,
                    -1,
                    TargetZone.Unspecified,
                    boardTarget.Card.InstanceId);
            }

            var shopTarget = service.State.Player.Tavern.Shop
                .Select((card, index) => new { Card = card, Index = index })
                .FirstOrDefault(item => item.Card != null &&
                                        item.Card.CardKind == CardKind.Minion &&
                                        TavernSpellEngine.IsLegalFriendlyMinionTarget(spell, item.Card) &&
                                        (TavernSpellEngine.TargetsFriendlyMinion(spell) || TavernSpellEngine.CanTargetTavernMinion(spell)));
            if (shopTarget != null)
            {
                return new GameCommand(
                    GameCommandType.PlayMinion,
                    handIndex,
                    shopTarget.Index,
                    TargetZone.TavernShop,
                    -1,
                    TargetZone.Unspecified,
                    shopTarget.Card.InstanceId);
            }

            return new GameCommand(GameCommandType.PlayMinion, handIndex);
        }

        private static GameCommand BuildShopPlayCommand(MatchService service, int handIndex, MinionInstance spell)
        {
            var shopTarget = service.State.Player.Tavern.Shop
                .Select((card, index) => new { Card = card, Index = index })
                .FirstOrDefault(item => item.Card != null &&
                                        item.Card.CardKind == CardKind.Minion &&
                                        TavernSpellEngine.IsLegalFriendlyMinionTarget(spell, item.Card));
            Assert.IsNotNull(shopTarget, spell.CardId + " needs a legal Tavern fixture target.");
            return new GameCommand(
                GameCommandType.PlayMinion,
                handIndex,
                shopTarget.Index,
                TargetZone.TavernShop,
                -1,
                TargetZone.Unspecified,
                shopTarget.Card.InstanceId);
        }

        private static MatchService PreparedService(int seed)
        {
            var service = MatchService.CreateWithDefaultCatalog(seed, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.Tier = 7;
            service.State.Player.Tavern.Gold = 20;
            service.State.Player.Tavern.MaxGold = 20;
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();

            service.State.Player.Board.Add(TestMinion("p-elemental", BoardSide.Player, 8, 8, Tribe.Elemental, Keyword.DivineShield));
            service.State.Player.Board.Add(TestMinion("p-murloc", BoardSide.Player, 5, 7, Tribe.Murloc));
            service.State.Player.Board.Add(TestMinion("p-naga", BoardSide.Player, 4, 9, Tribe.Naga));
            service.State.Player.Board.Add(TestMinion("p-undead", BoardSide.Player, 3, 6, Tribe.Undead, Keyword.Deathrattle));
            service.State.Player.Board.Add(TestMinion("p-dragon", BoardSide.Player, 6, 6, Tribe.Dragon));
            service.State.Player.Board.Add(TestMinion("p-demon", BoardSide.Player, 7, 7, Tribe.Demon));
            service.State.Player.Tavern.Shop.Clear();
            service.State.Player.Tavern.Shop.Add(TestMinion("shop-beast", BoardSide.Player, 4, 4, Tribe.Beast));
            service.State.Player.Tavern.Shop.Add(TestMinion("shop-elemental", BoardSide.Player, 5, 5, Tribe.Elemental));
            service.State.Player.Tavern.Shop.Add(TestMinion("shop-undead", BoardSide.Player, 3, 3, Tribe.Undead));
            service.State.Player.Tavern.Shop.Add(TestMinion("shop-demon", BoardSide.Player, 6, 6, Tribe.Demon));
            service.State.Player.Tavern.Shop.Add(TestMinion("shop-murloc", BoardSide.Player, 2, 2, Tribe.Murloc));
            service.State.Opponent.Board.Add(TestMinion("o-nearest", BoardSide.Opponent, 11, 13, Tribe.None));
            return service;
        }

        private static MinionInstance TestMinion(string id, BoardSide owner, int attack, int health, Tribe tribe, params Keyword[] keywords)
        {
            return new MinionInstance
            {
                InstanceId = id,
                DefinitionId = id,
                CardId = id.ToUpperInvariant(),
                Name = id,
                CardKind = CardKind.Minion,
                Attack = attack,
                BaseAttack = attack,
                Health = health,
                MaxHealth = health,
                BaseHealth = health,
                Owner = owner,
                TavernTier = 1,
                Tribes = new List<Tribe> { tribe },
                Keywords = keywords.ToList(),
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                EffectIds = new List<string>(),
                Tags = new List<string>(),
                CanAttack = true
            };
        }
    }
}
