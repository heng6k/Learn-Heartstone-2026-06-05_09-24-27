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
        private const string BloodGemCardId = "BLOOD_GEM";
        private const string BristlebackBloodGemCardId = "BRISTLEBACK_BLOOD_GEM";
        private const string ScarletSurvivorCardId = "BG35_814";
        private const string SlimyShieldCardId = "SLIMY_SHIELD";
        private const string ReefRifferSpellCardId = "REEF_RIFFER_SPELL";
        private const string SurfNSurfSpellCardId = "SURF_N_SURF_SPELL";
        private const string LockedTurnsCounter = "locked-turns";

        public static string Cast(MinionInstance spell, MatchState state, MinionCatalog minions, SpellCatalog spells, SeededRng rng)
        {
            if (spell == null || (spell.CardKind != CardKind.TavernSpell && spell.CardKind != CardKind.Spell))
            {
                throw new InvalidOperationException("Target card is not a spell.");
            }

            var cardNumber = spell.CardId;
            var applyTavernSpellBonus = spell.CardKind == CardKind.TavernSpell;
            switch (cardNumber)
            {
                case BloodGemCardId:
                    Buff(state, FirstFriendlyBoard(state), 1, 1, "鲜血宝石", applyTavernSpellBonus);
                    return "鲜血宝石：目标随从获得+1/+1";
                case BristlebackBloodGemCardId:
                    var gemTarget = FirstFriendlyBoard(state);
                    Buff(state, gemTarget, 1, 1, "Bristleback Blood Gem", applyTavernSpellBonus);
                    if (gemTarget != null && gemTarget.Tribes.Contains(Tribe.Quilboar))
                    {
                        AddKeyword(gemTarget, Keyword.Taunt);
                    }

                    return "Bristleback Blood Gem: target gains +1/+1 and Quilboar gain Taunt";
                case ReefRifferSpellCardId:
                    var reefMultiplier = spell.Counters != null && spell.Counters.TryGetValue("spellcraft_multiplier", out var storedMultiplier) ? Math.Max(1, storedMultiplier) : 1;
                    var reefAmount = Math.Max(1, state.Player.Tavern.Tier) * reefMultiplier;
                    Buff(state, FirstAnyMinion(state), reefAmount, reefAmount, "Reef Riffer Spellcraft", false);
                    return "Reef Riffer Spellcraft: target gains +" + reefAmount + "/+" + reefAmount;
                case SurfNSurfSpellCardId:
                    var surfTarget = FirstAnyMinion(state);
                    AddKeyword(surfTarget, Keyword.Deathrattle);
                    AddTag(surfTarget, "surf_n_surf_crab");
                    if (surfTarget != null)
                    {
                        surfTarget.Counters["surf_crab_attack"] = spell.Counters != null && spell.Counters.TryGetValue("crab_attack", out var crabAttack) ? crabAttack : 3;
                        surfTarget.Counters["surf_crab_health"] = spell.Counters != null && spell.Counters.TryGetValue("crab_health", out var crabHealth) ? crabHealth : 2;
                    }

                    return "Surf n' Surf Spellcraft: target gains a Crab Deathrattle";
                case SlimyShieldCardId:
                    var shieldTarget = FirstAnyMinion(state);
                    Buff(state, shieldTarget, 1, 1, "黏黏盾", applyTavernSpellBonus);
                    AddKeyword(shieldTarget, Keyword.Taunt);
                    return "黏黏盾：目标随从获得+1/+1和嘲讽";
                case "100596":
                    Buff(state, FirstAnyMinion(state), 4, 0, "尖利箭矢", applyTavernSpellBonus);
                    return "尖利箭矢：目标随从获得+4攻击力";
                case "103791":
                    Buff(state, FirstAnyMinion(state), 0, 3, "强固", applyTavernSpellBonus);
                    AddKeyword(FirstAnyMinion(state), Keyword.Taunt);
                    return "强固：目标随从获得+3生命值和嘲讽";
                case "105752":
                    Buff(state, FirstAnyMinion(state), 2, 2, "香蕉果盘", applyTavernSpellBonus);
                    return "香蕉果盘：目标随从获得+2/+2";
                case "103796":
                    AddKeyword(FirstAnyMinion(state), Keyword.DivineShield);
                    return "神圣赠礼：目标随从获得圣盾";
                case "104601":
                    SetStats(FirstAnyMinion(state), 20, 20);
                    return "完美形象：目标随从变为20/20";
                case "104445":
                    Buff(state, FirstFriendlyBoard(state), 6, 6, "防御者的仪式", applyTavernSpellBonus);
                    AddKeyword(FirstFriendlyBoard(state), Keyword.Taunt);
                    return "防御者的仪式：友方随从获得+6/+6和嘲讽";
                case "105667":
                    var pantsTarget = FirstAnyMinion(state);
                    Buff(state, pantsTarget, 1, 2, "搞怪裤", applyTavernSpellBonus);
                    ToggleKeyword(pantsTarget, Keyword.Taunt);
                    return "搞怪裤：目标随从获得+1/+2并切换嘲讽";
                case "104436":
                    GainGold(state.Player.Tavern, 1);
                    return "酒馆币：获得1枚铸币";
                case "104029":
                    state.Player.Tavern.MaxGold += 1;
                    return "钻探原油：铸币上限提高1";
                case "104446":
                    state.Player.Tavern.FreeRefreshes += 2;
                    return "快速浏览：获得2次免费的刷新";
                case "104559":
                    GainGold(state.Player.Tavern, 1);
                    return "拼命发掘：获得1枚铸币";
                case "127288":
                    StartLockedCurrentTierDiscover(state, minions, rng, "搜寻时光");
                    return "搜寻时光：发现当前等级随从，并锁入手牌1个回合";
                case "105664":
                    AddSameTribeMinionToHand(state, minions, rng, FirstAnyMinion(state), "主厨甄选");
                    return "主厨甄选：获取相同类型的另一张随从牌";
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
                    BuffAll(state, state.Player.Board, 1, 1, "闪亮的戒指", applyTavernSpellBonus);
                    return "闪亮的戒指：你的随从获得+1/+1";
                case "109232":
                    BuffAll(state, state.Player.Board, 4, 4, "艾泽里特强化", applyTavernSpellBonus);
                    return "艾泽里特强化：你的随从获得+4/+4";
                case "127506":
                    BuffAll(state, state.Player.Board, 3, 2, "黄金狂潮", applyTavernSpellBonus);
                    BuffAll(state, state.Player.Board.Where(minion => minion.Golden), 3, 2, "黄金狂潮-金色", applyTavernSpellBonus);
                    return "黄金狂潮：你的随从获得+3/+2，金色随从额外获得+3/+2";
                case "105271":
                    BuffOneOfEachTribe(state, state.Player.Board, 2, 2, "乱放的茶具", applyTavernSpellBonus);
                    return "乱放的茶具：每个类型各一个友方随从获得+2/+2";
                case "104472":
                    BuffSameTribeAsTarget(state, state.Player.Board, FirstFriendlyBoard(state), 3, 3, "自然祝福", applyTavernSpellBonus);
                    return "自然祝福：同类型友方随从获得+3/+3";
                case "105903":
                    BuffAll(state, state.Player.Tavern.Shop.Where(card => card != null && card.CardKind == CardKind.Minion), 1, 2, "意外之果", applyTavernSpellBonus);
                    return "意外之果：酒馆随从获得+1/+2";
                case "105276":
                    BuffAll(state, state.Player.Tavern.Shop.Where(card => card != null && card.CardKind == CardKind.Minion), 2, 2, "富足之杖", applyTavernSpellBonus);
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
            return FirstFriendlyBoard(state) ?? state.Player.Tavern.Shop.FirstOrDefault(card => card != null && card.CardKind == CardKind.Minion);
        }

        private static MinionInstance FirstFriendlyBoard(MatchState state)
        {
            return state.Player.Board.FirstOrDefault();
        }

        private static MinionInstance RandomShopMinion(MatchState state, SeededRng rng)
        {
            var candidates = state.Player.Tavern.Shop.Where(card => card != null && card.CardKind == CardKind.Minion).ToList();
            return candidates.Count == 0 ? null : rng.Pick(candidates);
        }

        private static void Buff(MatchState state, MinionInstance target, int attack, int health, string sourceId, bool applyTavernSpellBonus)
        {
            if (target == null)
            {
                return;
            }

            if (applyTavernSpellBonus && (attack != 0 || health != 0))
            {
                attack += state.Player.Tavern.TavernSpellBonusAttack;
                health += state.Player.Tavern.TavernSpellBonusHealth;
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
            RefreshScarletSurvivor(target);
        }

        private static void BuffAll(MatchState state, IEnumerable<MinionInstance> targets, int attack, int health, string sourceId, bool applyTavernSpellBonus)
        {
            foreach (var target in targets.Where(target => target != null))
            {
                Buff(state, target, attack, health, sourceId, applyTavernSpellBonus);
            }
        }

        private static void BuffOneOfEachTribe(MatchState state, IEnumerable<MinionInstance> targets, int attack, int health, string sourceId, bool applyTavernSpellBonus)
        {
            var seen = new HashSet<Tribe>();
            foreach (var target in targets.Where(target => target != null))
            {
                var tribe = target.Tribes.FirstOrDefault(candidate => candidate != Tribe.None && candidate != Tribe.All);
                if (tribe == Tribe.None || !seen.Add(tribe))
                {
                    continue;
                }

                Buff(state, target, attack, health, sourceId, applyTavernSpellBonus);
            }
        }

        private static void BuffSameTribeAsTarget(MatchState state, IEnumerable<MinionInstance> board, MinionInstance target, int attack, int health, string sourceId, bool applyTavernSpellBonus)
        {
            if (target == null)
            {
                return;
            }

            var tribes = target.Tribes.Where(tribe => tribe != Tribe.None).ToList();
            BuffAll(state, board.Where(minion => minion.Tribes.Any(tribes.Contains)), attack, health, sourceId, applyTavernSpellBonus);
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
            RefreshScarletSurvivor(target);
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
            tavern.Gold += amount;
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

        private static void StartLockedCurrentTierDiscover(MatchState state, MinionCatalog catalog, SeededRng rng, string source)
        {
            var exactTier = Math.Max(1, state.Player.Tavern.Tier);
            var candidates = catalog.All.Where(minion => minion.InPool && minion.TavernTier == exactTier).ToList();
            var options = new List<MinionInstance>();
            while (options.Count < 3 && candidates.Count > 0)
            {
                var index = rng.NextInt(candidates.Count);
                var definition = candidates[index];
                candidates.RemoveAt(index);
                var option = MinionFactory.Create(definition, BoardSide.Player, "discover-" + source + "-" + options.Count, false, PoolSource.Discover, 0);
                option.Counters[LockedTurnsCounter] = 1;
                AddTag(option, "locked_in_hand");
                options.Add(option);
            }

            state.Player.Tavern.Discover = new DiscoverState
            {
                Source = source,
                RewardTier = exactTier,
                Options = options
            };
        }

        private static void AddSameTribeMinionToHand(MatchState state, MinionCatalog catalog, SeededRng rng, MinionInstance target, string source)
        {
            if (target == null || state.Player.Tavern.Hand.Count >= HandLimit)
            {
                return;
            }

            var tribes = target.Tribes.Where(tribe => tribe != Tribe.None && tribe != Tribe.All).ToList();
            if (tribes.Count == 0)
            {
                return;
            }

            var candidates = catalog.All
                .Where(minion => minion.InPool && minion.CardId != target.CardId && minion.Tribes.Any(tribes.Contains))
                .ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            state.Player.Tavern.Hand.Add(MinionFactory.Create(rng.Pick(candidates), BoardSide.Player, "spell-" + source + "-" + state.Round, false, PoolSource.Copy, 0));
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
            RefreshScarletSurvivor(target);
        }

        private static void RefreshScarletSurvivor(MinionInstance target)
        {
            if (target != null && target.CardId == ScarletSurvivorCardId && target.Attack >= 6 && !target.Keywords.Contains(Keyword.DivineShield))
            {
                target.Keywords.Add(Keyword.DivineShield);
            }
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

        private static void AddTag(MinionInstance target, string tag)
        {
            if (target != null && !target.Tags.Contains(tag))
            {
                target.Tags.Add(tag);
            }
        }
    }
}
