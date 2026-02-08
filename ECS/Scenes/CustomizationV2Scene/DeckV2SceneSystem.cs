using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Loadouts;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("DeckV2 Scene")]
	public class DeckV2SceneSystem : Core.System
	{
		public DeckV2SceneSystem(EntityManager em) : base(em)
		{
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			var scene = entity.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.CustomizationV2) return;

			var nav = EntityManager.GetEntitiesWithComponent<CustomizationV2NavigationState>().FirstOrDefault()?.GetComponent<CustomizationV2NavigationState>();
			if (nav == null || nav.ActiveTab != CustomizationV2TabType.Deck) return;

			EnsureDeckStateLoaded();
		}

		private void EnsureDeckStateLoaded()
		{
			var existing = EntityManager.GetEntitiesWithComponent<CustomizationV2DeckState>().FirstOrDefault();
			if (existing != null) return;

			var stateEntity = EntityManager.CreateEntity("CV2_DeckState");
			var st = new CustomizationV2DeckState();

			if (LoadoutDefinitionCache.TryGet("loadout_1", out var def) && def != null)
			{
				st.DeckCardKeys = new List<string>(def.cardIds ?? new List<string>());
			}

			EntityManager.AddComponent(stateEntity, st);
		}
	}
}
