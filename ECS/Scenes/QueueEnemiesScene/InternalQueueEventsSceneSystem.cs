using System;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Data.Enemies;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Queue Enemies Menu")]
	public class InternalQueueEventsSceneSystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly ContentManager _content;
		private readonly SpriteFont _font;
		private Texture2D _pixel;
		private readonly System.Collections.Generic.Dictionary<(int w, int h, int r), Texture2D> _roundedCache = new System.Collections.Generic.Dictionary<(int, int, int), Texture2D>();
		private System.Collections.Generic.Dictionary<string, Texture2D> _enemyTextureCache = new System.Collections.Generic.Dictionary<string, Texture2D>();
		private MouseState _prevMouse;
		private readonly System.Collections.Generic.Dictionary<string, int> _enemyButtonIds = new System.Collections.Generic.Dictionary<string, int>();
		private int _confirmButtonId = 0;
		private int _customizeButtonId = 0;
		private int _howToButtonId = 0;
		private readonly System.Collections.Generic.List<int> _selectionItemIds = new System.Collections.Generic.List<int>();

		private HowToPlayOverlaySystem _howToPlayOverlaySystem;

		[DebugEditable(DisplayName = "Columns", Step = 1, Min = 1, Max = 10)]
		public int GridColumns { get; set; } = 4;

		[DebugEditable(DisplayName = "Button Width", Step = 4, Min = 40, Max = 1000)]
		public int ButtonWidth { get; set; } = 348;

		[DebugEditable(DisplayName = "Button Height", Step = 4, Min = 40, Max = 1000)]
		public int ButtonHeight { get; set; } = 324;

		[DebugEditable(DisplayName = "Corner Radius", Step = 1, Min = 0, Max = 64)]
		public int CornerRadius { get; set; } = 49;

		[DebugEditable(DisplayName = "Grid Padding", Step = 2, Min = 0, Max = 200)]
		public int GridPadding { get; set; } = 12;

		[DebugEditable(DisplayName = "Text Scale", Step = 0.05f, Min = 0.3f, Max = 3f)]
		public float TextScale { get; set; } = 0.25f;

		[DebugEditable(DisplayName = "Top List Height", Step = 4, Min = 20, Max = 600)]
		public int TopListHeight { get; set; } = 108;

		[DebugEditable(DisplayName = "Top List Image Scale", Step = 0.05f, Min = 0.1f, Max = 3f)]
		public float TopListImageScale { get; set; } = 0.8f;

		[DebugEditable(DisplayName = "Confirm Width", Step = 4, Min = 40, Max = 600)]
		public int ConfirmWidth { get; set; } = 200;

		[DebugEditable(DisplayName = "Confirm Height", Step = 2, Min = 24, Max = 200)]
		public int ConfirmHeight { get; set; } = 48;

		[DebugEditable(DisplayName = "Grid Image Scale", Step = 0.05f, Min = 0.1f, Max = 3f)]
		public float GridImageScale { get; set; } = 0.95f;

		[DebugEditable(DisplayName = "Menu Y Offset", Step = 4, Min = -400, Max = 800)]
		public int MenuYOffset { get; set; } = 60;

		[DebugEditable(DisplayName = "Show HowTo Button", Step = 1, Min = 0, Max = 1)]
		public int ShowHowToButton { get; set; } = 1;

		public InternalQueueEventsSceneSystem(EntityManager em, GraphicsDevice gd, SpriteBatch sb, ContentManager content, SpriteFont font) : base(em)
		{
			_graphicsDevice = gd;
			_spriteBatch = sb;
			_content = content;
			_font = font;
			_prevMouse = Mouse.GetState();
			_pixel = new Texture2D(gd, 1, 1);
			_pixel.SetData(new[] { Color.White });
			_howToPlayOverlaySystem = new HowToPlayOverlaySystem(EntityManager, _graphicsDevice, _spriteBatch, _font);
			EventManager.Subscribe<ShowTransition>(_ => OnShowTransition());
		}

		private void OnShowTransition()
		{
			// Destroy UI entities and clear caches when transitioning away
			foreach (var id in _enemyButtonIds.Values)
			{
				EntityManager.DestroyEntity(id);
			}
			_enemyButtonIds.Clear();
			if (_confirmButtonId != 0) { EntityManager.DestroyEntity(_confirmButtonId); _confirmButtonId = 0; }
			if (_customizeButtonId != 0) { EntityManager.DestroyEntity(_customizeButtonId); _customizeButtonId = 0; }
			if (_howToButtonId != 0) { EntityManager.DestroyEntity(_howToButtonId); _howToButtonId = 0; }
			foreach (var id in _selectionItemIds)
			{
				EntityManager.DestroyEntity(id);
			}
			_selectionItemIds.Clear();
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			var scene = entity.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.Internal_QueueEventsMenu) return;
			var mouse = Mouse.GetState();
			// Block clicks during scene transition
			if (TransitionStateSingleton.IsActive)
			{
				_prevMouse = mouse;
				return;
			}

			// If How To Play overlay is open, close it on any click and block underlying interactions
			var howOverlay = EntityManager.GetEntitiesWithComponent<HowToPlayOverlay>().FirstOrDefault()?.GetComponent<HowToPlayOverlay>();
			if (howOverlay != null && howOverlay.IsOpen)
			{
				// Pump overlay input (mouse wheel scroll)
				_howToPlayOverlaySystem?.UpdateFromMenu();
				// Overlay manages its own close; skip menu interactions while open
				// While open, do not process other menu interactions
				_prevMouse = mouse;
				return;
			}

			// Ensure queued enemies component exists
			var qeEntity = EntityManager.GetEntitiesWithComponent<QueuedEvents>().FirstOrDefault();
			if (qeEntity == null)
			{
				qeEntity = EntityManager.CreateEntity("QueuedEvents");
				EntityManager.AddComponent(qeEntity, new QueuedEvents());
			}
			var qe = qeEntity.GetComponent<QueuedEvents>();

			int vw = _graphicsDevice.Viewport.Width;
			int vh = _graphicsDevice.Viewport.Height;
			int gridTop = TopListHeight + GridPadding + MenuYOffset;
			int usableH = vh - gridTop - GridPadding - ConfirmHeight - GridPadding;
			int col = System.Math.Max(1, GridColumns);
			int cellW = ButtonWidth;
			int cellH = ButtonHeight;
			int totalW = col * cellW + (col - 1) * GridPadding;
			int startX = System.Math.Max(0, (vw - totalW) / 2);

			// Enemy buttons via UI entities
			var all = EnemyDefinitionCache.GetAll();
			int i = 0;
			foreach (var kv in all)
			{
				int r = i / col;
				int c = i % col;
				int x = startX + c * (cellW + GridPadding);
				int y = gridTop + r * (cellH + GridPadding);
				var rect = new Rectangle(x, y, cellW, cellH);
				var enemyButton = EnsureEnemyButton(kv.Key, rect);
				var ui = enemyButton?.GetComponent<UIElement>();
				if (ui != null && ui.IsClicked)
				{
					qe.Events.Add(new QueuedEvent { EventType = QueuedEventType.Enemy, EventId = kv.Key });
				}
				i++;
			}

			// Selection list items (UI elements; click to remove)
			int sx = GridPadding;
			int sy = GridPadding;
			int itemW = 180;
			int itemH = TopListHeight - GridPadding * 2;
			EnsureSelectionItemCapacity(qe.Events.Count);
			for (int idx = 0; idx < qe.Events.Count; idx++)
			{
				var rct = new Rectangle(sx, sy, itemW, itemH);
				var eSel = EnsureSelectionItem(idx, rct);
				var uiSel = eSel?.GetComponent<UIElement>();
				if (uiSel != null && uiSel.IsClicked)
				{
					qe.Events.RemoveAt(idx);
					break;
				}
				sx += itemW + GridPadding;
			}

			// Confirm button via UI element
			var confirmRect = new Rectangle((vw - ConfirmWidth) / 2, vh - GridPadding - ConfirmHeight, ConfirmWidth, ConfirmHeight);
			var eConfirm = EnsureConfirmButton(confirmRect);
			var uiConfirm = eConfirm?.GetComponent<UIElement>();
			if (uiConfirm != null && uiConfirm.IsClicked && qe.Events.Count > 0)
			{
				EventManager.Publish(new ShowTransition{ Scene = SceneId.Battle });
			}

			// Customize button -> go to Customization scene
			var customizeRect = new Rectangle(confirmRect.Right + GridPadding, confirmRect.Y, ConfirmWidth, ConfirmHeight);
			var eCustomize = EnsureCustomizeButton(customizeRect);
			var uiCustomize = eCustomize?.GetComponent<UIElement>();
			if (uiCustomize != null && uiCustomize.IsClicked)
			{
				EventManager.Publish(new ShowTransition { Scene = SceneId.Customization });
			}

			// How To Play button via UI element
			if (ShowHowToButton != 0)
			{
				int howW = ConfirmWidth;
				int howH = ConfirmHeight;
				var howRect = new Rectangle(confirmRect.X - GridPadding - howW, confirmRect.Y, howW, howH);
				var eHow = EnsureHowToButton(howRect);
				var uiHow = eHow?.GetComponent<UIElement>();
				if (uiHow != null && uiHow.IsClicked)
				{
					var h = EntityManager.GetEntitiesWithComponent<HowToPlayOverlay>().FirstOrDefault();
					if (h == null)
					{
						h = EntityManager.CreateEntity("HowToOverlay");
						EntityManager.AddComponent(h, new HowToPlayOverlay { IsOpen = true });
						EntityManager.AddComponent(h, new Transform { Position = Vector2.Zero, ZOrder = 30000 });
					}
					else
					{
						var st = h.GetComponent<HowToPlayOverlay>();
						if (st != null) st.IsOpen = true;
					}
				}
			}

			_prevMouse = mouse;
		}

		public void Draw()
		{
			var state = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
			if (state == null || state.Current != SceneId.Internal_QueueEventsMenu) return;
			if (_font == null) return;
			int vw = _graphicsDevice.Viewport.Width;
			int vh = _graphicsDevice.Viewport.Height;

			// Ensure texture cache warmed for button size
			var roundedGrid = GetRounded(ButtonWidth, ButtonHeight, CornerRadius);

			int gridTop = TopListHeight + GridPadding + MenuYOffset;
			int col = System.Math.Max(1, GridColumns);
			int cellW = ButtonWidth;
			int cellH = ButtonHeight;
			int totalW = col * cellW + (col - 1) * GridPadding;
			int startX = System.Math.Max(0, (vw - totalW) / 2);

			// Instructional text (drawn here to ensure SpriteBatch is active)
			string help = "Click on enemies to add to queue to battle, the press Confirm to start";
			var helpSize = _font.MeasureString(help) * 0.2f;
			float helpX = (vw - helpSize.X) / 2f;
			float helpY = System.Math.Max(GridPadding, gridTop - GridPadding - helpSize.Y - 4f);
			_spriteBatch.DrawString(_font, help, new Vector2(helpX + 1, helpY + 1), Color.Black, 0f, Vector2.Zero, 0.2f, SpriteEffects.None, 0f);
			_spriteBatch.DrawString(_font, help, new Vector2(helpX, helpY), Color.White, 0f, Vector2.Zero, 0.2f, SpriteEffects.None, 0f);

			// Enemy buttons grid
			var all = EnemyDefinitionCache.GetAll();
			int i = 0;
			foreach (var kv in all)
			{
				int r = i / col;
				int c = i % col;
				int x = startX + c * (cellW + GridPadding);
				int y = gridTop + r * (cellH + GridPadding);
				var rect = new Rectangle(x, y, cellW, cellH);
				// Button background
				_spriteBatch.Draw(roundedGrid, rect, Color.White);
				// Enemy image (PNG)
				var def = kv.Value;
				var tex = TryGetEnemyTexture(def);
				if (tex != null)
				{
					float scale = GridImageScale * System.Math.Min(rect.Width / (float)tex.Width, rect.Height / (float)tex.Height);
					var origin = new Vector2(tex.Width / 2f, tex.Height / 2f);
					var center = new Vector2(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f);
					_spriteBatch.Draw(tex, center, null, Color.White, 0f, origin, scale, SpriteEffects.None, 0f);
				}
				// Enemy name text (overlay)
				string name = def.name ?? kv.Key;
				var size = _font.MeasureString(name) * TextScale;
				var pos = new Vector2(x + (cellW - size.X) / 2f, y + (cellH - size.Y) / 2f);
				_spriteBatch.DrawString(_font, name, pos + new Vector2(1, 1), Color.Black, 0f, Vector2.Zero, TextScale, SpriteEffects.None, 0f);
				_spriteBatch.DrawString(_font, name, pos, Color.White, 0f, Vector2.Zero, TextScale, SpriteEffects.None, 0f);
				i++;
				FrameProfiler.Measure("HowToPlayOverlaySystem.Draw", _howToPlayOverlaySystem.Draw);
			}

			// Selection list at top
			var qe = EntityManager.GetEntitiesWithComponent<QueuedEvents>().FirstOrDefault()?.GetComponent<QueuedEvents>();
			if (qe != null)
			{
				int sx = GridPadding;
				int sy = GridPadding;
				int itemW = 180;
				int itemH = TopListHeight - GridPadding * 2;
				var roundedItem = GetRounded(itemW, itemH, CornerRadius);
				for (int idx = 0; idx < qe.Events.Count; idx++)
				{
					var rct = new Rectangle(sx, sy, itemW, itemH);
					_spriteBatch.Draw(roundedItem, rct, Color.White);
					string id = qe.Events[idx].EventId;
					EnemyDefinition def;
					EnemyDefinitionCache.TryGet(id, out def);
					var tex = def != null ? TryGetEnemyTexture(def) : null;
					if (tex != null)
					{
						float scale = TopListImageScale * System.Math.Min(rct.Width / (float)tex.Width, rct.Height / (float)tex.Height);
						var origin = new Vector2(tex.Width / 2f, tex.Height / 2f);
						var center = new Vector2(rct.X + rct.Width / 2f, rct.Y + rct.Height / 2f);
						_spriteBatch.Draw(tex, center, null, Color.White, 0f, origin, scale, SpriteEffects.None, 0f);
					}
					sx += itemW + GridPadding;
				}
			}

			// Confirm button
			var confirmRect = new Rectangle((vw - ConfirmWidth) / 2, vh - GridPadding - ConfirmHeight, ConfirmWidth, ConfirmHeight);
			var roundedConfirm = GetRounded(ConfirmWidth, ConfirmHeight, CornerRadius);
			_spriteBatch.Draw(roundedConfirm, confirmRect, Color.Black);
			var ctext = "Confirm";
			var csize = _font.MeasureString(ctext) * 0.225f;
			var cpos = new Vector2(confirmRect.X + (ConfirmWidth - csize.X) / 2f, confirmRect.Y + (ConfirmHeight - csize.Y) / 2f);
			_spriteBatch.DrawString(_font, ctext, cpos + new Vector2(1, 1), Color.Black, 0f, Vector2.Zero, 0.225f, SpriteEffects.None, 0f);
			_spriteBatch.DrawString(_font, ctext, cpos, Color.White, 0f, Vector2.Zero, 0.225f, SpriteEffects.None, 0f);

			// How To Play button (left of Confirm)
			if (ShowHowToButton != 0)
			{
				int howW = ConfirmWidth;
				int howH = ConfirmHeight;
				var howRect = new Rectangle(confirmRect.X - GridPadding - howW, confirmRect.Y, howW, howH);
				var roundedHow = GetRounded(howW, howH, CornerRadius);
				_spriteBatch.Draw(roundedHow, howRect, Color.Black);
				var htext = "How To Play";
				var hsize = _font.MeasureString(htext) * 0.2f;
				var hpos = new Vector2(howRect.X + (howW - hsize.X) / 2f, howRect.Y + (howH - hsize.Y) / 2f);
				_spriteBatch.DrawString(_font, htext, hpos + new Vector2(1, 1), Color.Black, 0f, Vector2.Zero, 0.2f, SpriteEffects.None, 0f);
				_spriteBatch.DrawString(_font, htext, hpos, Color.White, 0f, Vector2.Zero, 0.2f, SpriteEffects.None, 0f);
			}

			// Customize button (right of Confirm)
			{
				int custW = ConfirmWidth;
				int custH = ConfirmHeight;
				var customizeRect = new Rectangle(confirmRect.Right + GridPadding, confirmRect.Y, custW, custH);
				var roundedCust = GetRounded(custW, custH, CornerRadius);
				_spriteBatch.Draw(roundedCust, customizeRect, Color.Black);
				var t = "Customize";
				var ts = _font.MeasureString(t) * 0.2f;
				var tp = new Vector2(customizeRect.X + (custW - ts.X) / 2f, customizeRect.Y + (custH - ts.Y) / 2f);
				_spriteBatch.DrawString(_font, t, tp + new Vector2(1, 1), Color.Black, 0f, Vector2.Zero, 0.2f, SpriteEffects.None, 0f);
				_spriteBatch.DrawString(_font, t, tp, Color.White, 0f, Vector2.Zero, 0.2f, SpriteEffects.None, 0f);
			}
		}

		private Texture2D GetRounded(int w, int h, int radius)
		{
			var key = (w: w, h: h, r: System.Math.Max(0, System.Math.Min(radius, System.Math.Min(w, h) / 2)));
			if (_roundedCache.TryGetValue(key, out var tex) && tex != null) return tex;
			tex = RoundedRectTextureFactory.CreateRoundedRect(_graphicsDevice, w, h, key.r);
			_roundedCache[key] = tex;
			return tex;
		}

		private Entity EnsureEnemyButton(string enemyId, Rectangle bounds)
		{
			if (!_enemyButtonIds.TryGetValue(enemyId, out var id) || EntityManager.GetEntity(id) == null)
			{
				var e = EntityManager.CreateEntity($"MenuEnemy_{enemyId}");
				EntityManager.AddComponent(e, new UIElement { Bounds = bounds, IsInteractable = true });
				EntityManager.AddComponent(e, new Transform { Position = new Vector2(bounds.X, bounds.Y), ZOrder = 100 });
				_enemyButtonIds[enemyId] = e.Id;
				return e;
			}
			var existing = EntityManager.GetEntity(id);
			var ui = existing.GetComponent<UIElement>();
			if (ui != null) ui.Bounds = bounds;
			return existing;
		}

		private Entity EnsureConfirmButton(Rectangle bounds)
		{
			if (_confirmButtonId == 0 || EntityManager.GetEntity(_confirmButtonId) == null)
			{
				var e = EntityManager.CreateEntity("Menu_Confirm");
				EntityManager.AddComponent(e, new UIElement { Bounds = bounds, IsInteractable = true });
				EntityManager.AddComponent(e, new Transform { Position = new Vector2(bounds.X, bounds.Y), ZOrder = 70000 });
				_confirmButtonId = e.Id;
				return e;
			}
			var ex = EntityManager.GetEntity(_confirmButtonId);
			var ui = ex.GetComponent<UIElement>();
			if (ui != null) ui.Bounds = bounds;
			var t = ex.GetComponent<Transform>();
			if (t != null) t.ZOrder = 70000;
			return ex;
		}

		private Entity EnsureCustomizeButton(Rectangle bounds)
		{
			if (_customizeButtonId == 0 || EntityManager.GetEntity(_customizeButtonId) == null)
			{
				var e = EntityManager.CreateEntity("Menu_Customize");
				EntityManager.AddComponent(e, new UIElement { Bounds = bounds, IsInteractable = true });
				EntityManager.AddComponent(e, new Transform { Position = new Vector2(bounds.X, bounds.Y), ZOrder = 70000 });
				_customizeButtonId = e.Id;
				return e;
			}
			var ex = EntityManager.GetEntity(_customizeButtonId);
			var ui = ex.GetComponent<UIElement>();
			if (ui != null) ui.Bounds = bounds;
			var t = ex.GetComponent<Transform>();
			if (t != null) t.ZOrder = 70000;
			return ex;
		}

		private Entity EnsureHowToButton(Rectangle bounds)
		{
			if (_howToButtonId == 0 || EntityManager.GetEntity(_howToButtonId) == null)
			{
				var e = EntityManager.CreateEntity("Menu_HowTo");
				EntityManager.AddComponent(e, new UIElement { Bounds = bounds, IsInteractable = true });
				EntityManager.AddComponent(e, new Transform { Position = new Vector2(bounds.X, bounds.Y), ZOrder = 70000 });
				_howToButtonId = e.Id;
				return e;
			}
			var ex = EntityManager.GetEntity(_howToButtonId);
			var ui = ex.GetComponent<UIElement>();
			if (ui != null) ui.Bounds = bounds;
			var t = ex.GetComponent<Transform>();
			if (t != null) t.ZOrder = 70000;
			return ex;
		}

		private void EnsureSelectionItemCapacity(int count)
		{
			if (_selectionItemIds.Count == count) return;
			foreach (var id in _selectionItemIds) EntityManager.DestroyEntity(id);
			_selectionItemIds.Clear();
			for (int i = 0; i < count; i++)
			{
				var e = EntityManager.CreateEntity($"Menu_Selected_{i}");
				EntityManager.AddComponent(e, new UIElement { Bounds = new Rectangle(0,0,1,1), IsInteractable = true });
				EntityManager.AddComponent(e, new Transform { Position = Vector2.Zero, ZOrder = 115 });
				_selectionItemIds.Add(e.Id);
			}
		}

		private Entity EnsureSelectionItem(int index, Rectangle bounds)
		{
			if (index < 0 || index >= _selectionItemIds.Count) return null;
			var e = EntityManager.GetEntity(_selectionItemIds[index]);
			if (e == null) return null;
			var ui = e.GetComponent<UIElement>();
			if (ui != null) ui.Bounds = bounds;
			return e;
		}

		private Texture2D TryGetEnemyTexture(EnemyDefinition def)
		{
			if (def == null) return null;
			// Convention: content texture name is PascalCase of id (e.g., demon -> Demon)
			string key = def.id ?? "";
			if (string.IsNullOrEmpty(key)) return null;
			if (_enemyTextureCache.TryGetValue(key, out var cached)) return cached;
			string contentName = char.ToUpperInvariant(key[0]) + key.Substring(1);
			try
			{
				Console.WriteLine($"Loading enemy texture: {key}");
				var tex = _content.Load<Texture2D>(contentName);
				_enemyTextureCache[key] = tex;
				return tex;
			}
			catch
			{
				Console.WriteLine($"Error loading enemy texture: {key}");
				_enemyTextureCache[key] = null;
				return null;
			}
		}
	}
}


