using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Services;
using Xunit;

namespace Crusaders30XX.Tests;

public class BattleLocationAssetServiceTests
{
	[Fact]
	public void Maps_battle_locations_to_background_assets()
	{
		Assert.Equal("Battle_Backgrounds/desert-battle-background", BattleLocationAssetService.GetBackgroundAsset(BattleLocation.Desert));
		Assert.Equal("Battle_Backgrounds/tundra-battle-background", BattleLocationAssetService.GetBackgroundAsset(BattleLocation.Tundra));
		Assert.Equal("Battle_Backgrounds/jungle-battle-background", BattleLocationAssetService.GetBackgroundAsset(BattleLocation.Jungle));
		Assert.Equal("Battle_Backgrounds/the-gate-battle-background", BattleLocationAssetService.GetBackgroundAsset(BattleLocation.TheGate));
	}

	[Fact]
	public void Maps_battle_locations_to_music_tracks()
	{
		Assert.Equal(MusicTrack.DesertBattle, BattleLocationAssetService.GetMusicTrack(BattleLocation.Desert));
		Assert.Equal(MusicTrack.TundraBattle, BattleLocationAssetService.GetMusicTrack(BattleLocation.Tundra));
		Assert.Equal(MusicTrack.JungleBattle, BattleLocationAssetService.GetMusicTrack(BattleLocation.Jungle));
		Assert.Equal(MusicTrack.TheGateBattle, BattleLocationAssetService.GetMusicTrack(BattleLocation.TheGate));
	}

	[Fact]
	public void Rollable_climb_locations_exclude_the_gate()
	{
		Assert.Contains(BattleLocation.Desert, BattleLocationAssetService.ClimbEncounterLocations);
		Assert.Contains(BattleLocation.Tundra, BattleLocationAssetService.ClimbEncounterLocations);
		Assert.Contains(BattleLocation.Jungle, BattleLocationAssetService.ClimbEncounterLocations);
		Assert.DoesNotContain(BattleLocation.TheGate, BattleLocationAssetService.ClimbEncounterLocations);
		Assert.Equal(BattleLocation.TheGate, BattleLocationAssetService.FinalEncounterLocation);
	}
}
