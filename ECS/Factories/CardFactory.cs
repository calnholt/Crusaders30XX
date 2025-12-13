using Crusaders30XX.ECS.Objects.Cards;

namespace Crusaders30XX.ECS.Factories
{
    /// <summary>
    /// Factory for creating CardBase instances from card IDs
    /// </summary>
    public static class CardFactory
    {
        /// <summary>
        /// Creates a CardBase instance from a card ID string
        /// </summary>
        /// <param name="cardId">The card ID (e.g., "anoint_the_sick")</param>
        /// <returns>The corresponding CardBase instance, or null if not found</returns>
        public static CardBase Create(string cardId)
        {
            return cardId switch
            {
                "anoint_the_sick" => new AnointTheSick(),
                "bulwark" => new Bulwark(),
                "burn" => new Burn(),
                "courageous" => new Courageous(),
                "dagger" => new Dagger(),
                "divine_protection" => new DivineProtection(),
                "dowse_with_holy_water" => new DowseWithHolyWater(),
                "fury" => new Fury(),
                "heavens_glory" => new HeavensGlory(),
                "impale" => new Impale(),
                "increase_faith" => new IncreaseFaith(),
                "kunai" => new Kunai(),
                "narrow_gate" => new NarrowGate(),
                "pouch_of_kunai" => new PouchOfKunai(),
                "ravage" => new Ravage(),
                "reconciled" => new Reconciled(),
                "sacrifice" => new Sacrifice(),
                "seize" => new Seize(),
                "shield_of_faith" => new ShieldOfFaith(),
                "shroud_of_turin" => new ShroudOfTurin(),
                "stab" => new Stab(),
                "stalwart" => new Stalwart(),
                "strike" => new Strike(),
                "stun" => new Stun(),
                "sword" => new Sword(),
                "tempest" => new Tempest(),
                "vindicate" => new Vindicate(),
                "whirlwind" => new Whirlwind(),
                _ => null
            };
        }
    }
}

