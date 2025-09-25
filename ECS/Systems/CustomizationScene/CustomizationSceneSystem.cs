using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.IO;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Data.Loadouts;
using Crusaders30XX.ECS.Events;
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
        public int ButtonWidth { get; set; } = 140;

        [DebugEditable(DisplayName = "Bottom Button Height", Step = 2, Min = 24, Max = 200)]
        public int ButtonHeight { get; set; } = 56;

        [DebugEditable(DisplayName = "Panel Padding", Step = 2, Min = 0, Max = 64)]
        public int Padding { get; set; } = 12;

        [DebugEditable(DisplayName = "Bottom Margin", Step = 2, Min = 0, Max = 200)]
        public int BottomMargin { get; set; } = 20;

        [DebugEditable(DisplayName = "Button Spacing", Step = 2, Min = 0, Max = 200)]
        public int ButtonSpacing { get; set; } = 16;

        [DebugEditable(DisplayName = "Button Text Scale", Step = 0.01f, Min = 0.1f, Max = 1.0f)]
        public float ButtonTextScale { get; set; } = 0.12f;

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
			// Block clicks during scene transition
			if (TransitionStateSingleton.IsActive)
            {
                _prevMouse = mouse;
                return;
            }
            int vw = _graphicsDevice.Viewport.Width;
            int vh = _graphicsDevice.Viewport.Height;

            var (saveRect, cancelRect, undoRect, exitRect) = EnsureAndLayoutButtons(vw, vh);

            if (click)
            {
                if (saveRect.Contains(mouse.Position))
                {
                    var stc = EntityManager.GetEntitiesWithComponent<CustomizationState>().FirstOrDefault()?.GetComponent<CustomizationState>();
                    if (stc != null && stc.WorkingCardIds != null && stc.WorkingCardIds.Count == Crusaders30XX.ECS.Data.Loadouts.DeckRules.RequiredDeckSize)
                    {
                        SaveWorkingToDisk();
                    }
                }
                else if (cancelRect.Contains(mouse.Position)) RevertWorking();
                else if (undoRect.Contains(mouse.Position)) UndoWorking();
                else if (exitRect.Contains(mouse.Position))
                {
                    EventManager.Publish(new ShowTransition { Scene = SceneId.Menu });
                    TimerScheduler.Schedule(.8f, () => {
                        // Clear customization state before exiting
                        var stc = EntityManager.GetEntitiesWithComponent<CustomizationState>().FirstOrDefault();
                        if (stc != null)
                        {
                            EntityManager.DestroyEntity(stc.Id);
                        }
                    });
                }
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
            var (saveRect, cancelRect, undoRect, exitRect) = EnsureAndLayoutButtons(vw, vh);
            bool canSave = false;
            try
            {
                var st = EntityManager.GetEntitiesWithComponent<CustomizationState>().FirstOrDefault()?.GetComponent<CustomizationState>();
                canSave = st != null && st.WorkingCardIds != null && st.WorkingCardIds.Count == Crusaders30XX.ECS.Data.Loadouts.DeckRules.RequiredDeckSize;
            }
            catch {}
            DrawButton(saveRect, canSave ? "SAVE" : $"SAVE ({(EntityManager.GetEntitiesWithComponent<CustomizationState>().FirstOrDefault()?.GetComponent<CustomizationState>()?.WorkingCardIds?.Count ?? 0)}/{Crusaders30XX.ECS.Data.Loadouts.DeckRules.RequiredDeckSize})");
            DrawButton(cancelRect, "CANCEL");
            DrawButton(undoRect, "UNDO");
            DrawButton(exitRect, "EXIT");
        }

        private void DrawButton(Rectangle rect, string label)
        {
            var pixel = new Texture2D(_graphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });
            bool isDisabled = label.StartsWith("SAVE (");
            _spriteBatch.Draw(pixel, rect, isDisabled ? new Color(40, 40, 40) : Color.Black);
            var size = _font.MeasureString(label) * ButtonTextScale;
            var pos = new Vector2(rect.X + (rect.Width - size.X) / 2f, rect.Y + (rect.Height - size.Y) / 2f);
            var textShadow = isDisabled ? new Color(70, 70, 70) : Color.Black;
            var textMain = isDisabled ? new Color(170, 170, 170) : Color.White;
            _spriteBatch.DrawString(_font, label, pos + new Vector2(1, 1), textShadow, 0f, Vector2.Zero, ButtonTextScale, SpriteEffects.None, 0f);
            _spriteBatch.DrawString(_font, label, pos, textMain, 0f, Vector2.Zero, ButtonTextScale, SpriteEffects.None, 0f);
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
            // Reload from saved loadout definition to ensure full revert to on-disk state
            LoadoutDefinition def;
            if (!LoadoutDefinitionCache.TryGet("loadout_1", out def) || def == null)
            {
                def = new LoadoutDefinition { id = "loadout_1", name = "Loadout 1" };
            }
            var saved = new List<string>(def.cardIds ?? new List<string>());
            st.WorkingCardIds = saved;
            st.OriginalCardIds = new List<string>(saved);
        }

        private void UndoWorking()
        {
            var st = EntityManager.GetEntitiesWithComponent<CustomizationState>().FirstOrDefault()?.GetComponent<CustomizationState>();
            if (st == null) return;
            st.WorkingCardIds = new List<string>(st.OriginalCardIds ?? new List<string>());
        }

        private (Rectangle saveRect, Rectangle cancelRect, Rectangle undoRect, Rectangle exitRect) EnsureAndLayoutButtons(int vw, int vh)
        {
            // Four buttons centered: Save, Cancel, Undo, Exit
            int count = 4;
            int totalW = ButtonWidth * count + ButtonSpacing * (count - 1);
            int startX = vw / 2 - totalW / 2;
            int y = vh - BottomMargin - ButtonHeight;
            var saveRect = new Rectangle(startX, y, ButtonWidth, ButtonHeight);
            var cancelRect = new Rectangle(saveRect.Right + ButtonSpacing, y, ButtonWidth, ButtonHeight);
            var undoRect = new Rectangle(cancelRect.Right + ButtonSpacing, y, ButtonWidth, ButtonHeight);
            var exitRect = new Rectangle(undoRect.Right + ButtonSpacing, y, ButtonWidth, ButtonHeight);

            EnsureButtonEntity("Customization_SaveButton", saveRect, "Save current deck");
            EnsureButtonEntity("Customization_CancelButton", cancelRect, "Reload saved deck");
            EnsureButtonEntity("Customization_UndoButton", undoRect, "Undo to original");
            EnsureButtonEntity("Customization_ExitButton", exitRect, "Exit to menu");

            return (saveRect, cancelRect, undoRect, exitRect);
        }

        private void EnsureButtonEntity(string key, Rectangle rect, string tooltip)
        {
            var e = EntityManager.GetEntity(key);
            if (e == null)
            {
                e = EntityManager.CreateEntity(key);
                EntityManager.AddComponent(e, new Transform { Position = Vector2.Zero, ZOrder = 5000 });
                EntityManager.AddComponent(e, new UIElement { Bounds = rect, IsInteractable = true, Tooltip = tooltip, TooltipPosition = TooltipPosition.Above });
            }
            else
            {
                var ui = e.GetComponent<UIElement>();
                if (ui != null) ui.Bounds = rect;
            }
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