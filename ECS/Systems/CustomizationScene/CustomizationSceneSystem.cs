using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.IO;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Data.Loadouts;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Crusaders30XX.ECS.Systems
{
    [DebugTab("Customization Scene")]
    public class CustomizationSceneSystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private MouseState _prevMouse;

        [DebugEditable(DisplayName = "Bottom Button Width", Step = 4, Min = 40, Max = 600)]
        public int ButtonWidth { get; set; } = 220;

        [DebugEditable(DisplayName = "Bottom Button Height", Step = 2, Min = 24, Max = 200)]
        public int ButtonHeight { get; set; } = 56;

        [DebugEditable(DisplayName = "Panel Padding", Step = 2, Min = 0, Max = 64)]
        public int Padding { get; set; } = 12;

        public CustomizationSceneSystem(EntityManager entityManager, GraphicsDevice gd, SpriteBatch sb, SpriteFont font) : base(entityManager)
        {
            _graphicsDevice = gd;
            _spriteBatch = sb;
            _font = font;
            _prevMouse = Mouse.GetState();
        }

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<SceneState>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            var scene = entity.GetComponent<SceneState>();
            if (scene == null || scene.Current != SceneId.Customization) return;
            EnsureStateLoaded();

            var mouse = Mouse.GetState();
            bool click = mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released;
            int vw = _graphicsDevice.Viewport.Width;
            int vh = _graphicsDevice.Viewport.Height;

            var saveRect = new Rectangle(vw / 2 - ButtonWidth - Padding, vh - Padding - ButtonHeight, ButtonWidth, ButtonHeight);
            var cancelRect = new Rectangle(vw / 2 + Padding, vh - Padding - ButtonHeight, ButtonWidth, ButtonHeight);

            if (click)
            {
                if (saveRect.Contains(mouse.Position)) SaveWorkingToDisk();
                else if (cancelRect.Contains(mouse.Position)) RevertWorking();
            }

            _prevMouse = mouse;
        }

        public void Draw()
        {
            var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
            if (scene == null || scene.Current != SceneId.Customization) return;
            int vw = _graphicsDevice.Viewport.Width;
            int vh = _graphicsDevice.Viewport.Height;

            // Buttons
            var saveRect = new Rectangle(vw / 2 - ButtonWidth - Padding, vh - Padding - ButtonHeight, ButtonWidth, ButtonHeight);
            var cancelRect = new Rectangle(vw / 2 + Padding, vh - Padding - ButtonHeight, ButtonWidth, ButtonHeight);
            DrawButton(saveRect, "SAVE");
            DrawButton(cancelRect, "CANCEL");
        }

        private void DrawButton(Rectangle rect, string label)
        {
            var pixel = new Texture2D(_graphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });
            _spriteBatch.Draw(pixel, rect, Color.Black);
            var size = _font.MeasureString(label) * 0.25f;
            var pos = new Vector2(rect.X + (rect.Width - size.X) / 2f, rect.Y + (rect.Height - size.Y) / 2f);
            _spriteBatch.DrawString(_font, label, pos + new Vector2(1, 1), Color.Black, 0f, Vector2.Zero, 0.25f, SpriteEffects.None, 0f);
            _spriteBatch.DrawString(_font, label, pos, Color.White, 0f, Vector2.Zero, 0.25f, SpriteEffects.None, 0f);
            pixel.Dispose();
        }

        private void EnsureStateLoaded()
        {
            var stateEntity = EntityManager.GetEntitiesWithComponent<CustomizationState>().FirstOrDefault();
            if (stateEntity != null) return;
            stateEntity = EntityManager.CreateEntity("CustomizationState");
            var st = new CustomizationState();
            LoadoutDefinition def;
            if (!LoadoutDefinitionCache.TryGet("loadout_1", out def) || def == null)
            {
                def = new LoadoutDefinition { id = "loadout_1", name = "Loadout 1" };
            }
            st.WorkingCardIds = new List<string>(def.cardIds ?? new List<string>());
            st.OriginalCardIds = new List<string>(st.WorkingCardIds);
            EntityManager.AddComponent(stateEntity, st);
        }

        private void RevertWorking()
        {
            var st = EntityManager.GetEntitiesWithComponent<CustomizationState>().FirstOrDefault()?.GetComponent<CustomizationState>();
            if (st == null) return;
            st.WorkingCardIds = new List<string>(st.OriginalCardIds);
        }

        private void SaveWorkingToDisk()
        {
            var st = EntityManager.GetEntitiesWithComponent<CustomizationState>().FirstOrDefault()?.GetComponent<CustomizationState>();
            if (st == null) return;
            // Load existing def and overwrite cardIds
            if (!LoadoutDefinitionCache.TryGet("loadout_1", out var def) || def == null)
            {
                def = new LoadoutDefinition { id = "loadout_1", name = "Loadout 1" };
            }
            def.cardIds = new List<string>(st.WorkingCardIds);

            string folder = ResolveLoadoutsFolder();
            if (string.IsNullOrEmpty(folder)) return;
            string path = Path.Combine(folder, "loadout_1.json");
            var opts = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(def, opts);
            File.WriteAllText(path, json);
            LoadoutDefinitionCache.Reload();
            st.OriginalCardIds = new List<string>(st.WorkingCardIds);
        }

        private string ResolveLoadoutsFolder()
        {
            // mirror LoadoutDefinitionCache.ResolveFolderPath logic
            try
            {
                var dir = new DirectoryInfo(System.AppContext.BaseDirectory);
                for (int i = 0; i < 6 && dir != null; i++)
                {
                    var candidate = Path.Combine(dir.FullName, "Crusaders30XX.csproj");
                    if (File.Exists(candidate))
                    {
                        return Path.Combine(dir.FullName, "ECS", "Data", "Loadouts");
                    }
                    dir = dir.Parent;
                }
            }
            catch {}
            return null;
        }
    }
}