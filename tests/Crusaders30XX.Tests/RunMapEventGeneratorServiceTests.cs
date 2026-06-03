using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Data.Locations;
using Crusaders30XX.ECS.Services;
using Xunit;

namespace Crusaders30XX.Tests;

public class RunMapEventGeneratorServiceTests
{
	[Fact]
	public void Generate_places_events_enterable_after_first_quest_completed_with_distinct_types()
	{
		const int attempts = 24;
		for (int i = 0; i < attempts; i++)
		{
			var (seed, nodes) = LocationMapGeneratorService.Generate();
			var shops = RunMapShopGeneratorService.Generate(seed, nodes);
			var treasures = RunMapTreasureGeneratorService.Generate(seed, nodes, shops);
			var events = RunMapEventGeneratorService.Generate(seed, nodes, shops, treasures);
			Assert.Equal(LocationMapConstants.RunMapEventCount, events.Count);

			var typeIds = events.Select(e => e.eventTypeId).ToList();
			Assert.Equal(LocationMapConstants.RunMapEventCount, typeIds.Distinct().Count());

			float revealRadius = LocationMapConstants.DefaultRevealRadius;
			float revealRadiusSq = revealRadius * revealRadius;

			foreach (var mapEvent in events)
			{
				Assert.False(string.IsNullOrWhiteSpace(mapEvent.eventTypeId));

				foreach (var node in nodes)
				{
					if (node != null)
					{
						node.isRevealed = false;
						node.isCompleted = false;
					}
				}

				bool unlocked = false;
				for (int n = 0; n < nodes.Count; n++)
				{
					var node = nodes[n];
					if (node == null) continue;
					float dx = mapEvent.worldX - node.worldX;
					float dy = mapEvent.worldY - node.worldY;
					if (dx * dx + dy * dy > revealRadiusSq) continue;

					node.isRevealed = true;
					node.isCompleted = true;
					unlocked = RunMapEventService.IsEnterable(mapEvent, nodes);
					break;
				}

				Assert.True(
					unlocked,
					$"seed {seed} Map event {mapEvent.id} not enterable when a nearby quest is completed");
			}
		}
	}
}
