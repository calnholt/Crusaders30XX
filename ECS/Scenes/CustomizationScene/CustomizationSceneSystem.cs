using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.IO;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Data.Loadouts;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Data.Cards;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using Crusaders30XX.ECS.Singletons;

namespace Crusaders30XX.ECS.Systems
{
    [DebugTab("Customization Scene")]
    public class CustomizationSceneSystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font = FontSingleton.ContentFont;
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

        public CustomizationSceneSystem(EntityManager entityManager, GraphicsDevice gd, SpriteBatch sb) : base(entityManager)
        {
            _graphicsDevice = gd;
            _spriteBatch = sb;
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

            // Block interactions during scene transition
            if (StateSingleton.IsActive)
            {
                return;
            }
            int vw = _graphicsDevice.Viewport.Width;
            int vh = _graphicsDevice.Viewport.Height;

            // Ensure entities exist and keep their transforms positioned by layout each frame
            EnsureAndLayoutButtons(vw, vh);

            // Compute Save enable state
            var stc = EntityManager.GetEntitiesWithComponent<CustomizationState>().FirstOrDefault()?.GetComponent<CustomizationState>();
            bool canSave = false;
            try
            {
                bool withinLimit = IsWithinNameCopyLimit(stc, out _, out _);
                canSave = stc != null && stc.WorkingCardIds != null && stc.WorkingCardIds.Count == DeckRules.RequiredDeckSize && withinLimit;
            }
            catch {}

            var saveE = EntityManager.GetEntity("Customization_SaveButton");
            var cancelE = EntityManager.GetEntity("Customization_CancelButton");
            var undoE = EntityManager.GetEntity("Customization_UndoButton");
            var exitE = EntityManager.GetEntity("Customization_ExitButton");
            if (saveE != null)
            {
                var hotKey = saveE.GetComponent<HotKey>();
                if (hotKey == null)
                {
                    EntityManager.AddComponent(saveE, new HotKey { Button = FaceButton.Y, RequiresHold = true, Position = HotKeyPosition.Top });
                }
            }
            if (exitE != null)
            {
                var hotKey = exitE.GetComponent<HotKey>();
                if (hotKey == null)
                {
                    EntityManager.AddComponent(exitE, new HotKey { Button = FaceButton.B, RequiresHold = true, Position = HotKeyPosition.Top });
                }
            }

            var saveUi = saveE?.GetComponent<UIElement>();
            var cancelUi = cancelE?.GetComponent<UIElement>();
            var undoUi = undoE?.GetComponent<UIElement>();
            var exitUi = exitE?.GetComponent<UIElement>();

            if (saveUi != null) saveUi.IsInteractable = canSave;
            if (cancelUi != null) cancelUi.IsInteractable = true;
            if (undoUi != null) undoUi.IsInteractable = true;
            if (exitUi != null) exitUi.IsInteractable = true;

            // Handle clicks delegated from InputSystem/HotKey via UIElement.IsClicked
            if (saveUi != null && saveUi.IsClicked && canSave)
            {
                SaveWorkingToDisk();
            }
            if (cancelUi != null && cancelUi.IsClicked)
            {
                RevertWorking();
            }
            if (undoUi != null && undoUi.IsClicked)
            {
                UndoWorking();
            }
            if (exitUi != null && exitUi.IsClicked)
            {
                EventManager.Publish(new ShowTransition { Scene = SceneId.Location });
                TimerScheduler.Schedule(.8f, () => {
                    // Clear customization state before exiting
                    var st = EntityManager.GetEntitiesWithComponent<CustomizationState>().FirstOrDefault();
                    if (st != null)
                    {
                        EntityManager.DestroyEntity(st.Id);
                    }
                });
            }
        }

