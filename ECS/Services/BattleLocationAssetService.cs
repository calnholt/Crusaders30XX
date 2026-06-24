using System;
using System.Collections.Generic;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Services
{
	public static class BattleLocationAssetService
	{
		public static readonly BattleLocation[] ClimbEncounterLocations =
		{
			BattleLocation.Desert,
			BattleLocation.Tundra,
			BattleLocation.Jungle,
		};

		public const BattleLocation FinalEncounterLocation = BattleLocation.TheGate;

		private static readonly Dictionary<string, Texture2D> BackgroundCache = new(StringComparer.OrdinalIgnoreCase);

		public static BattleLocation RollClimbEncounterLocation(Random rng)
		{
			rng ??= Random.Shared;
			return ClimbEncounterLocations[rng.Next(ClimbEncounterLocations.Length)];
		}

		public static string GetBackgroundAsset(BattleLocation location)
		{
			return location switch
			{
				BattleLocation.Desert => "Battle_Backgrounds/desert-battle-background",
				BattleLocation.Tundra => "Battle_Backgrounds/tundra-battle-background",
				BattleLocation.Jungle => "Battle_Backgrounds/jungle-battle-background",
				BattleLocation.TheGate => "Battle_Backgrounds/the-gate-battle-background",
				BattleLocation.Forest => "forest-background",
				BattleLocation.Cathedral => "cathedral-background",
				_ => string.Empty,
			};
		}

		public static MusicTrack GetMusicTrack(BattleLocation location)
		{
			return location switch
			{
				BattleLocation.Desert => MusicTrack.DesertBattle,
				BattleLocation.Tundra => MusicTrack.TundraBattle,
				BattleLocation.Jungle => MusicTrack.JungleBattle,
				BattleLocation.TheGate => MusicTrack.TheGateBattle,
				_ => MusicTrack.DesertBattle,
			};
		}

		public static Texture2D TryLoad(ContentManager content, BattleLocation location)
		{
			return TryLoad(content, GetBackgroundAsset(location));
		}

		public static Texture2D TryLoad(ContentManager content, string asset)
		{
			if (content == null || string.IsNullOrWhiteSpace(asset)) return null;
			if (BackgroundCache.TryGetValue(asset, out var cached)) return cached;

			try
			{
				var texture = content.Load<Texture2D>(asset);
				BackgroundCache[asset] = texture;
				return texture;
			}
			catch
			{
				BackgroundCache[asset] = null;
				return null;
			}
		}
	}
}
