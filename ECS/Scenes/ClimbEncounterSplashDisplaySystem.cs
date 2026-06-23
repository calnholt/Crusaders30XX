using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Services;
using Crusaders30XX.ECS.Singletons;
using Crusaders30XX.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Climb Splash")]
	public class ClimbEncounterSplashDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly ContentManager _content;
		private readonly Texture2D _pixel;
		private readonly SpriteFont _titleFont = FontSingleton.TitleFont;
		private readonly SpriteFont _bodyFont = FontSingleton.ChakraPetchFont;
		private readonly RasterizerState _scissorState = new RasterizerState { ScissorTestEnable = true };

		private string _enemyName = string.Empty;
		private string _enemyId = string.Empty;
		private Texture2D _enemyPortrait;
		private SplashLayout _layout;

		private enum Phase { Idle, FadeIn, GateOpen, Hold, FadeOut }
		private Phase _phase = Phase.Idle;
		private float _phaseTime;
		private float _totalTime;
		private bool _sceneLoadRequested;
		private bool _previewMode;

		// Timing
		[DebugEditable(DisplayName = "Fade In (s)", Step = 0.01f, Min = 0.05f, Max = 0.5f)]
		public float FadeInDurationSeconds { get; set; } = 0.14f;
		[DebugEditable(DisplayName = "Gate Open (s)", Step = 0.01f, Min = 0.5f, Max = 4f)]
		public float GateOpenDurationSeconds { get; set; } = 1.73f;
		[DebugEditable(DisplayName = "Hold (s)", Step = 0.01f, Min = 0f, Max = 1f)]
		public float HoldDurationSeconds { get; set; } = 0.14f;
		[DebugEditable(DisplayName = "Fade Out (s)", Step = 0.01f, Min = 0.1f, Max = 1f)]
		public float FadeOutDurationSeconds { get; set; } = 0.38f;

		// Gate
		[DebugEditable(DisplayName = "Gate Start Size", Step = 10, Min = 10, Max = 300)]
		public float GateStartSize { get; set; } = 60f;
		[DebugEditable(DisplayName = "Gate Frame Thickness", Step = 1, Min = 1, Max = 8)]
		public float GateFrameThickness { get; set; } = 4f;
		[DebugEditable(DisplayName = "Gate Frame Inset", Step = 0.01f, Min = 0f, Max = 0.25f)]
		public float GateFrameInset { get; set; } = 0.08f;

		// Slash
		[DebugEditable(DisplayName = "Slash Thickness", Step = 1, Min = 2, Max = 20)]
		public float SlashThickness { get; set; } = 8f;
		[DebugEditable(DisplayName = "Slash Delay", Step = 0.01f, Min = 0f, Max = 1f)]
		public float SlashDelay { get; set; } = 0.34f;
		[DebugEditable(DisplayName = "Slash Duration", Step = 0.01f, Min = 0.05f, Max = 0.5f)]
		public float SlashDuration { get; set; } = 0.19f;
		[DebugEditable(DisplayName = "Slash Glow Alpha", Step = 0.01f, Min = 0f, Max = 1f)]
		public float SlashGlowAlpha { get; set; } = 0.3f;

		// Portrait
		[DebugEditable(DisplayName = "Portrait Width", Step = 10, Min = 100, Max = 600)]
		public float PortraitWidth { get; set; } = 300f;
		[DebugEditable(DisplayName = "Portrait Height", Step = 10, Min = 100, Max = 600)]
		public float PortraitHeight { get; set; } = 380f;
		[DebugEditable(DisplayName = "Portrait Crop Top Bias", Step = 0.01f, Min = 0f, Max = 1f)]
		public float PortraitCropTopBias { get; set; } = 0.07f;
		[DebugEditable(DisplayName = "Portrait Bounce Overshoot", Step = 0.01f, Min = 1f, Max = 1.5f)]
		public float PortraitBounceOvershoot { get; set; } = 1.1f;
		[DebugEditable(DisplayName = "Portrait Bounce Settle", Step = 0.01f, Min = 0.8f, Max = 1.2f)]
		public float PortraitBounceSettle { get; set; } = 0.97f;
		[DebugEditable(DisplayName = "Portrait Shadow Alpha", Step = 0.01f, Min = 0f, Max = 1f)]
		public float PortraitShadowAlpha { get; set; } = 0.8f;

		// Name
		[DebugEditable(DisplayName = "Name Font Scale", Step = 0.01f, Min = 0.1f, Max = 1f)]
		public float NameFontScale { get; set; } = 0.44f;
		[DebugEditable(DisplayName = "Name Letter Gap", Step = 1, Min = 0, Max = 20)]
		public float NameLetterGap { get; set; } = 4f;
		[DebugEditable(DisplayName = "Name Letter Delay", Step = 0.001f, Min = 0f, Max = 0.1f)]
		public float NameLetterDelay { get; set; } = 0.026f;
		[DebugEditable(DisplayName = "Name Start Percent", Step = 0.01f, Min = 0f, Max = 0.8f)]
		public float NameStartPercent { get; set; } = 0.38f;
		[DebugEditable(DisplayName = "Name Drop Height", Step = 1, Min = 0, Max = 100)]
		public float NameDropHeight { get; set; } = 48f;
		[DebugEditable(DisplayName = "Name Gap To Portrait", Step = 1, Min = 0, Max = 80)]
		public float NameGapToPortrait { get; set; } = 20f;

		// Subtitle
		[DebugEditable(DisplayName = "Subtitle Font Scale", Step = 0.01f, Min = 0.03f, Max = 0.3f)]
		public float SubtitleFontScale { get; set; } = 0.109f;
		[DebugEditable(DisplayName = "Subtitle Gap To Name", Step = 1, Min = 0, Max = 60)]
		public float SubtitleGapToName { get; set; } = 20f;
		[DebugEditable]
		public string SubtitleText { get; set; } = "Prepare for battle";

		// Shade
		[DebugEditable(DisplayName = "Shade Alpha", Step = 0.01f, Min = 0f, Max = 1f)]
		public float ShadeAlpha { get; set; } = 0.92f;
		[DebugEditable(DisplayName = "Dim Scene Alpha", Step = 0.01f, Min = 0f, Max = 1f)]
		public float DimSceneAlpha { get; set; } = 0.22f;

		private float TotalDuration =>
			FadeInDurationSeconds + GateOpenDurationSeconds + HoldDurationSeconds + FadeOutDurationSeconds;

		private struct SplashLayout
		{
			public Rectangle Viewport;
			public Rectangle GateFrame;
			public Vector2 PortraitCenter;
			public Rectangle PortraitRect;
			public Vector2 NameCenter;
			public Vector2 SubtitleCenter;
			public float SlashAngle;
			public float SlashLength;
			public float[] LetterBaseX;
		}

		public ClimbEncounterSplashDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice,
			SpriteBatch spriteBatch, ContentManager content) : base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			_content = content;
			_pixel = new Texture2D(graphicsDevice, 1, 1);
			_pixel.SetData(new[] { Color.White });

			EventManager.Subscribe<ClimbEncounterSplashRequested>(OnSplashRequested);
			EventManager.Subscribe<BattleSceneInitializedEvent>(OnBattleSceneInitialized);
			EventManager.Subscribe<DeleteCachesEvent>(_ => _enemyPortrait = null);
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities() => Array.Empty<Entity>();

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		public override void Update(GameTime gameTime)
		{
			if (_phase == Phase.Idle) return;
			StateSingleton.IsActive = true;
			float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
			_phaseTime += dt;
			_totalTime += dt;
			switch (_phase)
			{
				case Phase.FadeIn:
					if (_phaseTime >= FadeInDurationSeconds) { _phase = Phase.GateOpen; _phaseTime = 0f; }
					break;
				case Phase.GateOpen:
					if (!_sceneLoadRequested && !_previewMode && GetShadeAlpha() >= ShadeAlpha * 0.99f)
						LoadTargetScene(SceneId.Battle);
					if (_phaseTime >= GateOpenDurationSeconds) { _phase = Phase.Hold; _phaseTime = 0f; }
					break;
				case Phase.Hold:
					if (_phaseTime >= HoldDurationSeconds) { _phase = Phase.FadeOut; _phaseTime = 0f; }
					break;
				case Phase.FadeOut:
					if (_phaseTime >= FadeOutDurationSeconds)
					{
						if (!_previewMode)
							EventManager.Publish(new TransitionCompleteEvent { Scene = SceneId.Battle });
						StateSingleton.IsActive = false;
						_phase = Phase.Idle;
						_phaseTime = 0f;
						_totalTime = 0f;
					}
					break;
			}
		}

		public void Draw()
		{
			if (_phase == Phase.Idle) return;
			int vw = Game1.VirtualWidth;
			int vh = Game1.VirtualHeight;

			DrawScrimDim();
			DrawGatePass(vw, vh);
			DrawSlash();
			DrawPortraitShadow();
			DrawEnemyPortrait(GetPortraitScale(out float portraitAlpha), portraitAlpha);
			DrawNameLetters();
			DrawSubtitle(vw);
			DrawShade(vw, vh);
		}

		private void DrawScrimDim()
		{
			if (_phase == Phase.FadeIn || _phase == Phase.GateOpen)
			{
				float scrimAlpha = _phase == Phase.FadeIn
					? MathHelper.Lerp(0f, DimSceneAlpha, MathHelper.Clamp(_phaseTime / Math.Max(0.0001f, FadeInDurationSeconds), 0f, 1f))
					: MathHelper.Lerp(DimSceneAlpha, 0f, MathHelper.Clamp(_phaseTime / Math.Max(0.0001f, GateOpenDurationSeconds * 0.1f), 0f, 1f));
				if (scrimAlpha > 0.001f)
					_spriteBatch.Draw(_pixel, new Rectangle(0, 0, Game1.VirtualWidth, Game1.VirtualHeight), Color.Black * scrimAlpha);
			}
		}

		private void DrawGatePass(int vw, int vh)
		{
			float gateProgress = GetGateProgress();
			if (gateProgress <= 0f) return;
			int gateW = (int)MathHelper.Lerp(GateStartSize, vw, gateProgress);
			int gateH = (int)MathHelper.Lerp(GateStartSize, vh, gateProgress);
			var gateClip = new Rectangle((vw - gateW) / 2, (vh - gateH) / 2, gateW, gateH);
			_graphicsDevice.ScissorRectangle = gateClip;

			_spriteBatch.End();
			_spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp,
				DepthStencilState.None, _scissorState);

			var gateBgColor = new Color(10, 8, 8) * 0.97f;
			_spriteBatch.Draw(_pixel, new Rectangle(0, 0, vw, vh), gateBgColor);

			if (_layout.GateFrame.Width > 0)
				ClimbSceneDrawHelpers.DrawBorder(_spriteBatch, _pixel, _layout.GateFrame,
					ClimbSceneDrawHelpers.Red3, (int)GateFrameThickness);

			_spriteBatch.End();
			_spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
		}

		private void DrawSlash()
		{
			float progress = GetSlashProgress();
			if (progress <= 0.001f) return;
			int vw = Game1.VirtualWidth;
			float len = _layout.SlashLength;
			float thick = SlashThickness;
			float angle = _layout.SlashAngle;
			var center = new Vector2(vw / 2f, Game1.VirtualHeight / 2f);
			var origin = new Vector2(len / 2f, thick / 2f);

			_spriteBatch.Draw(_pixel, center, null, ClimbSceneDrawHelpers.Red3 * progress, angle, origin,
				new Vector2(len, thick), SpriteEffects.None, 0f);

			float glowLen = len * 0.25f;
			float glowAlpha = SlashGlowAlpha * progress;
			if (glowAlpha > 0.001f)
			{
				var glowOrigin = new Vector2(glowLen / 2f, thick / 2f);
				_spriteBatch.Draw(_pixel, center - new Vector2(len * 0.3f, 0).Rotate(angle),
					null, Color.White * glowAlpha, angle, glowOrigin, new Vector2(glowLen, thick * 0.6f), SpriteEffects.None, 0f);
				_spriteBatch.Draw(_pixel, center + new Vector2(len * 0.3f, 0).Rotate(angle),
					null, Color.White * glowAlpha, angle, glowOrigin, new Vector2(glowLen, thick * 0.6f), SpriteEffects.None, 0f);
			}
		}

		private void DrawPortraitShadow()
		{
			float scale = GetPortraitScale(out float alpha);
			if (alpha < 0.001f) return;
			var rect = _layout.PortraitRect;
			int offsetY = 20;
			var shadowRect = new Rectangle(rect.X, rect.Y + offsetY, rect.Width, Math.Max(1, rect.Height - offsetY));
			float shadowScale = MathHelper.Lerp(0.2f, 1f, scale);
			int sw = (int)(shadowRect.Width * shadowScale);
			int sh = (int)(shadowRect.Height * shadowScale);
			var centered = new Rectangle(
				shadowRect.Center.X - sw / 2,
				shadowRect.Center.Y - sh / 2,
				sw, sh);
			_spriteBatch.Draw(_pixel, centered, Color.Black * PortraitShadowAlpha * alpha);
		}

		private void DrawEnemyPortrait(float scale, float alpha)
		{
			if (alpha < 0.001f || _enemyPortrait == null) return;
			var baseRect = _layout.PortraitRect;
			int sw = (int)(baseRect.Width * scale);
			int sh = (int)(baseRect.Height * scale);
			var dest = new Rectangle(baseRect.Center.X - sw / 2, baseRect.Center.Y - sh / 2, sw, sh);
			ClimbSceneDrawHelpers.DrawPortraitCropped(_spriteBatch, _enemyPortrait, dest, PortraitCropTopBias);
			var overlay = Color.White * alpha;
			if (alpha < 0.999f)
				_spriteBatch.Draw(_pixel, dest, Color.Black * (1f - alpha));
		}

		private void DrawNameLetters()
		{
			if (string.IsNullOrEmpty(_enemyName) || _titleFont == null) return;
			if (_totalTime < NameStartPercent * TotalDuration) return;
			var chars = _enemyName.ToCharArray();
			float letterAnimDuration = GateOpenDurationSeconds * 0.12f;
			float contentFade = GetContentFadeAlpha();
			float nameAlpha = contentFade;
			if (_phase == Phase.FadeOut) nameAlpha *= GetShadeAlpha() / Math.Max(0.001f, ShadeAlpha);

			for (int i = 0; i < chars.Length; i++)
			{
				float progress = GetLetterProgress(i);
				if (progress <= 0.001f) continue;
				float easedProgress = EaseOutBack(progress);
				float offsetY = (1f - easedProgress) * NameDropHeight;
				float rotation = (1f - easedProgress) * 6f * (MathF.PI / 180f);
				float letterAlpha = MathHelper.Clamp(progress, 0f, 1f) * nameAlpha;

				if (chars[i] == ' ')
				{
					float spaceWidth = NameLetterGap * 2f;
					if (i < _layout.LetterBaseX.Length - 1)
					{
						_layout.LetterBaseX[i + 1] += spaceWidth - (NameLetterGap);
					}
					continue;
				}

				string ch = chars[i].ToString();
				float chX = _layout.LetterBaseX[i];
				float chY = _layout.NameCenter.Y + offsetY;
				var pos = new Vector2(chX, chY);

				var glowColor = new Color(196, 30, 58) * 0.7f * letterAlpha * 0.25f;
				for (int gx = -2; gx <= 2; gx += 2)
				{
					for (int gy = -2; gy <= 2; gy += 2)
					{
						if (gx == 0 && gy == 0) continue;
						_spriteBatch.DrawString(_titleFont, ch, pos + new Vector2(gx, gy),
							glowColor, rotation, Vector2.Zero, NameFontScale, SpriteEffects.None, 0f);
					}
				}

				_spriteBatch.DrawString(_titleFont, ch, pos,
					ClimbSceneDrawHelpers.White1 * letterAlpha, rotation, Vector2.Zero,
					NameFontScale, SpriteEffects.None, 0f);
			}
		}

		private void DrawSubtitle(int vw)
		{
			float contentFade = GetContentFadeAlpha();
			float nameAlpha = contentFade;
			if (_phase == Phase.FadeOut) nameAlpha *= GetShadeAlpha() / Math.Max(0.001f, ShadeAlpha);
			if (nameAlpha < 0.001f) return;
			float subtitleAlpha = MathHelper.Clamp(
				(_totalTime - (NameStartPercent + 0.04f) * TotalDuration) / Math.Max(0.001f, GateOpenDurationSeconds * 0.08f),
				0f, 1f) * nameAlpha;
			if (subtitleAlpha < 0.001f) return;
			var text = ClimbSceneDrawHelpers.ToAscii(SubtitleText);
			var size = _bodyFont.MeasureString(text) * SubtitleFontScale;
			var pos = new Vector2(vw / 2f - size.X / 2f, _layout.SubtitleCenter.Y);
			_spriteBatch.DrawString(_bodyFont, text, pos,
				ClimbSceneDrawHelpers.White3 * subtitleAlpha, 0f, Vector2.Zero,
				SubtitleFontScale, SpriteEffects.None, 0f);
		}

		private void DrawShade(int vw, int vh)
		{
			float alpha = GetShadeAlpha();
			if (alpha < 0.001f) return;
			_spriteBatch.Draw(_pixel, new Rectangle(0, 0, vw, vh), Color.Black * alpha);
		}

		private float GetShadeAlpha()
		{
			return _phase switch
			{
				Phase.FadeIn => MathHelper.Lerp(0f, ShadeAlpha,
					MathHelper.Clamp(_phaseTime / Math.Max(0.0001f, FadeInDurationSeconds), 0f, 1f)),
				Phase.GateOpen => ShadeAlpha,
				Phase.Hold => ShadeAlpha,
				Phase.FadeOut => MathHelper.Lerp(ShadeAlpha, 0f,
					MathHelper.Clamp(_phaseTime / Math.Max(0.0001f, FadeOutDurationSeconds), 0f, 1f)),
				_ => 0f,
			};
		}

		private float GetGateProgress()
		{
			if (_phase == Phase.FadeIn) return 0f;
			if (_phase == Phase.Hold || _phase == Phase.FadeOut) return 1f;
			if (_phase == Phase.Idle) return 0f;
			float t = MathHelper.Clamp(_phaseTime / Math.Max(0.0001f, GateOpenDurationSeconds * 0.24f), 0f, 1f);
			return EaseOutCubic(t);
		}

		private float GetPortraitScale(out float alpha)
		{
			alpha = 0f;
			if (_phase == Phase.FadeIn) return 0.2f;
			if (_phase == Phase.Idle) return 0.2f;
			float gateFrac = _phase == Phase.GateOpen
				? MathHelper.Clamp(_phaseTime / GateOpenDurationSeconds, 0f, 1f)
				: 1f;
			float contentFade = GetContentFadeAlpha();

			if (_phase == Phase.FadeOut)
			{
				alpha = contentFade;
				return 1f;
			}

			float animFrac = MathHelper.Clamp((gateFrac - 0.14f) / (0.55f - 0.14f), 0f, 1f);
			float scale;
			if (animFrac <= 0f) scale = 0.2f;
			else if (animFrac < 0.4f) scale = MathHelper.Lerp(0.2f, PortraitBounceOvershoot, EaseOutBack(animFrac / 0.4f));
			else if (animFrac < 0.7f) scale = MathHelper.Lerp(PortraitBounceOvershoot, PortraitBounceSettle, (animFrac - 0.4f) / 0.3f);
			else scale = MathHelper.Lerp(PortraitBounceSettle, 1f, (animFrac - 0.7f) / 0.3f);

			alpha = contentFade;
			return scale;
		}

		private float GetSlashProgress()
		{
			if (_phase == Phase.FadeIn || _phase == Phase.Idle) return 0f;
			float slashStart = SlashDelay;
			float slashEnd = slashStart + SlashDuration;
			float total = TotalDuration;
			float t = (_totalTime - slashStart) / Math.Max(0.0001f, SlashDuration);
			if (_totalTime < slashStart || t <= 0f) return 0f;
			if (_totalTime > slashEnd || t >= 1f) return 0f;
			if (t < 0.5f) return t * 2f;
			return 2f - t * 2f;
		}

		private float GetLetterProgress(int index)
		{
			float letterStart = NameStartPercent * TotalDuration + index * NameLetterDelay;
			float letterEnd = letterStart + GateOpenDurationSeconds * 0.1f;
			if (_totalTime < letterStart) return 0f;
			float t = (_totalTime - letterStart) / Math.Max(0.0001f, letterEnd - letterStart);
			if (t > 1f) t = 1f;
			return t;
		}

		private float GetContentFadeAlpha()
		{
			if (_phase == Phase.FadeIn || _phase == Phase.Idle) return 0f;
			if (_phase == Phase.FadeOut) return 0f;
			if (_phase == Phase.Hold) return 0f;
			float gateFrac = MathHelper.Clamp(_phaseTime / GateOpenDurationSeconds, 0f, 1f);
			if (gateFrac < 0.6f) return 1f;
			if (gateFrac > 0.9f) return 0f;
			return 1f - (gateFrac - 0.6f) / 0.3f;
		}

		private void LoadTargetScene(SceneId nextScene)
		{
			var sceneEntity = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault();
			var previous = sceneEntity?.GetComponent<SceneState>()?.Current ?? SceneId.None;
			EventManager.Publish(new DeleteCachesEvent { Scene = nextScene });
			DeleteEntities(nextScene);
			EventManager.Publish(new LoadSceneEvent { Scene = nextScene, PreviousScene = previous });
			_sceneLoadRequested = true;
		}

		private void DeleteEntities(SceneId nextScene)
		{
			var sceneEntity = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault();
			var scene = sceneEntity.GetComponent<SceneState>();
			var previous = scene.Current;
			scene.Current = nextScene;
			var isReload = previous == nextScene;
			var toDestroy = EntityManager.GetAllEntities()
				.Where(e =>
					e.HasComponent<OwnedByScene>() &&
					e.GetComponent<OwnedByScene>().Scene == previous &&
					!isReload &&
					!e.HasComponent<DontDestroyOnLoad>())
				.ToList();
			foreach (var e in toDestroy)
				EntityManager.DestroyEntity(e.Id);
		}

		private SplashLayout ComputeLayout(int vw, int vh)
		{
			var layout = new SplashLayout
			{
				Viewport = new Rectangle(0, 0, vw, vh),
			};

			int insetX = (int)(vw * GateFrameInset);
			int insetY = (int)(vh * GateFrameInset);
			layout.GateFrame = new Rectangle(insetX, insetY, vw - insetX * 2, vh - insetY * 2);

			float portraitCenterY = vh * 0.45f;
			layout.PortraitCenter = new Vector2(vw / 2f, portraitCenterY);
			layout.PortraitRect = new Rectangle(
				(int)(layout.PortraitCenter.X - PortraitWidth / 2f),
				(int)(layout.PortraitCenter.Y - PortraitHeight / 2f),
				(int)PortraitWidth, (int)PortraitHeight);

			var chars = _enemyName.ToCharArray();
			float nameY = layout.PortraitRect.Bottom + NameGapToPortrait;
			ComputeLetterPositions(chars, vw, nameY, ref layout);
			var nameSize = MeasureName(chars);
			layout.NameCenter = new Vector2(vw / 2f - nameSize.X / 2f, nameY);

			var subtitleSize = _bodyFont.MeasureString(ClimbSceneDrawHelpers.ToAscii(SubtitleText)) * SubtitleFontScale;
			layout.SubtitleCenter = new Vector2(vw / 2f, nameY + nameSize.Y + SubtitleGapToName);

			layout.SlashAngle = MathF.Atan2(-vh, vw);
			layout.SlashLength = vw / MathF.Cos(MathF.Abs(layout.SlashAngle));

			return layout;
		}

		private void ComputeLetterPositions(char[] chars, int vw, float nameY, ref SplashLayout layout)
		{
			layout.LetterBaseX = new float[chars.Length];
			float cursor = vw / 2f;
			for (int i = 0; i < chars.Length; i++)
			{
				if (chars[i] == ' ')
				{
					layout.LetterBaseX[i] = cursor;
					cursor += NameLetterGap * 3f;
				}
				else
				{
					layout.LetterBaseX[i] = cursor;
					var chSize = _titleFont.MeasureString(chars[i].ToString()) * NameFontScale;
					cursor += chSize.X + NameLetterGap;
				}
			}
			float totalW = cursor - NameLetterGap - vw / 2f;
			float offset = totalW / 2f;
			for (int i = 0; i < chars.Length; i++)
				layout.LetterBaseX[i] -= offset;
		}

		private Vector2 MeasureName(char[] chars)
		{
			float total = 0f;
			float maxH = 0f;
			for (int i = 0; i < chars.Length; i++)
			{
				if (chars[i] == ' ') { total += NameLetterGap * 3f; continue; }
				var chSize = _titleFont.MeasureString(chars[i].ToString()) * NameFontScale;
				total += chSize.X + NameLetterGap;
				if (chSize.Y > maxH) maxH = chSize.Y;
			}
			return new Vector2(total, maxH);
		}

		private void OnSplashRequested(ClimbEncounterSplashRequested evt)
		{
			_enemyName = evt.EnemyName ?? evt.EnemyId ?? string.Empty;
			_enemyId = evt.EnemyId ?? string.Empty;
			_enemyPortrait = null;
			_sceneLoadRequested = false;
			_previewMode = false;
			string assetName = EnemyPortraitContent.ToAssetName(_enemyId);
			if (!string.IsNullOrEmpty(assetName))
			{
				try { _enemyPortrait = _content.Load<Texture2D>(assetName); }
				catch { _enemyPortrait = null; }
			}
			var vw = Game1.VirtualWidth;
			var vh = Game1.VirtualHeight;
			_layout = ComputeLayout(vw, vh);
			_phase = Phase.FadeIn;
			_phaseTime = 0f;
			_totalTime = 0f;
			StateSingleton.IsActive = true;
		}

		private void OnBattleSceneInitialized(BattleSceneInitializedEvent evt)
		{
			if (evt.Scene == SceneId.Battle)
				_sceneLoadRequested = true;
		}

		[DebugAction("Preview Gate Break")]
		private void Debug_PreviewSplash()
		{
			_previewMode = true;
			_enemyName = "Fire Skeleton";
			_enemyId = "fire_skeleton";
			_sceneLoadRequested = false;
			string assetName = EnemyPortraitContent.ToAssetName("fire_skeleton");
			try { _enemyPortrait = _content.Load<Texture2D>(assetName); }
			catch { _enemyPortrait = null; }
			var vw = Game1.VirtualWidth;
			var vh = Game1.VirtualHeight;
			_layout = ComputeLayout(vw, vh);
			_phase = Phase.FadeIn;
			_phaseTime = 0f;
			_totalTime = 0f;
			StateSingleton.IsActive = true;
		}

		private static float EaseOutBack(float t)
		{
			float c1 = 1.70158f;
			float c3 = c1 + 1f;
			return 1f + c3 * MathF.Pow(t - 1f, 3f) + c1 * MathF.Pow(t - 1f, 2f);
		}

		private static float EaseOutCubic(float t) => 1f - MathF.Pow(1f - t, 3f);

		private static float EaseInOutQuad(float t) =>
			t < 0.5f ? 2f * t * t : 1f - MathF.Pow(-2f * t + 2f, 2f) / 2f;
	}
}

internal static class Vector2Extensions
{
	public static Vector2 Rotate(this Vector2 v, float radians)
	{
		float cos = MathF.Cos(radians);
		float sin = MathF.Sin(radians);
		return new Vector2(v.X * cos - v.Y * sin, v.X * sin + v.Y * cos);
	}
}
