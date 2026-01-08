using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Factories;

namespace Crusaders30XX.ECS.Services
{
	/// <summary>
	/// Service for generating random dungeon encounters
	/// </summary>
	public static class DungeonEncounterGeneratorService
	{
		/// <summary>
		/// Generates a random enemy encounter for a dungeon
		/// </summary>
		/// <param name="count">Number of enemies to generate (default 3)</param>
		/// <returns>List of enemy IDs</returns>
		public static List<string> GenerateRandomEnemyEncounter(int count = 3)
		{
			var allEnemies = EnemyFactory.GetAllEnemies().Keys.ToList();
			var selected = new List<string>();
			
			for (int i = 0; i < count && allEnemies.Count > 0; i++)
			{
				int index = Random.Shared.Next(allEnemies.Count);
				selected.Add(allEnemies[index]);
				allEnemies.RemoveAt(index); // Prevent duplicates in the same encounter
			}
			
			return selected;
		}
	}
}
