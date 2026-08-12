using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Domain.Engine
{
    public static class DarkGiftOfferService
    {
        private static readonly CardPoolAvailability DefaultPoolAvailability = new CardPoolAvailability(null);

        private static readonly HashSet<string> BattlecryAndChooseOneGiftAllowlist =
            new HashSet<string>(new[] { "DG-R13", "DG-R17", "DG-R18", "DG-R27" }, StringComparer.Ordinal);

        private static readonly HashSet<string> NeutralCoreCardIds = new HashSet<string>(new[]
        {
            "BG_LOE_077",   // Brann Bronzebeard
            "BG25_354",     // Titus Rivendare
            "BG26_ICC_901"  // Drakkari Enchanter
        }, StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> StatGiftKeys = new HashSet<string>(new[]
        {
            "DG-R04", "DG-R06", "DG-R07", "DG-R08", "DG-R14", "DG-R15", "DG-R16",
            "DG-R25", "DG-R26", "DG-R30", "DG-R31", "DG-R32", "DG-R33", "DG-R34",
            "DG-R37", "DG-R38", "DG-R39", "DG-R42"
        }, StringComparer.Ordinal);

        public static DarkGiftOfferOption SelectAutomaticOption(
            DarkGiftProfile profile,
            DarkGiftOfferResult offer)
        {
            if (profile?.AutoChoicePolicy != DarkGiftAutoChoicePolicy.FirstOption ||
                offer?.Succeeded != true ||
                offer.Options == null ||
                offer.Options.Count == 0)
            {
                return null;
            }

            return offer.Options[0]?.Clone();
        }

        public static DarkGiftOfferResult CreateOffer(
            DarkGiftOfferRequest request,
            DarkGiftProfile profile,
            MinionCatalog minions,
            IEnumerable<DarkGiftDefinition> gifts,
            CardPoolAvailability minionAvailability = null)
        {
            if (request == null)
            {
                return Failure(null, "dark-gift-offer.invalid-request", "Dark Gift offer request is required.");
            }

            if (profile == null || !profile.Enabled)
            {
                return Failure(request, "dark-gift-offer.disabled", "Dark Gifts are disabled for this ruleset.");
            }

            if (minions == null)
            {
                return Failure(request, "dark-gift-offer.invalid-catalog", "Minion catalog is required.");
            }

            if (!request.IgnoreNormalRoundRestrictions && request.Round < Math.Max(1, profile.NormalEntryStartRound))
            {
                return Failure(request, "dark-gift-offer.not-available", "Dark Gifts are not available this round.");
            }

            var offerCount = request.OfferCount > 0 ? request.OfferCount : profile.OfferCount;
            var pickCount = request.PickCount > 0 ? request.PickCount : profile.PickCount;
            if (offerCount <= 0 || pickCount <= 0 || pickCount > offerCount)
            {
                return Failure(request, "dark-gift-offer.invalid-counts", "Offer and pick counts are invalid.");
            }

            if (!TryResolveTierRange(request, profile, out var minTier, out var maxTier))
            {
                return Failure(request, "dark-gift-offer.tier-range-unavailable", "No Dark Gift minion tier range is configured.");
            }

            var eligibleMinions = minions.All
                .Where(minion => IsEligibleMinion(
                    minion,
                    request,
                    profile,
                    minTier,
                    maxTier,
                    minionAvailability ?? DefaultPoolAvailability))
                .OrderBy(StableMinionId, StringComparer.Ordinal)
                .ToList();
            var eligibleGifts = (gifts ?? Enumerable.Empty<DarkGiftDefinition>())
                .Where(gift => IsEligibleGift(gift, request))
                .OrderBy(StableGiftId, StringComparer.Ordinal)
                .ToList();

            if (eligibleMinions.Count < offerCount || eligibleGifts.Count < offerCount)
            {
                return Failure(request, "dark-gift-offer.insufficient-candidates", "Not enough eligible minions or Dark Gifts are available.");
            }

            var rng = new CursorRng(DeriveSeed(request), request.RngCursor);
            Shuffle(eligibleMinions, rng);
            Shuffle(eligibleGifts, rng);
            var mostCommonTribe = ResolveMostCommonTribe(request.CurrentBoardTribeCounts);
            var requireCommonTribe = profile.CommonTribeGuarantee?.Enabled == true &&
                                     request.Round >= profile.CommonTribeGuarantee.StartRound &&
                                     profile.CommonTribeGuarantee.MinimumOfferCount > 0 &&
                                     mostCommonTribe != Tribe.None;
            var requireNeutralCore = maxTier >= 5 && eligibleMinions.Any(IsNeutralCore);
            var selected = new List<GiftMinionPair>();
            if (!TrySelectPairs(
                    eligibleGifts,
                    eligibleMinions,
                    0,
                    offerCount,
                    mostCommonTribe,
                    requireCommonTribe,
                    requireNeutralCore,
                    new HashSet<string>(StringComparer.Ordinal),
                    selected))
            {
                if (requireNeutralCore)
                {
                    selected.Clear();
                    if (TrySelectPairs(
                            eligibleGifts,
                            eligibleMinions,
                            0,
                            offerCount,
                            mostCommonTribe,
                            requireCommonTribe,
                            false,
                            new HashSet<string>(StringComparer.Ordinal),
                            selected))
                    {
                        return Success(request, pickCount, rng.Cursor, selected);
                    }
                }

                return Failure(
                    request,
                    requireCommonTribe
                        ? "dark-gift-offer.common-tribe-unavailable"
                        : "dark-gift-offer.insufficient-compatible-pairs",
                    "Not enough compatible minion and Dark Gift pairs are available.",
                    rng.Cursor);
            }

            return Success(request, pickCount, rng.Cursor, selected);
        }

        private static DarkGiftOfferResult Success(
            DarkGiftOfferRequest request,
            int pickCount,
            int nextRngCursor,
            List<GiftMinionPair> selected)
        {
            return new DarkGiftOfferResult
            {
                Succeeded = true,
                Code = "dark-gift-offer.created",
                Message = "Dark Gift offer created.",
                SourceKind = request.SourceKind,
                SourceId = request.SourceId,
                GiftPoolProfileId = request.GiftPoolProfileId,
                PickCount = pickCount,
                NextRngCursor = nextRngCursor,
                Options = selected.ConvertAll(pair => CreateOption(pair.Minion, pair.Gift))
            };
        }

        private static bool TryResolveTierRange(
            DarkGiftOfferRequest request,
            DarkGiftProfile profile,
            out int minTier,
            out int maxTier)
        {
            if (request.RequestedTier > 0)
            {
                minTier = request.RequestedTier;
                maxTier = request.RequestedTier;
                return true;
            }

            if (request.MinTier > 0 || request.MaxTier > 0)
            {
                minTier = request.MinTier > 0 ? request.MinTier : request.MaxTier;
                maxTier = request.MaxTier > 0 ? request.MaxTier : request.MinTier;
                if (minTier > maxTier)
                {
                    var swap = minTier;
                    minTier = maxTier;
                    maxTier = swap;
                }
                return minTier > 0;
            }

            var rule = (profile.TierRanges ?? new List<DarkGiftTierRangeRule>())
                .Where(item => item != null && item.FromRound <= request.Round)
                .OrderByDescending(item => item.FromRound)
                .FirstOrDefault();
            minTier = rule?.MinTier ?? 0;
            maxTier = rule?.MaxTier ?? 0;
            return minTier > 0 && maxTier >= minTier;
        }

        private static bool IsEligibleMinion(
            MinionDefinition minion,
            DarkGiftOfferRequest request,
            DarkGiftProfile profile,
            int minTier,
            int maxTier,
            CardPoolAvailability minionAvailability)
        {
            if (minion == null ||
                !minionAvailability.AllowsMinion(minion) ||
                minion.TavernTier < minTier ||
                minion.TavernTier > maxTier ||
                !TribeAvailabilityRules.IsMinionAvailable(minion, request.ActiveTribes))
            {
                return false;
            }

            var filter = profile.CandidateFilter ?? new DarkGiftCandidateFilter();
            if (!request.IgnoreNormalRoundRestrictions &&
                filter.BattlecryAllowedFromRound > 0 &&
                request.Round < filter.BattlecryAllowedFromRound &&
                HasMinionTag(minion, "keyword:battlecry"))
            {
                return false;
            }

            if (!request.IgnoreNormalRoundRestrictions &&
                filter.ChooseOneAllowedFromRound > 0 &&
                request.Round < filter.ChooseOneAllowedFromRound &&
                HasMinionTag(minion, "keyword:chooseone"))
            {
                return false;
            }

            return (filter.RequiredTags ?? new List<string>()).All(tag => HasMinionTag(minion, tag)) &&
                   !(filter.ExcludedTags ?? new List<string>()).Any(tag => HasMinionTag(minion, tag)) &&
                   !(filter.ExcludedMechanics ?? new List<string>()).Any(tag => HasMinionTag(minion, tag));
        }

        private static bool IsEligibleGift(DarkGiftDefinition gift, DarkGiftOfferRequest request)
        {
            if (gift == null || string.IsNullOrWhiteSpace(StableGiftId(gift)))
            {
                return false;
            }

            switch (gift.ResearchKey)
            {
                case "DG-R07":
                    if ((request.ActiveTribes ?? new List<Tribe>()).Contains(Tribe.Quilboar) ||
                        (request.ActiveTribes ?? new List<Tribe>()).Contains(Tribe.Naga))
                    {
                        return false;
                    }
                    break;
                case "DG-R22":
                    if (request.PlayerTavernTier < 3)
                    {
                        return false;
                    }
                    break;
                case "DG-R14":
                case "DG-R31":
                    if (request.BattlecriesTriggeredThisGame <= 0)
                    {
                        return false;
                    }
                    break;
                case "DG-R15":
                case "DG-R32":
                    if (request.DeathrattlesTriggeredThisGame <= 0)
                    {
                        return false;
                    }
                    break;
                case "DG-R16":
                case "DG-R33":
                    if (request.TavernSpellsCastThisGame <= 0)
                    {
                        return false;
                    }
                    break;
            }

            if (request.IgnoreNormalRoundRestrictions)
            {
                return true;
            }

            return (gift.EarliestOfferRound <= 0 || request.Round >= gift.EarliestOfferRound) &&
                   (gift.LatestOfferRound <= 0 || request.Round <= gift.LatestOfferRound);
        }

        private static bool IsCompatible(DarkGiftDefinition gift, MinionDefinition minion)
        {
            var researchKey = gift.ResearchKey ?? string.Empty;
            if (!string.IsNullOrEmpty(researchKey))
            {
                if ((HasMinionTag(minion, "keyword:battlecry") || HasMinionTag(minion, "keyword:chooseone")) &&
                    !BattlecryAndChooseOneGiftAllowlist.Contains(researchKey))
                {
                    return false;
                }

                if (HasMinionTag(minion, "keyword:deathrattle") &&
                    StatGiftKeys.Contains(researchKey) &&
                    researchKey != "DG-R06" &&
                    researchKey != "DG-R15" &&
                    researchKey != "DG-R32")
                {
                    return false;
                }
            }

            return (gift.RequiredMinionTags ?? new List<string>()).All(tag => HasMinionTag(minion, tag)) &&
                   !(gift.ExcludedMinionTags ?? new List<string>()).Any(tag => HasMinionTag(minion, tag));
        }

        private static bool TrySelectPairs(
            IReadOnlyList<DarkGiftDefinition> gifts,
            IReadOnlyList<MinionDefinition> minions,
            int giftIndex,
            int remaining,
            Tribe mostCommonTribe,
            bool requireCommonTribe,
            bool requireNeutralCore,
            HashSet<string> usedMinions,
            List<GiftMinionPair> selected)
        {
            if (remaining == 0)
            {
                if (requireCommonTribe && !selected.Any(pair => HasTribe(pair.Minion, mostCommonTribe)))
                {
                    return false;
                }

                if (requireNeutralCore && !selected.Any(pair => IsNeutralCore(pair.Minion)))
                {
                    return false;
                }

                var gilded = selected.FirstOrDefault(pair => pair.Gift?.ResearchKey == "DG-R17");
                return gilded == null || gilded.Minion.TavernTier == selected.Min(pair => pair.Minion.TavernTier);
            }

            if (giftIndex >= gifts.Count || gifts.Count - giftIndex < remaining)
            {
                return false;
            }

            var gift = gifts[giftIndex];
            foreach (var minion in minions)
            {
                var minionId = StableMinionId(minion);
                if (usedMinions.Contains(minionId) || !IsCompatible(gift, minion))
                {
                    continue;
                }

                if (requireCommonTribe && remaining == 1 &&
                    !selected.Any(pair => HasTribe(pair.Minion, mostCommonTribe)) &&
                    !HasTribe(minion, mostCommonTribe))
                {
                    continue;
                }

                if (requireNeutralCore && remaining == 1 &&
                    !selected.Any(pair => IsNeutralCore(pair.Minion)) &&
                    !IsNeutralCore(minion))
                {
                    continue;
                }

                usedMinions.Add(minionId);
                selected.Add(new GiftMinionPair(gift, minion));
                if (TrySelectPairs(
                        gifts,
                        minions,
                        giftIndex + 1,
                        remaining - 1,
                        mostCommonTribe,
                        requireCommonTribe,
                        requireNeutralCore,
                        usedMinions,
                        selected))
                {
                    return true;
                }
                selected.RemoveAt(selected.Count - 1);
                usedMinions.Remove(minionId);
            }

            return TrySelectPairs(
                gifts,
                minions,
                giftIndex + 1,
                remaining,
                mostCommonTribe,
                requireCommonTribe,
                requireNeutralCore,
                usedMinions,
                selected);
        }

        private static DarkGiftOfferOption CreateOption(MinionDefinition minion, DarkGiftDefinition gift)
        {
            return new DarkGiftOfferOption
            {
                OptionId = StableMinionId(minion) + "|" + StableGiftId(gift),
                MinionDefinitionId = minion.Id,
                MinionCardId = minion.CardId,
                MinionRevisionId = minion.RevisionId,
                MinionName = minion.Name,
                MinionText = minion.Text,
                MinionImagePath = minion.ImagePath,
                MinionTier = minion.TavernTier,
                MinionAttack = minion.BaseAttack,
                MinionHealth = minion.BaseHealth,
                MinionTribes = new List<Tribe>(minion.Tribes ?? new List<Tribe>()),
                GiftId = gift.Id,
                GiftRevisionId = gift.RevisionId,
                GiftName = gift.DisplayName,
                GiftText = gift.Text,
                GiftImagePath = gift.ImagePath
            };
        }

        private static bool HasMinionTag(MinionDefinition minion, string requestedTag)
        {
            if (minion == null || string.IsNullOrWhiteSpace(requestedTag))
            {
                return false;
            }

            var normalized = NormalizeTag(requestedTag);
            return (minion.Tags ?? new List<string>()).Any(tag => TagMatches(normalized, "tag", tag)) ||
                   (minion.EffectIds ?? new List<string>()).Any(tag => TagMatches(normalized, "effect", tag)) ||
                   (minion.Keywords ?? new List<Keyword>()).Any(keyword => TagMatches(normalized, "keyword", keyword.ToString())) ||
                   (minion.OfficialKeywords ?? new List<Keyword>()).Any(keyword => TagMatches(normalized, "keyword", keyword.ToString())) ||
                   (minion.Tribes ?? new List<Tribe>()).Any(tribe => TagMatches(normalized, "tribe", tribe.ToString()));
        }

        private static bool TagMatches(string requested, string prefix, string value)
        {
            var normalizedValue = NormalizeTag(value);
            return requested == normalizedValue || requested == prefix + ":" + normalizedValue;
        }

        private static string NormalizeTag(string value)
        {
            return new string((value ?? string.Empty)
                .Where(character => char.IsLetterOrDigit(character) || character == ':' || character == '-' || character == '_')
                .Select(char.ToLowerInvariant)
                .ToArray());
        }

        private static Tribe ResolveMostCommonTribe(IEnumerable<DarkGiftTribeCount> counts)
        {
            return (counts ?? Enumerable.Empty<DarkGiftTribeCount>())
                .Where(item => item != null && item.Count > 0 && item.Tribe != Tribe.None && item.Tribe != Tribe.All)
                .OrderByDescending(item => item.Count)
                .ThenBy(item => item.Tribe)
                .Select(item => item.Tribe)
                .DefaultIfEmpty(Tribe.None)
                .First();
        }

        private static bool HasTribe(MinionDefinition minion, Tribe tribe)
        {
            return tribe != Tribe.None &&
                   minion != null &&
                   (minion.Tribes ?? new List<Tribe>()).Any(candidate => candidate == tribe || candidate == Tribe.All);
        }

        private static bool IsNeutralCore(MinionDefinition minion)
        {
            return minion != null &&
                   (NeutralCoreCardIds.Contains(minion.CardId ?? string.Empty) ||
                    NeutralCoreCardIds.Contains(minion.Id ?? string.Empty));
        }

        private static string StableMinionId(MinionDefinition minion)
        {
            return string.IsNullOrWhiteSpace(minion?.CardId) ? minion?.Id ?? string.Empty : minion.CardId;
        }

        private static string StableGiftId(DarkGiftDefinition gift)
        {
            return string.IsNullOrWhiteSpace(gift?.RevisionId) ? gift?.Id ?? string.Empty : gift.RevisionId;
        }

        private static int DeriveSeed(DarkGiftOfferRequest request)
        {
            var value = unchecked((uint)request.Seed);
            value = unchecked(value * 16777619u) ^ unchecked((uint)request.Round);
            value = unchecked(value * 16777619u) ^ unchecked((uint)request.RngCursor);
            value = Hash(value, request.SourceKind.ToString());
            value = Hash(value, request.SourceId);
            value = Hash(value, request.GiftPoolProfileId);
            return unchecked((int)value);
        }

        private static uint Hash(uint value, string text)
        {
            foreach (var character in text ?? string.Empty)
            {
                value = unchecked((value ^ character) * 16777619u);
            }
            return value;
        }

        private static void Shuffle<T>(IList<T> values, CursorRng rng)
        {
            for (var index = values.Count - 1; index > 0; index -= 1)
            {
                var swapIndex = rng.NextInt(index + 1);
                var value = values[index];
                values[index] = values[swapIndex];
                values[swapIndex] = value;
            }
        }

        private static DarkGiftOfferResult Failure(
            DarkGiftOfferRequest request,
            string code,
            string message,
            int? nextCursor = null)
        {
            return new DarkGiftOfferResult
            {
                Succeeded = false,
                Code = code,
                Message = message,
                SourceKind = request?.SourceKind ?? DarkGiftOfferSourceKind.Debug,
                SourceId = request?.SourceId,
                GiftPoolProfileId = request?.GiftPoolProfileId,
                PickCount = 0,
                NextRngCursor = nextCursor ?? Math.Max(0, request?.RngCursor ?? 0),
                Options = new List<DarkGiftOfferOption>()
            };
        }

        private sealed class CursorRng
        {
            private readonly SeededRng rng;

            public CursorRng(int seed, int cursor)
            {
                rng = new SeededRng(seed);
                Cursor = Math.Max(0, cursor);
            }

            public int Cursor { get; private set; }

            public int NextInt(int maxExclusive)
            {
                Cursor += 1;
                return rng.NextInt(maxExclusive);
            }
        }

        private sealed class GiftMinionPair
        {
            public GiftMinionPair(DarkGiftDefinition gift, MinionDefinition minion)
            {
                Gift = gift;
                Minion = minion;
            }

            public DarkGiftDefinition Gift { get; }
            public MinionDefinition Minion { get; }
        }
    }
}