        public void Draw()
        {
            var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
            if (scene == null || scene.Current != SceneId.Customization) return;
            int vw = _graphicsDevice.Viewport.Width;
            int vh = _graphicsDevice.Viewport.Height;

            // Ensure entities and layout first
            EnsureAndLayoutButtons(vw, vh);

            var st = EntityManager.GetEntitiesWithComponent<CustomizationState>().FirstOrDefault()?.GetComponent<CustomizationState>();
            bool withinLimit = IsWithinNameCopyLimit(st, out _, out _);
            bool canSave = st != null && st.WorkingCardIds != null && st.WorkingCardIds.Count == DeckRules.RequiredDeckSize && withinLimit;
            int count = st?.WorkingCardIds?.Count ?? 0;

            var saveE = EntityManager.GetEntity("Customization_SaveButton");
            var cancelE = EntityManager.GetEntity("Customization_CancelButton");
            var undoE = EntityManager.GetEntity("Customization_UndoButton");
            var exitE = EntityManager.GetEntity("Customization_ExitButton");

            var saveTr = saveE?.GetComponent<Transform>();
            var cancelTr = cancelE?.GetComponent<Transform>();
            var undoTr = undoE?.GetComponent<Transform>();
            var exitTr = exitE?.GetComponent<Transform>();

            var saveRect = new Rectangle((int)(saveTr?.Position.X ?? 0), (int)(saveTr?.Position.Y ?? 0), ButtonWidth, ButtonHeight);
            var cancelRect = new Rectangle((int)(cancelTr?.Position.X ?? 0), (int)(cancelTr?.Position.Y ?? 0), ButtonWidth, ButtonHeight);
            var undoRect = new Rectangle((int)(undoTr?.Position.X ?? 0), (int)(undoTr?.Position.Y ?? 0), ButtonWidth, ButtonHeight);
            var exitRect = new Rectangle((int)(exitTr?.Position.X ?? 0), (int)(exitTr?.Position.Y ?? 0), ButtonWidth, ButtonHeight);

            DrawButton(saveRect, canSave ? "SAVE" : $"SAVE ({count}/{DeckRules.RequiredDeckSize})");
            DrawButton(cancelRect, "CANCEL");
            DrawButton(undoRect, "UNDO");
            DrawButton(exitRect, "EXIT");

            // Keep UI bounds aligned with drawn rects
            var saveUi = saveE?.GetComponent<UIElement>();
            if (saveUi != null) saveUi.Bounds = saveRect;
            var cancelUi = cancelE?.GetComponent<UIElement>();
            if (cancelUi != null) cancelUi.Bounds = cancelRect;
            var undoUi = undoE?.GetComponent<UIElement>();
            if (undoUi != null) undoUi.Bounds = undoRect;
            var exitUi = exitE?.GetComponent<UIElement>();
            if (exitUi != null) exitUi.Bounds = exitRect;
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
            // Initialize working temperance from loadout if present, else from player's current equip
            st.WorkingTemperanceId = def.temperanceId ?? st.WorkingTemperanceId;
            st.WorkingWeaponId = def.weaponId ?? st.WorkingWeaponId;
            st.WorkingChestId = def.chestId ?? st.WorkingChestId;
            st.WorkingLegsId = def.legsId ?? st.WorkingLegsId;
            st.WorkingArmsId = def.armsId ?? st.WorkingArmsId;
            st.WorkingHeadId = def.headId ?? st.WorkingHeadId;
            st.WorkingMedalIds = new List<string>(def.medalIds ?? new List<string>());
            st.OriginalTemperanceId = st.WorkingTemperanceId;
            st.OriginalWeaponId = st.WorkingWeaponId;
            st.OriginalChestId = st.WorkingChestId;
            st.OriginalLegsId = st.WorkingLegsId;
            st.OriginalArmsId = st.WorkingArmsId;
            st.OriginalHeadId = st.WorkingHeadId;
            st.OriginalMedalIds = new List<string>(st.WorkingMedalIds);
            EntityManager.AddComponent(stateEntity, st);
            if (string.IsNullOrEmpty(st.WorkingTemperanceId))
            {
                var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
                var eq = player?.GetComponent<EquippedTemperanceAbility>();
                if (eq != null && !string.IsNullOrEmpty(eq.AbilityId)) st.WorkingTemperanceId = eq.AbilityId;
            }
            st.OriginalTemperanceId = st.WorkingTemperanceId;
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
            var savedMedals = new List<string>(def.medalIds ?? new List<string>());
            st.WorkingMedalIds = savedMedals;
            st.OriginalMedalIds = new List<string>(savedMedals);
        }

