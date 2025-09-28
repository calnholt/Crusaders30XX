using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Handles adding/removing cards between the library and the working loadout using events.
	/// </summary>
	public class LoadoutEditSystem : Core.System
	{
		public LoadoutEditSystem(EntityManager em) : base(em)
		{
			EventManager.Subscribe<AddCardToLoadoutRequested>(OnAddRequested);
			EventManager.Subscribe<RemoveCardFromLoadoutRequested>(OnRemoveRequested);
			EventManager.Subscribe<UpdateTemperanceLoadoutRequested>(OnUpdateTemperanceRequested);
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, Microsoft.Xna.Framework.GameTime gameTime)
		{
			// No per-frame logic; purely event-driven
		}

		private void OnAddRequested(AddCardToLoadoutRequested evt)
		{
			if (evt == null || string.IsNullOrEmpty(evt.CardKey)) return;
			var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.Customization) return;
			var st = EntityManager.GetEntitiesWithComponent<CustomizationState>().FirstOrDefault()?.GetComponent<CustomizationState>();
			if (st == null) return;
			// Only add if not already present, to keep library unique filtering meaningful
			if (!st.WorkingCardIds.Contains(evt.CardKey))
			{
				st.WorkingCardIds.Add(evt.CardKey);
			}
		}

		private void OnRemoveRequested(RemoveCardFromLoadoutRequested evt)
		{
			if (evt == null || string.IsNullOrEmpty(evt.CardKey)) return;
			var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.Customization) return;
			var st = EntityManager.GetEntitiesWithComponent<CustomizationState>().FirstOrDefault()?.GetComponent<CustomizationState>();
			if (st == null) return;

			if (evt.Index.HasValue)
			{
				int i = evt.Index.Value;
				if (i >= 0 && i < st.WorkingCardIds.Count && st.WorkingCardIds[i] == evt.CardKey)
				{
					st.WorkingCardIds.RemoveAt(i);
					return;
				}
			}
			// Fallback: remove first match
			int idx = st.WorkingCardIds.IndexOf(evt.CardKey);
			if (idx >= 0) st.WorkingCardIds.RemoveAt(idx);
		}

		private void OnUpdateTemperanceRequested(UpdateTemperanceLoadoutRequested evt)
		{
			if (evt == null || string.IsNullOrEmpty(evt.TemperanceId)) return;
			var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.Customization) return;
			var st = EntityManager.GetEntitiesWithComponent<CustomizationState>().FirstOrDefault()?.GetComponent<CustomizationState>();
			if (st == null) return;
			st.WorkingTemperanceId = evt.TemperanceId;
		}
	}
}


