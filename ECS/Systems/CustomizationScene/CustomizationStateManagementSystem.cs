using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Systems
{
    public class CustomizationStateManagementSystem : Core.System
    {
        public CustomizationStateManagementSystem(EntityManager em) : base(em)
        {
            EventManager.Subscribe<SetCustomizationTab>(OnSetTab);
        }

        protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<SceneState>();
        }

        protected override void UpdateEntity(Entity entity, Microsoft.Xna.Framework.GameTime gameTime) { }

        private void OnSetTab(SetCustomizationTab evt)
        {
            var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
            if (scene == null || scene.Current != SceneId.Customization) return;
            var stEntity = EntityManager.GetEntitiesWithComponent<CustomizationState>().FirstOrDefault();
            var st = stEntity?.GetComponent<CustomizationState>();
            if (st == null) return;
            st.SelectedTab = evt.Tab;
        }
    }
}