        private void UndoWorking()
        {
            var st = EntityManager.GetEntitiesWithComponent<CustomizationState>().FirstOrDefault()?.GetComponent<CustomizationState>();
            if (st == null) return;
            st.WorkingCardIds = new List<string>(st.OriginalCardIds ?? new List<string>());
            st.WorkingMedalIds = new List<string>(st.OriginalMedalIds ?? new List<string>());
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

            EnsureButtonEntity("Customization_SaveButton", saveRect);
            EnsureButtonEntity("Customization_CancelButton", cancelRect);
            EnsureButtonEntity("Customization_UndoButton", undoRect);
            EnsureButtonEntity("Customization_ExitButton", exitRect);

            return (saveRect, cancelRect, undoRect, exitRect);
        }

        private void EnsureButtonEntity(string key, Rectangle rect)
        {
            var e = EntityManager.GetEntity(key);
            if (e == null)
            {
                e = EntityManager.CreateEntity(key);
                EntityManager.AddComponent(e, new Transform { Position = new Vector2(rect.X, rect.Y), ZOrder = 5000 });
                EntityManager.AddComponent(e, new UIElement { Bounds = rect, IsInteractable = true });
            }
            else
            {
                var tr = e.GetComponent<Transform>();
                if (tr != null)
                {
                    tr.Position = new Vector2(rect.X, rect.Y);
                    tr.ZOrder = 5000;
                }
                var ui = e.GetComponent<UIElement>();
                if (ui != null) ui.Bounds = rect;
            }
        }

        private void SaveWorkingToDisk()
        {
            var st = EntityManager.GetEntitiesWithComponent<CustomizationState>().FirstOrDefault()?.GetComponent<CustomizationState>();
            if (st == null) return;
            if (!IsWithinNameCopyLimit(st, out var overName, out var overCount))
            {
                System.Console.WriteLine($"[Customization] Cannot save: more than 2 copies of '{overName}' ({overCount}).");
                return;
            }
            // Load existing def and overwrite cardIds
            if (!LoadoutDefinitionCache.TryGet("loadout_1", out var def) || def == null)
            {
                def = new LoadoutDefinition { id = "loadout_1", name = "Loadout 1" };
            }
            def.cardIds = new List<string>(st.WorkingCardIds);
            def.temperanceId = st.WorkingTemperanceId;
            def.weaponId = st.WorkingWeaponId;
            def.chestId = st.WorkingChestId;
            def.legsId = st.WorkingLegsId;
            def.armsId = st.WorkingArmsId;
            def.headId = st.WorkingHeadId;
            def.medalIds = new List<string>(st.WorkingMedalIds ?? new List<string>());

            SaveCache.SaveLoadout(def);
            st.OriginalCardIds = new List<string>(st.WorkingCardIds);
            st.OriginalMedalIds = new List<string>(st.WorkingMedalIds ?? new List<string>());
        }

        private static bool IsWithinNameCopyLimit(Crusaders30XX.ECS.Components.CustomizationState st, out string overName, out int count)
        {
            overName = null; count = 0;
            if (st?.WorkingCardIds == null) return true;

            var idToName = CardDefinitionCache.GetAll()
                .ToDictionary(kv => (kv.Key ?? string.Empty).ToLowerInvariant(),
                              kv => ((kv.Value?.name ?? kv.Value?.id) ?? string.Empty).Trim());

            var nameCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in st.WorkingCardIds)
            {
                var baseId = ((key?.Split('|')[0]) ?? string.Empty).ToLowerInvariant();
                var displayName = idToName.TryGetValue(baseId, out var n) ? n : baseId;
                var newCount = (nameCounts.TryGetValue(displayName, out var c) ? c : 0) + 1;
                nameCounts[displayName] = newCount;
                if (newCount > 2)
                {
                    overName = displayName; count = newCount; return false;
                }
            }
            return true;
        }
    }
}