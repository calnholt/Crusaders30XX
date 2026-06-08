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
                "absolution" => new Absolution(),
                "anoint_the_sick" => new AnointTheSick(),
                "ark_of_the_covenant" => new ArkOfTheCovenant(),
                "bulwark" => new Bulwark(),
                "burn" => new Burn(),
                "carve" => new Carve(),
                "consecrate" => new Consecrate(),
                "courageous" => new Courageous(),
                "crimson_rite" => new CrimsonRite(),
                "crusade" => new Crusade(),
                "dagger" => new Dagger(),
                "deus_vult" => new DeusVult(),
                "divine_protection" => new DivineProtection(),
                "dowse_with_holy_water" => new DowseWithHolyWater(),
                "exaltation" => new Exaltation(),
                "fervor" => new Fervor(),
                "fury" => new Fury(),
                "glacial_maul" => new GlacialMaul(),
                // "heavens_glory" => new HeavensGlory(),
                "hold_the_line" => new HoldTheLine(),
                "hidden_kunai" => new HiddenKunai(),
                "impale" => new Impale(),
                "increase_faith" => new IncreaseFaith(),
                "kunai" => new Kunai(),
                "litany_of_wrath" => new LitanyOfWrath(),
                "narrow_gate" => new NarrowGate(),
                "quick_wit" => new QuickWit(),
                "rally_the_faithful" => new RallyTheFaithful(),
                "relentless_strike" => new RelentlessStrike(),
                "pouch_of_kunai" => new PouchOfKunai(),
                "pierce_through" => new PierceThrough(),
                "purge" => new Purge(),
                "ravage" => new Ravage(),
                "razor_storm" => new RazorStorm(),
                "reckoning" => new Reckoning(),
                "reap" => new Reap(),
                "reconciled" => new Reconciled(),
                "sacrifice" => new Sacrifice(),
                "serpent_crush" => new SerpentCrush(),
                "seize" => new Seize(),
                "shield_of_faith" => new ShieldOfFaith(),
                "shroud_of_turin" => new ShroudOfTurin(),
                "smite" => new Smite(),
                "stab" => new Stab(),
                "stalwart" => new Stalwart(),
                "strike" => new Strike(),
                "sudden_thrust" => new SuddenThrust(),
                // "stun" => new Stun(),
                "sword" => new Sword(),
                "temper_the_blade" => new TemperTheBlade(),
                "tempest" => new Tempest(),
                "vindicate" => new Vindicate(),
                "whirlwind" => new Whirlwind(),
                "zealous_vow" => new ZealousVow(),
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
                { "absolution", new Absolution() },
                { "anoint_the_sick", new AnointTheSick() },
                { "ark_of_the_covenant", new ArkOfTheCovenant() },
                { "bulwark", new Bulwark() },
                { "burn", new Burn() },
                { "carve", new Carve() },
                { "consecrate", new Consecrate() },
                { "courageous", new Courageous() },
                { "crimson_rite", new CrimsonRite() },
                { "crusade", new Crusade() },
                { "dagger", new Dagger() },
                { "deus_vult", new DeusVult() },
                { "divine_protection", new DivineProtection() },
                { "dowse_with_holy_water", new DowseWithHolyWater() },
                { "exaltation", new Exaltation() },
                { "fervor", new Fervor() },
                { "fury", new Fury() },
                { "glacial_maul", new GlacialMaul() },
                // { "heavens_glory", new HeavensGlory() },
                { "impale", new Impale() },
                { "increase_faith", new IncreaseFaith() },
                { "hold_the_line", new HoldTheLine() },
                { "hidden_kunai", new HiddenKunai() },
                { "kunai", new Kunai() },
                { "litany_of_wrath", new LitanyOfWrath() },
                { "narrow_gate", new NarrowGate() },
                { "quick_wit", new QuickWit() },
                { "rally_the_faithful", new RallyTheFaithful() },
                { "relentless_strike", new RelentlessStrike() },
                { "pouch_of_kunai", new PouchOfKunai() },
                { "pierce_through", new PierceThrough() },
                { "purge", new Purge() },
                { "ravage", new Ravage() },
                { "razor_storm", new RazorStorm() },
                { "reckoning", new Reckoning() },
                { "reap", new Reap() },
                { "reconciled", new Reconciled() },
                { "sacrifice", new Sacrifice() },
                { "serpent_crush", new SerpentCrush() },
                { "seize", new Seize() },
                { "shield_of_faith", new ShieldOfFaith() },
                { "shroud_of_turin", new ShroudOfTurin() },
                { "smite", new Smite() },
                { "stab", new Stab() },
                { "stalwart", new Stalwart() },
                { "strike", new Strike() },
                { "sudden_thrust", new SuddenThrust() },
                // { "stun", new Stun() },
                { "sword", new Sword() },
                { "temper_the_blade", new TemperTheBlade() },
                { "tempest", new Tempest() },
                { "vindicate", new Vindicate() },
                { "whirlwind", new Whirlwind() },
                { "zealous_vow", new ZealousVow() }
            };
        }
    }
}

