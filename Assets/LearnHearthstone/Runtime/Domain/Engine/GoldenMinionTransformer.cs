using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Domain.Engine
{
    public static class GoldenMinionTransformer
    {
        public static bool MakeGoldenInPlace(MinionInstance target, MinionCatalog catalog)
        {
            if (target == null)
            {
                return false;
            }

            if (target.Golden)
            {
                catalog?.TrySyncGoldenText(target);
                return false;
            }

            ResolveBaseStats(target, catalog, out var normalAttack, out var normalHealth, out var goldenAttack, out var goldenHealth);
            var attackDelta = StatMath.SaturatingDelta(goldenAttack, normalAttack);
            var healthDelta = StatMath.SaturatingDelta(goldenHealth, normalHealth);

            target.Golden = true;
            target.BaseAttack = goldenAttack;
            target.BaseHealth = goldenHealth;
            target.Attack = StatMath.SaturatingAdd(target.Attack, attackDelta, 0, StatMath.MaxStat);
            target.MaxHealth = StatMath.SaturatingAdd(target.MaxHealth, healthDelta, 1, StatMath.MaxStat);
            target.Health = StatMath.SaturatingAdd(target.Health, healthDelta, int.MinValue, StatMath.MaxStat);
            StatMath.ClampCurrentHealthToMax(target);
            catalog?.TrySyncGoldenText(target);
            return true;
        }

        private static void ResolveBaseStats(
            MinionInstance target,
            MinionCatalog catalog,
            out int normalAttack,
            out int normalHealth,
            out int goldenAttack,
            out int goldenHealth)
        {
            MinionDefinition definition = null;
            if (catalog != null)
            {
                catalog.TryGetById(target.DefinitionId, out definition);
                if (definition == null)
                {
                    catalog.TryGetByCardId(target.CardId, out definition);
                }
            }

            normalAttack = target.BaseHealth > 0
                ? target.BaseAttack
                : definition?.BaseAttack ?? target.Attack;
            normalHealth = target.BaseHealth > 0
                ? target.BaseHealth
                : definition?.BaseHealth ?? target.MaxHealth;

            var usesPrintedBase = definition != null &&
                                  normalAttack == definition.BaseAttack &&
                                  normalHealth == definition.BaseHealth;
            goldenAttack = usesPrintedBase && definition.Golden != null
                ? definition.Golden.BaseAttack
                : StatMath.SaturatingMultiply(normalAttack, 2, 0, StatMath.MaxStat);
            goldenHealth = usesPrintedBase && definition.Golden != null
                ? definition.Golden.BaseHealth
                : StatMath.SaturatingMultiply(normalHealth, 2, 1, StatMath.MaxStat);
        }
    }
}
