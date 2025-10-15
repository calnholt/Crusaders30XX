using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System.Collections.Generic;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Data.Medals;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Medal Display")]
	public class MedalDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly ContentManager _content;
		private SpriteFont _font;
		private Texture2D _fallbackMedalTex;
		private readonly Dictionary<string, Texture2D> _medalTexById = new Dictionary<string, Texture2D>();
		private Texture2D _roundedCache;
		private int _roundedW, _roundedH, _roundedR;
		private readonly Dictionary<int, float> _bounceByEntityId = new Dictionary<int, float>();
		private double _lastDt = 0.0;

		// Layout/debug controls
		[DebugEditable(DisplayName = "Left Margin", Step = 2, Min = 0, Max = 2000)]
		public int LeftMargin { get; set; } = 0;
		[DebugEditable(DisplayName = "Top Margin", Step = 2, Min = 0, Max = 2000)]
		public int TopMargin { get; set; } = 10;
		[DebugEditable(DisplayName = "Icon Size", Step = 1, Min = 8, Max = 512)]
		public int IconSize { get; set; } = 105;
		[DebugEditable(DisplayName = "Spacing X", Step = 1, Min = 0, Max = 256)]
		public int SpacingX { get; set; } = 0;
		[DebugEditable(DisplayName = "Background Corner Radius", Step = 1, Min = 0, Max = 64)]
		public int BgCornerRadius { get; set; } = 16;
		[DebugEditable(DisplayName = "Background Padding", Step = 1, Min = 0, Max = 64)]
		public int BgPadding { get; set; } = 0;
		[DebugEditable(DisplayName = "Background Opacity", Step = 0.05f, Min = 0f, Max = 1f)]
		public float BgOpacity { get; set; } = 0f;

		// Pulse/jiggle animation tuning
		[DebugEditable(DisplayName = "Pulse Duration (s)", Step = 0.05f, Min = 0.1f, Max = 2f)]
		public float PulseDurationSeconds { get; set; } = 0.5f;
		[DebugEditable(DisplayName = "Pulse Scale Amp", Step = 0.01f, Min = 0f, Max = 0.6f)]
		public float PulseScaleAmplitude { get; set; } = 0.44f;
		[DebugEditable(DisplayName = "Jiggle Degrees", Step = 0.5f, Min = 0f, Max = 45f)]
		public float JiggleDegrees { get; set; } = 5f;
		[DebugEditable(DisplayName = "Pulse Frequency (Hz)", Step = 0.1f, Min = 0.5f, Max = 8f)]
		public float PulseFrequencyHz { get; set; } = 1.7f;

		public MedalDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, ContentManager content, SpriteFont font)
			: base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			_content = content;
			_font = font;
			TryLoadAssets();
			EventManager.Subscribe<MedalTriggered>(OnMedalTriggered);
		}

		private void OnMedalTriggered(MedalTriggered evt)
		{
			if (evt?.MedalEntity == null) return;
			_bounceByEntityId[evt.MedalEntity.Id] = 0f; // start bounce timer
		}

		private void TryLoadAssets()
		{
			try { _fallbackMedalTex = _content.Load<Texture2D>("medal"); } catch { _fallbackMedalTex = null; }
		}

		private Texture2D GetMedalTexture(string medalId)
		{
			if (string.IsNullOrWhiteSpace(medalId)) return _fallbackMedalTex;
			if (_medalTexById.TryGetValue(medalId, out var cached) && cached != null) return cached;
			Texture2D tex = null;
			try { tex = _content.Load<Texture2D>(medalId); }
			catch { tex = _fallbackMedalTex; }
			_medalTexById[medalId] = tex;
			return tex;
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<Player>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		public override void Update(GameTime gameTime)
		{
			_lastDt = gameTime.ElapsedGameTime.TotalSeconds;
			// Decay active bounces over time
			if (_bounceByEntityId.Count > 0)
			{
				var keys = _bounceByEntityId.Keys.ToList();
				for (int i = 0; i < keys.Count; i++)
				{
					int id = keys[i];
					float t = _bounceByEntityId[id];
					t += (float)_lastDt;
					if (t >= 0.5f) _bounceByEntityId.Remove(id); else _bounceByEntityId[id] = t;
				}
			}
			base.Update(gameTime);
		}

		public void Draw()
		{
			var player = GetRelevantEntities().FirstOrDefault();
			if (player == null) return;
			var medals = EntityManager.GetEntitiesWithComponent<EquippedMedal>()
				.Where(e => e.GetComponent<EquippedMedal>().EquippedOwner == player)
				.Select(e => e.GetComponent<EquippedMedal>())
				.ToList();
			if (medals.Count == 0) return;

			int x = LeftMargin;
			int y = TopMargin;
			foreach (var m in medals)
			{
				int bgW = IconSize + BgPadding * 2;
				int bgH = IconSize + BgPadding * 2;
				// Ensure transform and parallax; write layout to BasePosition only
				var t = m.Owner.GetComponent<Transform>();
				if (t == null)
				{
					t = new Transform { BasePosition = new Vector2(x, y), Position = new Vector2(x, y), ZOrder = 10001 };
					EntityManager.AddComponent(m.Owner, t);
					if (m.Owner.GetComponent<ParallaxLayer>() == null)
					{
						EntityManager.AddComponent(m.Owner, ParallaxLayer.GetUIParallaxLayer());
					}
				}
				else
				{
					t.BasePosition = new Vector2(x, y);
				}
				var cur = t.Position;
				var rect = new Rectangle((int)System.Math.Round(cur.X), (int)System.Math.Round(cur.Y), bgW, bgH);
				// Backgrounds removed: draw medals without black rounded panels
				UpdateTooltipForMedal(m, rect);
				// Jiggle/pulse the medal icon only
				float scale = 1f;
				float rotation = 0f;
				if (_bounceByEntityId.TryGetValue(m.Owner.Id, out var tPulse))
				{
					float dur = System.Math.Max(0.1f, PulseDurationSeconds);
					float norm = MathHelper.Clamp(tPulse / dur, 0f, 1f);
					float env = (1f - norm);
					env *= env; // quadratic decay
					float phase = MathHelper.TwoPi * PulseFrequencyHz * tPulse;
					float s = (float)System.Math.Sin(phase);
					scale = 1f + PulseScaleAmplitude * env * s;
					float jiggleRad = MathHelper.ToRadians(JiggleDegrees);
					rotation = jiggleRad * env * (float)System.Math.Sin(phase * 1.2f);
				}
				var medalTex = GetMedalTexture(m.MedalId);
				var drawnRect = DrawMedalIcon(rect, medalTex, scale, rotation);
				// Advance by intended layout width to preserve consistent margins across medals
				x += bgW + SpacingX;
			}
		}

		private Rectangle DrawMedalIcon(Rectangle bgRect, Texture2D tex, float scale, float rotationRad)
		{
			if (tex == null) return Rectangle.Empty;
			// Compute base uniform scale to fit within IconSize
			float baseScale = 1f;
			if (tex.Width > 0 && tex.Height > 0)
			{
				float sx = IconSize / (float)tex.Width;
				float sy = IconSize / (float)tex.Height;
				baseScale = System.Math.Min(sx, sy);
			}
			float finalScale = baseScale * System.Math.Max(0.1f, scale);
			// Center of inner padded square
			float centerX = bgRect.X + BgPadding + IconSize / 2f;
			float centerY = bgRect.Y + BgPadding + IconSize / 2f;
			var origin = new Vector2(tex.Width / 2f, tex.Height / 2f);
			// Compute drawn bounds for spacing and tooltip
			int drawW = (int)System.Math.Round(tex.Width * finalScale);
			int drawH = (int)System.Math.Round(tex.Height * finalScale);
			int left = (int)System.Math.Round(centerX - drawW / 2f);
			int top = (int)System.Math.Round(centerY - drawH / 2f);
			_spriteBatch.Draw(tex, new Vector2(centerX, centerY), null, Color.White, rotationRad, origin, finalScale, SpriteEffects.None, 0f);
			return new Rectangle(left, top, drawW, drawH);
		}

		private void UpdateTooltipForMedal(EquippedMedal medal, Rectangle rect)
		{
			var ui = medal.Owner.GetComponent<UIElement>();
			if (ui == null)
			{
				ui = new UIElement { IsInteractable = true };
				EntityManager.AddComponent(medal.Owner, ui);
			}
			ui.Bounds = rect;
			ui.IsInteractable = true;
			ui.TooltipPosition = TooltipPosition.Below; // show tooltip below and centered when possible
			ui.Tooltip = BuildMedalTooltip(medal);
		}

		private string BuildMedalTooltip(EquippedMedal medal)
		{
			if (string.IsNullOrWhiteSpace(medal.MedalId)) return string.Empty;
			if (MedalDefinitionCache.TryGet(medal.MedalId, out var def) && def != null)
			{
				string name = string.IsNullOrWhiteSpace(def.name) ? medal.MedalId : def.name;
				string txt = def.text ?? string.Empty;
				return string.IsNullOrWhiteSpace(txt) ? name : (name + "\n\n" + txt);
			}
			return medal.MedalId;
		}


    [DebugAction("Animation Test")]
    public void debug_animation() 
    {
      EventManager.Publish(new MedalTriggered { MedalEntity = EntityManager.GetEntity("Medal_StLuke"), MedalId = "st_luke" });
    }
  }
}


