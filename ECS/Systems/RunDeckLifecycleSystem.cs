using System.Collections.Generic;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Services;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Keeps run deck ECS entities in sync with save loadout and run lifecycle events.
	/// </summary>
	public class RunDeckLifecycleSystem : Core.System
	{
		public RunDeckLifecycleSystem(EntityManager entityManager) : base(entityManager)
		{
			EventManager.Subscribe<QuestSelected>(_ => RunDeckService.EnsureRunDeck(EntityManager));
			EventManager.Subscribe<LoadoutCardAdded>(OnLoadoutCardAdded);
			EventManager.Subscribe<LoadoutCardRemoved>(OnLoadoutCardRemoved);
		}

		private void OnLoadoutCardAdded(LoadoutCardAdded evt)
		{
			if (evt == null || string.IsNullOrWhiteSpace(evt.CardKey)) return;
			if (!string.Equals(evt.LoadoutId, RunDeckService.PrimaryLoadoutId, System.StringComparison.OrdinalIgnoreCase))
			{
				return;
			}
			RunDeckService.AddCardFromKey(EntityManager, evt.CardKey);
		}

		private void OnLoadoutCardRemoved(LoadoutCardRemoved evt)
		{
			if (evt == null || string.IsNullOrWhiteSpace(evt.CardKey)) return;
			if (!string.Equals(evt.LoadoutId, RunDeckService.PrimaryLoadoutId, System.StringComparison.OrdinalIgnoreCase))
			{
				return;
			}
			RunDeckService.RemoveCardByKey(EntityManager, evt.CardKey);
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			yield break;
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
		}
	}
}
