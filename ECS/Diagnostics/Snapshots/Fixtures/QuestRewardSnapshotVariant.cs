using System;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Factories;

namespace Crusaders30XX.Diagnostics.Snapshots.Fixtures
{
    public sealed class QuestRewardSnapshotVariant
    {
        public int RewardGold { get; init; }
        public bool HasCardReward { get; init; }
        public string RewardCardKey { get; init; } = string.Empty;
        public string FileSlug { get; init; } = "default";

        public static QuestRewardSnapshotVariant Parse(string[] args)
        {
            int? gold = null;
            string cardKey = null;

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
                else if (string.Equals(args[i], "--card", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[i + 1]))
                    {
                        throw new DisplaySnapshotSetupException("Invalid --card value; expected cardId|color");
                    }
                    cardKey = args[i + 1];
                    i++;
                }
                else
                {
                    throw new DisplaySnapshotSetupException($"Unknown argument: '{args[i]}'");
                }
            }

            if (gold == null && cardKey == null)
            {
                gold = 500;
                cardKey = "strike|white";
            }

            bool hasCard = !string.IsNullOrEmpty(cardKey);
            if (hasCard)
            {
                ValidateCardKey(cardKey);
            }

            int rewardGold = gold ?? 0;
            string slug = BuildSlug(rewardGold, hasCard, cardKey);

            return new QuestRewardSnapshotVariant
            {
                RewardGold = rewardGold,
                HasCardReward = hasCard,
                RewardCardKey = cardKey ?? string.Empty,
                FileSlug = slug
            };
        }

        private static void ValidateCardKey(string cardKey)
        {
            var parts = cardKey.Split('|');
            if (parts.Length < 2)
            {
                throw new DisplaySnapshotSetupException($"Invalid --card format '{cardKey}'; expected cardId|color");
            }

            string cardId = parts[0];
            if (CardFactory.Create(cardId) == null)
            {
                throw new DisplaySnapshotSetupException($"Unknown card id in --card: '{cardId}'");
            }

            string colorToken = parts[1].Trim().ToLowerInvariant();
            if (colorToken is not ("white" or "red" or "black"))
            {
                throw new DisplaySnapshotSetupException($"Invalid card color '{parts[1]}'; expected white, red, or black");
            }
        }

        private static string BuildSlug(int gold, bool hasCard, string cardKey)
        {
            if (!hasCard)
            {
                return $"gold-{gold}";
            }

            var parts = cardKey.Split('|');
            string cardSlug = $"{parts[0]}-{parts[1].Trim().ToLowerInvariant()}";
            if (gold > 0)
            {
                return $"gold-{gold}-card-{cardSlug}";
            }

            return $"card-{cardSlug}";
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
