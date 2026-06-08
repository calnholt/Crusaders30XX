using System;
using System.Collections.Generic;
using Crusaders30XX.Diagnostics.Snapshots.Fixtures;

namespace Crusaders30XX.Diagnostics.Snapshots
{
    public static class DisplaySnapshotRegistry
    {
        private static readonly Dictionary<string, IDisplaySnapshotFixture> Fixtures = new(StringComparer.OrdinalIgnoreCase);

        static DisplaySnapshotRegistry()
        {
            Register(new CardDisplaySnapshotFixture());
            Register(new BrittleCardSnapshotFixture());
            Register(new QuestRewardModalSnapshotFixture());
            Register(new NarrativeEventModalSnapshotFixture());
            Register(new WayStationSnapshotFixture());
            Register(new PlayerHudSnapshotFixture());
            Register(new EquipmentTooltipSnapshotFixture());
        }

        public static void Register(IDisplaySnapshotFixture fixture)
        {
            Fixtures[fixture.Id] = fixture;
        }

        public static bool TryGet(string fixtureId, out IDisplaySnapshotFixture fixture)
        {
            return Fixtures.TryGetValue(fixtureId, out fixture);
        }
    }
}
