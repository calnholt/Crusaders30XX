using System.Collections.Generic;
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
                "ark_of_the_covenant" => new ArkOfTheCovenant(),
                "bulwark" => new Bulwark(),
                "burn" => new Burn(),
                "carve" => new Carve(),
                "courageous" => new Courageous(),
                "dagger" => new Dagger(),
                "deus_vult" => new DeusVult(),
                "divine_protection" => new DivineProtection(),
                "dowse_with_holy_water" => new DowseWithHolyWater(),
                "fury" => new Fury(),
                "heavens_glory" => new HeavensGlory(),
                "hold_the_line" => new HoldTheLine(),
                "impale" => new Impale(),
                "increase_faith" => new IncreaseFaith(),
                "kunai" => new Kunai(),
                "narrow_gate" => new NarrowGate(),
                "pouch_of_kunai" => new PouchOfKunai(),
                "purge" => new Purge(),
                "ravage" => new Ravage(),
                "razor_storm" => new RazorStorm(),
                "reap" => new Reap(),
                "reconciled" => new Reconciled(),
                "sacrifice" => new Sacrifice(),
                "serpent_crush" => new SerpentCrush(),
                "seize" => new Seize(),
                "shield_of_faith" => new ShieldOfFaith(),
                "shroud_of_turin" => new ShroudOfTurin(),
                "stab" => new Stab(),
                "stalwart" => new Stalwart(),
                "strike" => new Strike(),
                // "stun" => new Stun(),
                "sword" => new Sword(),
                "tempest" => new Tempest(),
                "vindicate" => new Vindicate(),
                "whirlwind" => new Whirlwind(),
                _ => null
            };
        }

        /// <summary>
        /// Returns a dictionary of all available cards, keyed by card ID
        /// </summary>
        /// <returns>A dictionary mapping card IDs to CardBase instances</returns>
        public static Dictionary<string, CardBase> GetAllCards()
        {
            return new Dictionary<string, CardBase>
            {
                { "anoint_the_sick", new AnointTheSick() },
                { "ark_of_the_covenant", new ArkOfTheCovenant() },
                { "bulwark", new Bulwark() },
                { "burn", new Burn() },
                { "carve", new Carve() },
                { "courageous", new Courageous() },
                { "dagger", new Dagger() },
                { "deus_vult", new DeusVult() },
                { "divine_protection", new DivineProtection() },
                { "dowse_with_holy_water", new DowseWithHolyWater() },
                { "fury", new Fury() },
                { "heavens_glory", new HeavensGlory() },
                { "impale", new Impale() },
                { "increase_faith", new IncreaseFaith() },
                { "hold_the_line", new HoldTheLine() },
                { "kunai", new Kunai() },
                { "narrow_gate", new NarrowGate() },
                { "pouch_of_kunai", new PouchOfKunai() },
                { "purge", new Purge() },
                { "ravage", new Ravage() },
                { "razor_storm", new RazorStorm() },
                { "reap", new Reap() },
                { "reconciled", new Reconciled() },
                { "sacrifice", new Sacrifice() },
                { "serpent_crush", new SerpentCrush() },
                { "seize", new Seize() },
                { "shield_of_faith", new ShieldOfFaith() },
                { "shroud_of_turin", new ShroudOfTurin() },
                { "stab", new Stab() },
                { "stalwart", new Stalwart() },
                { "strike", new Strike() },
                // { "stun", new Stun() },
                { "sword", new Sword() },
                { "tempest", new Tempest() },
                { "vindicate", new Vindicate() },
                { "whirlwind", new Whirlwind() }
            };
        }
    }
}

