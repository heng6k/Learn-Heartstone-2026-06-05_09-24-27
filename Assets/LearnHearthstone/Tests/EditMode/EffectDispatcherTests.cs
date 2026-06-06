using System.Collections.Generic;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class EffectDispatcherTests
    {
        [Test]
        public void Dispatch_CardPlayedAppliesSourceBattlecryBuff()
        {
            var source = TestMinion("source", "battlecry_self_buff_2_2");
            var board = new List<MinionInstance> { source };
            var dispatcher = new EffectDispatcher(MinionEffectCatalog.CreateDefault(), new SeededRng(1));

            dispatcher.Dispatch(new EffectDispatchContext
            {
                EventType = MechanicEventType.CardPlayed,
                Source = source,
                FriendlyBoard = board,
                FriendlyHand = new List<MinionInstance>(),
                FriendlyShop = new List<MinionInstance>()
            });

            Assert.AreEqual(4, source.Attack);
            Assert.AreEqual(4, source.MaxHealth);
            Assert.AreEqual(4, source.Health);
        }

        [Test]
        public void Dispatch_BuffsAllFriendlyHandTargets()
        {
            var source = TestMinion("source", "card_played_buff_hand_1_1");
            var handTarget = TestMinion("hand", "");
            var dispatcher = new EffectDispatcher(MinionEffectCatalog.CreateDefault(), new SeededRng(1));

            dispatcher.Dispatch(new EffectDispatchContext
            {
                EventType = MechanicEventType.CardPlayed,
                Source = source,
                FriendlyBoard = new List<MinionInstance> { source },
                FriendlyHand = new List<MinionInstance> { handTarget },
                FriendlyShop = new List<MinionInstance>()
            });

            Assert.AreEqual(3, handTarget.Attack);
            Assert.AreEqual(3, handTarget.MaxHealth);
        }

        private static MinionInstance TestMinion(string id, string effectId)
        {
            return new MinionInstance
            {
                InstanceId = id,
                DefinitionId = id,
                CardId = id.ToUpperInvariant(),
                Name = id,
                BaseAttack = 2,
                BaseHealth = 2,
                Attack = 2,
                Health = 2,
                MaxHealth = 2,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword>(),
                EffectIds = string.IsNullOrEmpty(effectId) ? new List<string>() : new List<string> { effectId },
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>()
            };
        }
    }
}
