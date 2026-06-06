using System.Collections.Generic;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Systems;
using Microsoft.Xna.Framework;
using Xunit;

namespace Crusaders30XX.Tests;

public class LocationPoiRevealCutsceneSystemTests
{
	[Fact]
	public void Nearby_allowed_hellrift_is_revealed_and_persisted()
	{
		var hellrift = new PointOfInterest
		{
			Id = "run_6",
			Type = PointOfInterestType.Hellrift,
			WorldPosition = new Vector2(4702.6f, 748.1f),
			DisplayRadius = 123f,
		};
		var allowedIds = new HashSet<string> { hellrift.Id };
		string persistedId = null;

		bool revealed = LocationPoiRevealCutsceneSystem.TryRevealCombatPoi(
			hellrift,
			allowedIds,
			"run_5",
			5534.3f,
			463.4f,
			1000f,
			id =>
			{
				persistedId = id;
				return true;
			});

		Assert.True(revealed);
		Assert.Equal("run_6", persistedId);
		Assert.True(hellrift.IsRevealed);
		Assert.Equal(0f, hellrift.DisplayRadius);
	}

	[Theory]
	[InlineData(PointOfInterestType.Shop)]
	[InlineData(PointOfInterestType.Treasure)]
	[InlineData(PointOfInterestType.Event)]
	public void Non_combat_landmarks_are_not_revealed(PointOfInterestType type)
	{
		var landmark = new PointOfInterest
		{
			Id = "landmark",
			Type = type,
			WorldPosition = Vector2.Zero,
		};
		bool persistCalled = false;

		bool revealed = LocationPoiRevealCutsceneSystem.TryRevealCombatPoi(
			landmark,
			new HashSet<string> { landmark.Id },
			"run_5",
			0f,
			0f,
			1000f,
			_ =>
			{
				persistCalled = true;
				return true;
			});

		Assert.False(revealed);
		Assert.False(persistCalled);
		Assert.False(landmark.IsRevealed);
	}
}
