using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Factories;
using Crusaders30XX.ECS.Services;

namespace Crusaders30XX.Diagnostics.Snapshots.Fixtures
{
    public sealed class QuestRewardSnapshotVariant
    {
        public int RewardGold { get; init; }
        public bool HasCardReward => DeckRewardOffer?.options != null && DeckRewardOffer.options.Count > 0;
        public string RewardCardKey => RewardCardKeys.FirstOrDefault() ?? string.Empty;
        public List<string> RewardCardKeys { get; init; } = new();
        public DeckRewardOfferSave DeckRewardOffer { get; init; }
        public string FileSlug { get; init; } = "default";

        public static QuestRewardSnapshotVariant Parse(string[] args)
        {
            int? gold = null;
            var options = new List<DeckRewardOfferOptionSave>();
            var legacyCardKeys = new List<string>();

            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], "--gold", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 >= args.Length || !int.TryParse(args[i + 1], out int value) || value < 0)
                    {
                        throw new DisplaySnapshotSetupException("Invalid --gold value; expected non-negative integer");
                    }
                    gold = value;
                    i++;
                }
                else if (string.Equals(args[i], "--exchange", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 2 >= args.Length)
                    {
                        throw new DisplaySnapshotSetupException("Invalid --exchange value; expected outgoingKey incomingKey");
                    }
                    string outgoing = args[i + 1];
                    string incoming = args[i + 2];
                    ValidateCardKey(outgoing);
                    ValidateCardKey(incoming);
                    options.Add(new DeckRewardOfferOptionSave
                    {
                        kind = DeckRewardOfferKinds.Exchange,
                        loadoutIndex = options.Count,
                        outgoingCardKey = outgoing,
                        incomingCardKey = incoming
                    });
                    i += 2;
                }
                else if (string.Equals(args[i], "--upgrade", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[i + 1]))
                    {
                        throw new DisplaySnapshotSetupException("Invalid --upgrade value; expected cardId|color");
                    }
                    string outgoing = args[i + 1];
                    ValidateCardKey(outgoing);
                    string upgraded = RunDeckService.BuildUpgradedCardKey(outgoing);
                    if (string.IsNullOrWhiteSpace(upgraded))
                    {
                        throw new DisplaySnapshotSetupException($"Invalid --upgrade key '{outgoing}'");
                    }
                    options.Add(new DeckRewardOfferOptionSave
                    {
                        kind = DeckRewardOfferKinds.Upgrade,
                        loadoutIndex = options.Count,
                        outgoingCardKey = outgoing,
                        upgradedCardKey = upgraded
                    });
                    i++;
                }
                else if (string.Equals(args[i], "--card", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[i + 1]))
                    {
                        throw new DisplaySnapshotSetupException("Invalid --card value; expected cardId|color");
                    }
                    ValidateCardKey(args[i + 1]);
                    legacyCardKeys.Add(args[i + 1]);
                    i++;
                }
                else
                {
                    throw new DisplaySnapshotSetupException($"Unknown argument: '{args[i]}'");
                }
            }

            int rewardGold = gold ?? 500;
            if (options.Count == 0 && legacyCardKeys.Count > 0)
            {
                string[] outgoingDefaults = { "strike|white", "smite|red" };
                for (int i = 0; i < legacyCardKeys.Count; i++)
                {
                    string outgoing = outgoingDefaults[Math.Min(i, outgoingDefaults.Length - 1)];
                    options.Add(new DeckRewardOfferOptionSave
                    {
                        kind = DeckRewardOfferKinds.Exchange,
                        loadoutIndex = i,
                        outgoingCardKey = outgoing,
                        incomingCardKey = legacyCardKeys[i]
                    });
                }
            }

            if (options.Count == 0)
            {
                options.Add(new DeckRewardOfferOptionSave
                {
                    kind = DeckRewardOfferKinds.Exchange,
                    loadoutIndex = 0,
                    outgoingCardKey = "strike|white",
                    incomingCardKey = "smite|red"
                });
                options.Add(new DeckRewardOfferOptionSave
                {
                    kind = DeckRewardOfferKinds.Exchange,
                    loadoutIndex = 1,
                    outgoingCardKey = "reckoning|white",
                    incomingCardKey = "unburdened_strike|black"
                });
                options.Add(new DeckRewardOfferOptionSave
                {
                    kind = DeckRewardOfferKinds.Upgrade,
                    loadoutIndex = 2,
                    outgoingCardKey = "smite|white",
                    upgradedCardKey = "smite|white|Upgraded"
                });
            }

            var offer = new DeckRewardOfferSave
            {
                rewardGold = rewardGold,
                options = options.Take(3).ToList()
            };
            var rewardKeys = offer.options
                .Select(o => string.Equals(o.kind, DeckRewardOfferKinds.Upgrade, StringComparison.OrdinalIgnoreCase)
                    ? o.upgradedCardKey
                    : o.incomingCardKey)
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .ToList();

            return new QuestRewardSnapshotVariant
            {
                RewardGold = rewardGold,
                RewardCardKeys = rewardKeys,
                DeckRewardOffer = offer,
                FileSlug = BuildSlug(rewardGold, offer.options)
            };
        }

        private static void ValidateCardKey(string cardKey)
        {
            if (!RunDeckService.TryParseCardKey(cardKey, out var cardId, out _, out _))
            {
                throw new DisplaySnapshotSetupException($"Invalid card key '{cardKey}'; expected cardId|color or cardId|color|Upgraded");
            }

            if (CardFactory.Create(cardId) == null)
            {
                throw new DisplaySnapshotSetupException($"Unknown card id in card key: '{cardId}'");
            }
        }

        private static string BuildSlug(int gold, List<DeckRewardOfferOptionSave> options)
        {
            if (options == null || options.Count == 0) return $"gold-{gold}";
            var parts = new List<string> { $"gold-{gold}", "deck-offer" };
            foreach (var option in options)
            {
                string key = string.Equals(option.kind, DeckRewardOfferKinds.Upgrade, StringComparison.OrdinalIgnoreCase)
                    ? option.upgradedCardKey
                    : option.incomingCardKey;
                parts.Add(ToSlug(key));
            }
            return string.Join("-", parts);
        }

        private static string ToSlug(string cardKey)
        {
            if (string.IsNullOrWhiteSpace(cardKey)) return "empty";
            return cardKey
                .Replace("|", "-", StringComparison.Ordinal)
                .Trim()
                .ToLowerInvariant();
        }

        public static CardData.CardColor ParseColor(string color)
        {
            if (string.IsNullOrEmpty(color)) return CardData.CardColor.White;
            return color.Trim().ToLowerInvariant() switch
            {
                "red" => CardData.CardColor.Red,
                "black" => CardData.CardColor.Black,
                _ => CardData.CardColor.White
            };
        }
    }
}
