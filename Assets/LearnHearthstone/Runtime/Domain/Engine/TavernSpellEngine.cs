using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Domain.Engine
{
    public static class TavernSpellEngine
    {
        private const int HandLimit = 10;

        public static string Cast(MinionInstance spell, MatchState state, MinionCatalog minions, SpellCatalog spells, SeededRng rng)
        {
            if (spell == null || spell.CardKind != CardKind.TavernSpell)
            {
                throw new InvalidOperationException("Target card is not a tavern spell.");
            }

            var cardNumber = spell.CardId;
            switch (cardNumber)
            {
                case "100596":
                    Buff(FirstAnyMinion(state), 4, 0, "尖利箭矢");
                    return "尖利箭矢：目标随从获得+4攻击力";
                case "103791":
                    Buff(FirstAnyMinion(state), 0, 3, "强固");
                    AddKeyword(FirstAnyMinion(state), Keyword.Taunt);
                    return "强固：目标随从获得+3生命值和嘲讽";
                case "105752":
                    Buff(FirstAnyMinion(state), 2, 2, "香蕉果盘");
                    return "香蕉果盘：目标随从获得+2/+2";
                case "103796":
                    AddKeyword(FirstAnyMinion(state), Keyword.DivineShield);
                    return "神圣赠礼：目标随从获得圣盾";
                case "104601":
                    SetStats(FirstAnyMinion(state), 20, 20);
                    return "完美形象：目标随从变为20/20";
                case "104445":
                    Buff(FirstFriendlyBoard(state), 6, 6, "防御者的仪式");
                    AddKeyword(FirstFriendlyBoard(state), Keyword.Taunt);
                    return "防御者的仪式：友方随从获得+6/+6和嘲讽";
                case "105667":
                    var pantsTarget = FirstAnyMinion(state);
                    Buff(pantsTarget, 1, 2, "搞怪裤");
                    ToggleKeyword(pantsTarget, Keyword.Taunt);
                    return "搞怪裤：目标随从获得+1/+2并切换嘲讽";
                case "104436":
                    GainGold(state.Player.Tavern, 1);
                    return "酒馆币：获得1枚铸币";
                case "104029":
                    state.Player.Tavern.MaxGold += 1;
                    return "钻探原油：铸币上限提高1";
                case "103785":
                    state.Player.Armor = 5;
                    return "护甲储备：护甲变为5";
                case "103793":
                    AddRandomMinionToHand(state, minions, rng, 1, "招募新人");
                    return "招募新人：获取等级1随从";
                case "122864":
                    StartDiscover(state, minions, rng, 1, "新生幼苗");
                    return "新生幼苗：发现等级1随从";
                case "119718":
                    StartDiscover(state, minions, rng, 7, "降圣仪式");
                    return "降圣仪式：发现等级7随从";
                case "109230":
                    BuffAll(state.Player.Board, 1, 1, "闪亮的戒指");
                    return "闪亮的戒指：你的随从获得+1/+1";
                case "109232":
                    BuffAll(state.Player.Board, 4, 4, "艾泽里特强化");
                    return "艾泽里特强化：你的随从获得+4/+4";
                case "127506":
                    BuffAll(state.Player.Board, 3, 2, "黄金狂潮");
                    BuffAll(state.Player.Board.Where(minion => minion.Golden), 3, 2, "黄金狂潮-金色");
                    return "黄金狂潮：你的随从获得+3/+2，金色随从额外获得+3/+2";
                case "105271":
                    BuffOneOfEachTribe(state.Player.Board, 2, 2, "乱放的茶具");
                    return "乱放的茶具：每个类型各一个友方随从获得+2/+2";
                case "104472":
                    BuffSameTribeAsTarget(state.Player.Board, FirstFriendlyBoard(state), 3, 3, "自然祝福");
                    return "自然祝福：同类型友方随从获得+3/+3";
                case "105903":
                    BuffAll(state.Player.Tavern.Shop.Where(card => card.CardKind == CardKind.Minion), 1, 2, "意外之果");
                    return "意外之果：酒馆随从获得+1/+2";
                case "105276":
                    BuffAll(state.Player.Tavern.Shop.Where(card => card.CardKind == CardKind.Minion), 2, 2, "富足之杖");
                    return "富足之杖：酒馆随从获得+2/+2";
                case "104448":
                    MakeGolden(RandomShopMinion(state, rng));
                    return "点金之触：随机酒馆随从变为金色";
                case "104502":
                    StealRandomShopMinion(state, rng);
                    return "附魔链索：随机偷取酒馆随从";
                case "110407":
                    AddSpellcraftBundleToHand(state, spells, rng);
                    return "恶鳞套餐：获取3张塑造法术";
                case "126676":
                    state.Player.Tavern.Growth.ShopModifiers.Add(new TavernGrowthModifier
                    {
                        Scope = BuffScope.ShopGlobal,
                        Tribe = Tribe.All,
                        Attack = 1,
                        Health = 1,
                        SourceId = "鲜血宝石弹幕"
                    });
                    return "鲜血宝石弹幕：后续酒馆刷新获得+1/+1";
                default:
                    return spell.Name + "：暂未实现具体效果";
            }
        }

        private static MinionInstance FirstAnyMinion(MatchState state)
        {
            return FirstFriendlyBoard(state) ?? state.Player.Tavern.Shop.FirstOrDefault(card => card.CardKind == CardKind.Minion);
        }

        private static MinionInstance FirstFriendlyBoard(MatchState state)
        {
            return state.Player.Board.FirstOrDefault();
        }

        private static MinionInstance RandomShopMinion(MatchState state, SeededRng rng)
        {
            var candidates = state.Player.Tavern.Shop.Where(card => card.CardKind == CardKind.Minion).ToList();
            return candidates.Count == 0 ? null : rng.Pick(candidates);
        }

        private static void Buff(MinionInstance target, int attack, int health, string sourceId)
        {
            if (target == null)
            {
                return;
            }

            target.Attack += attack;
            target.MaxHealth += health;
            target.Health += health;
            target.Enchantments.Add(new Enchantment
            {
                Id = sourceId,
                SourceId = sourceId,
                AttackBonus = attack,
                HealthBonus = health
            });
        }

        private static void BuffAll(IEnumerable<MinionInstance> targets, int attack, int health, string sourceId)
        {
            foreach (var target in targets.Where(target => target != null))
            {
                Buff(target, attack, health, sourceId);
            }
        }

        private static void BuffOneOfEachTribe(IEnumerable<MinionInstance> targets, int attack, int health, string sourceId)
        {
            var seen = new HashSet<Tribe>();
            foreach (var target in targets.Where(target => target != null))
            {
                var tribe = target.Tribes.FirstOrDefault(candidate => candidate != Tribe.None && candidate != Tribe.All);
                if (tribe == Tribe.None || !seen.Add(tribe))
                {
                    continue;
                }

                Buff(target, attack, health, sourceId);
            }
        }

        private static void BuffSameTribeAsTarget(IEnumerable<MinionInstance> board, MinionInstance target, int attack, int health, string sourceId)
        {
            if (target == null)
            {
                return;
            }

            var tribes = target.Tribes.Where(tribe => tribe != Tribe.None).ToList();
            BuffAll(board.Where(minion => minion.Tribes.Any(tribes.Contains)), attack, health, sourceId);
        }

        private static void SetStats(MinionInstance target, int attack, int health)
        {
            if (target == null)
            {
                return;
            }

            target.Attack = attack;
            target.MaxHealth = health;
            target.Health = health;
        }

        private static void AddKeyword(MinionInstance target, Keyword keyword)
        {
            if (target != null && !target.Keywords.Contains(keyword))
            {
                target.Keywords.Add(keyword);
            }
        }

        private static void ToggleKeyword(MinionInstance target, Keyword keyword)
        {
            if (target == null)
            {
                return;
            }

            if (target.Keywords.Contains(keyword))
            {
                target.Keywords.Remove(keyword);
                return;
            }

            target.Keywords.Add(keyword);
        }

        private static void GainGold(TavernState tavern, int amount)
        {
            tavern.Gold = Math.Min(tavern.MaxGold, tavern.Gold + amount);
        }

        private static void AddRandomMinionToHand(MatchState state, MinionCatalog catalog, SeededRng rng, int exactTier, string suffix)
        {
            if (state.Player.Tavern.Hand.Count >= HandLimit)
            {
                return;
            }

            var candidates = catalog.All.Where(minion => minion.InPool && minion.TavernTier == exactTier).ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            state.Player.Tavern.Hand.Add(MinionFactory.Create(rng.Pick(candidates), BoardSide.Player, "spell-" + suffix + "-" + state.Round, false, PoolSource.Copy, 0));
        }

        private static void StartDiscover(MatchState state, MinionCatalog catalog, SeededRng rng, int exactTier, string source)
        {
            var candidates = catalog.All.Where(minion => minion.InPool && minion.TavernTier == exactTier).ToList();
            var options = new List<MinionInstance>();
            while (options.Count < 3 && candidates.Count > 0)
            {
                var index = rng.NextInt(candidates.Count);
                var definition = candidates[index];
                candidates.RemoveAt(index);
                options.Add(MinionFactory.Create(definition, BoardSide.Player, "discover-" + source + "-" + options.Count, false, PoolSource.Copy, 0));
            }

            state.Player.Tavern.Discover = new DiscoverState
            {
                Source = source,
                RewardTier = exactTier,
                Options = options
            };
        }

        private static void MakeGolden(MinionInstance target)
        {
            if (target == null || target.Golden)
            {
                return;
            }

            target.Golden = true;
            target.Attack *= 2;
            target.MaxHealth *= 2;
            target.Health *= 2;
        }

        private static void StealRandomShopMinion(MatchState state, SeededRng rng)
        {
            if (state.Player.Tavern.Hand.Count >= HandLimit)
            {
                return;
            }

            var candidates = state.Player.Tavern.Shop
                .Select((card, index) => new { Card = card, Index = index })
                .Where(item => item.Card != null && item.Card.CardKind == CardKind.Minion)
                .ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            var picked = rng.Pick(candidates);
            state.Player.Tavern.Shop[picked.Index] = null;
            picked.Card.Owner = BoardSide.Player;
            picked.Card.PoolSource = PoolSource.Copy;
            state.Player.Tavern.Hand.Add(picked.Card);
        }

        private static void AddSpellcraftBundleToHand(MatchState state, SpellCatalog spells, SeededRng rng)
        {
            var candidates = spells.All.Where(spell => spell.Category == "TavernSpell" && spell.TavernTier <= Math.Max(1, state.Player.Tavern.Tier)).ToList();
            for (var count = 0; count < 3 && state.Player.Tavern.Hand.Count < HandLimit && candidates.Count > 0; count += 1)
            {
                state.Player.Tavern.Hand.Add(MinionFactory.Create(rng.Pick(candidates), BoardSide.Player, "spellcraft-" + state.Round + "-" + count));
            }
        }
    }
}
