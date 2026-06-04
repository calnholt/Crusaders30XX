using System.Collections.Generic;
using Crusaders30XX.ECS.Objects.Temperance;

namespace Crusaders30XX.ECS.Factories
{
    public static class TemperanceFactory
    {
        public static TemperanceBase Create(string temperanceId)
        {
            return temperanceId switch
            {
                "angelic_aura" => new AngelicAura(),
                "fling_fling" => new FlingFling(),
                "radiance" => new Radiance(),
                _ => null
            };
        }

        public static Dictionary<string, TemperanceBase> GetAllTemperanceAbilities()
        {
            return new Dictionary<string, TemperanceBase>
            {
                { "angelic_aura", new AngelicAura() },
                { "fling_fling", new FlingFling() },
                { "radiance", new Radiance() },
            };
        }
    }
}
