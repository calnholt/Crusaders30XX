using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Crusaders30XX.ECS.Rendering;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Data.Cards;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Displays the currently equipped weapon as a gold circle with the weapon icon centered,
    /// positioned just above the discard pile display. Shows tooltip on hover.
    /// </summary>
    [DebugTab("Equipped Weapon Display")]
    public class EquippedWeaponDisplaySystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly ContentManager _content;
        private Texture2D _weaponTex;
		private const string RootEntityName = "UI_EquippedWeaponRoot";

        // Layout/debug controls
        [DebugEditable(DisplayName = "Circle Radius", Step = 1, Min = 8, Max = 400)]
		public int CircleRadius { get; set; } = 30;

        [DebugEditable(DisplayName = "Above Discard Offset Y", Step = 1, Min = -400, Max = 400)]
		public int AboveDiscardOffsetY { get; set; } = 16;

        [DebugEditable(DisplayName = "Gold R", Step = 1, Min = 0, Max = 255)]
		public int GoldR { get; set; } = 215;
        [DebugEditable(DisplayName = "Gold G", Step = 1, Min = 0, Max = 255)]
		public int GoldG { get; set; } = 186;
        [DebugEditable(DisplayName = "Gold B", Step = 1, Min = 0, Max = 255)]
		public int GoldB { get; set; } = 147;

        [DebugEditable(DisplayName = "Icon Scale", Step = 0.01f, Min = 0.05f, Max = 2f)]
		public float IconScale { get; set; } = 1.2f;

        public EquippedWeaponDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, ContentManager content)
            : base(entityManager)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = spriteBatch;
            _content = content;
            TryLoadWeaponTexture();
        }

        private void TryLoadWeaponTexture()
        {
            // Try to load weapon sprite from Content by id of equipped weapon; fallback to sword or shield
            string[] fallbacks = new[] { "sword", "weapon_sword", "shield" };
            Texture2D tex = null;
            // Try to infer from EquippedWeapon if available
            var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
            var ew = player?.GetComponent<EquippedWeapon>();
            if (ew != null && !string.IsNullOrWhiteSpace(ew.WeaponId))
            {
                string[] candidates = new[] { ew.WeaponId, "weapon_" + ew.WeaponId };
                foreach (var c in candidates)
                {
                    try { tex = _content.Load<Texture2D>(c); } catch { tex = null; }
                    if (tex != null) break;
                }
            }
            if (tex == null)
            {
                foreach (var id in fallbacks)
                {
                    try { tex = _content.Load<Texture2D>(id); } catch { tex = null; }
                    if (tex != null) break;
                }
            }
            _weaponTex = tex;
        }

        protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<Player>();
        }

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			EnsureRootEntity();
			// Re-anchor weapon center relative to Discard root each frame; ParallaxLayer will offset after
			var discardRoot = EntityManager.GetEntity("UI_DiscardPileRoot");
			var discardUI = discardRoot?.GetComponent<UIElement>();
			var tRoot = EntityManager.GetEntity(RootEntityName)?.GetComponent<Transform>();
			if (tRoot != null)
			{
				if (discardUI != null && discardUI.Bounds.Width > 0 && discardUI.Bounds.Height > 0)
				{
					int r = System.Math.Max(4, CircleRadius);
					var dr = discardUI.Bounds;
					tRoot.BasePosition = new Vector2(dr.Center.X, dr.Y - AboveDiscardOffsetY - r);
				}
			}
		}

        public void Draw()
        {
            var player = GetRelevantEntities().FirstOrDefault();
            if (player == null) return;

            // Discover discard pile rect for anchoring
            var discardClickable = EntityManager.GetEntitiesWithComponent<DiscardPileClickable>().FirstOrDefault();
            Rectangle? discardRect = null;
            if (discardClickable != null)
            {
                var uiDP = discardClickable.GetComponent<UIElement>();
                if (uiDP != null && uiDP.Bounds.Width > 0 && uiDP.Bounds.Height > 0) discardRect = uiDP.Bounds;
            }

            int r = System.Math.Max(4, CircleRadius);
            var circle = PrimitiveTextureFactory.GetAntiAliasedCircle(_graphicsDevice, r);
			EnsureRootEntity();
			var root = EntityManager.GetEntity(RootEntityName);
			var tRoot = root?.GetComponent<Transform>();
            Vector2 center = new Vector2(100, _graphicsDevice.Viewport.Height - 200);
            if (discardRect.HasValue)
            {
                var dr = discardRect.Value;
                center = new Vector2(dr.Center.X, dr.Y - AboveDiscardOffsetY - r);
			}
			// Do not overwrite root position here; ParallaxLayer has already adjusted after Update
            else
            {
                // fall back to current root pos if available
                if (tRoot != null) center = tRoot.Position;
            }

            // Draw gold filled circle
            var gold = new Color(GoldR, GoldG, GoldB);
            _spriteBatch.Draw(circle, new Vector2(center.X - r, center.Y - r), gold);

            // Draw weapon icon centered if available
            if (_weaponTex == null)
            {
                // Attempt lazy-load if initially null (e.g., player created after system)
                TryLoadWeaponTexture();
            }
            if (_weaponTex != null)
            {
                // Fit within circle with padding, uniform scale
                float availableDiameter = r * 2f;
                float sx = (availableDiameter / _weaponTex.Width) * System.Math.Max(0.05f, IconScale);
                float sy = (availableDiameter / _weaponTex.Height) * System.Math.Max(0.05f, IconScale);
                float scale = System.Math.Min(sx, sy);
                var origin = new Vector2(_weaponTex.Width / 2f, _weaponTex.Height / 2f);
                _spriteBatch.Draw(_weaponTex, center, null, Color.White, 0f, origin, scale, SpriteEffects.None, 0f);
            }

			// Use root entity UIElement for hover/tooltip
			int hoverR = (int)System.Math.Ceiling(r * System.Math.Max(1f, IconScale));
			var hitRect = new Rectangle((int)(center.X - hoverR), (int)(center.Y - hoverR), hoverR * 2, hoverR * 2);
			var rootUi = root.GetComponent<UIElement>();
			if (rootUi == null)
			{
				EntityManager.AddComponent(root, new UIElement { Bounds = hitRect, Tooltip = BuildWeaponTooltip(), TooltipPosition = TooltipPosition.Right });
			}
			else
			{
				rootUi.Bounds = hitRect;
				rootUi.TooltipPosition = TooltipPosition.Right;
				rootUi.Tooltip = BuildWeaponTooltip();
			}
        }

        private string BuildWeaponTooltip()
        {
            var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
            var ew = player?.GetComponent<EquippedWeapon>();
            if (ew == null || string.IsNullOrWhiteSpace(ew.WeaponId)) return "Weapon";
            CardDefinitionCache.TryGet(ew.WeaponId, out var def);
            string name = $"{def.name} - {(def.isFreeAction ? "free action" : "1AP")}";
            string desc = def.text ?? string.Empty;
            string cost = (def.cost != null && def.cost.Length > 0) ? $"Cost: {string.Join(", ", def.cost)}" : string.Empty;
            return $"{name} {(cost != string.Empty ? $"\n{cost}" : string.Empty)}\n\n{desc}";
        }

	private void EnsureRootEntity()
	{
		var e = EntityManager.GetEntity(RootEntityName);
		if (e == null)
		{
			e = EntityManager.CreateEntity(RootEntityName);
			EntityManager.AddComponent(e, new Transform { Position = new Vector2(100, _graphicsDevice.Viewport.Height - 200), ZOrder = 10000 });
			EntityManager.AddComponent(e, ParallaxLayer.GetUIParallaxLayer());
		}
	}
    }
}


