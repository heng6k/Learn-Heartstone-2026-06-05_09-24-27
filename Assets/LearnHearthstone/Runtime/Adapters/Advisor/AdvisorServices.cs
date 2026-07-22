using System.Collections.Generic;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Adapters.Advisor
{
    public interface IAdvisorService
    {
        List<string> GetAdvice(MatchState state, bool useEnglish = false);
    }

    public sealed class LocalAdvisorService : IAdvisorService
    {
        public List<string> GetAdvice(MatchState state, bool useEnglish = false)
        {
            var advice = new List<string>();
            if (state.Player.Tavern.Gold < 3)
            {
                advice.Add(useEnglish
                    ? "You have less than 3 Gold. Consider freezing or ending the turn."
                    : "金币不足 3，优先评估是否冻结或结束回合。");
            }

            if (state.Player.Tavern.Shop.Exists(minion => minion != null) && state.Player.Tavern.Gold >= 3)
            {
                advice.Add(useEnglish
                    ? "You can buy a minion. Fill your board or look for triple pieces."
                    : "当前可以买随从，优先补齐战场或寻找三连材料。");
            }

            if (state.Player.Board.Count >= 7)
            {
                advice.Add(useEnglish
                    ? "Your board is full. Sell or rearrange before playing another minion."
                    : "战场已满，打出新随从前需要先出售或调整阵容。");
            }

            if (advice.Count == 0)
            {
                advice.Add(useEnglish
                    ? "Your position is stable. Refresh or upgrade based on your current plan."
                    : "局面稳定，可以根据搜索目标决定刷新或升本。");
            }

            return advice;
        }
    }
}
