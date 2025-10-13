using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Events;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using Crusaders30XX.ECS.Data.Cards;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Assigned Block Display")]
	public class AssignedBlockCardsDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly ContentManager _content;
		private readonly SpriteFont _font;
		private readonly Texture2D _pixel;
		private readonly System.Collections.Generic.Dictionary<(int w, int h, int r), Texture2D> _roundedRectCache = new();
		private readonly List<Entity> _pendingReturn = new();

		[DebugEditable(DisplayName = "Anchor Offset X", Step = 2, Min = -1000, Max = 1000)]
		public int AnchorOffsetX { get; set; } = 0;
		[DebugEditable(DisplayName = "Anchor Offset Y", Step = 2, Min = -1000, Max = 1000)]
		public int AnchorOffsetY { get; set; } = -210;
		[DebugEditable(DisplayName = "Slot Spacing X", Step = 2, Min = 10, Max = 200)]
		public int SlotSpacingX { get; set; } = 56;
		[DebugEditable(DisplayName = "Card Draw W", Step = 2, Min = 20, Max = 300)]
		public int CardDrawWidth { get; set; } = 80;
		[DebugEditable(DisplayName = "Card Draw H", Step = 2, Min = 20, Max = 400)]
		public int CardDrawHeight { get; set; } = 110;
		[DebugEditable(DisplayName = "Target Scale", Step = 0.02f, Min = 0.1f, Max = 1.0f)]
		public float TargetScale { get; set; } = 0.43f;
		[DebugEditable(DisplayName = "Pullback Seconds", Step = 0.01f, Min = 0f, Max = 1f)]
		public float PullbackSeconds { get; set; } = 0.0f;
		[DebugEditable(DisplayName = "Launch Seconds", Step = 0.01f, Min = 0f, Max = 1f)]
		public float LaunchSeconds { get; set; } = 0.18f;
		[DebugEditable(DisplayName = "Impact Seconds", Step = 0.01f, Min = 0f, Max = 1f)]
		public float ImpactSeconds { get; set; } = 0.00f;
		[DebugEditable(DisplayName = "Return Seconds", Step = 0.01f, Min = 0f, Max = 1f)]
		public float ReturnSeconds { get; set; } = 0.18f;

		[DebugEditable(DisplayName = "Above Gap (px)", Step = 1, Min = 0, Max = 100)]
		public int AboveGap { get; set; } = 8;
		[DebugEditable(DisplayName = "Block Text Scale", Step = 0.05f, Min = 0.2f, Max = 2.0f)]
		public float BlockTextScale { get; set; } = 0.2f;
		[DebugEditable(DisplayName = "Assigned Corner Radius", Step = 1, Min = 0, Max = 64)]
		public int AssignedCornerRadius { get; set; } = 6;
		[DebugEditable(DisplayName = "Assigned Background Alpha", Step = 1, Min = 0, Max = 255)]
		public int AssignedBackgroundAlpha { get; set; } = 225;

		[DebugEditable(DisplayName = "Equip Icon Height", Step = 1, Min = 8, Max = 128)]
		public int EquipIconHeight { get; set; } = 99;
		[DebugEditable(DisplayName = "Equip Icon Gap", Step = 1, Min = 0, Max = 64)]
		public int EquipIconGap { get; set; } = 6;
		[DebugEditable(DisplayName = "Equip Icon Offset X", Step = 1, Min = -200, Max = 200)]
		public int EquipIconOffsetX { get; set; } = 0;
		[DebugEditable(DisplayName = "Equip Icon Offset Y", Step = 1, Min = -200, Max = 200)]
		public int EquipIconOffsetY { get; set; } = 10;

		public AssignedBlockCardsDisplaySystem(EntityManager em, GraphicsDevice gd, SpriteBatch sb, SpriteFont font, ContentManager content) : base(em)
		{
			_graphicsDevice = gd;
			_spriteBatch = sb;
			_font = font;
			_content = content;
			_pixel = new Texture2D(gd, 1, 1);
			_pixel.SetData(new[] { Color.White });
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<AssignedBlockCard>();
		}

		public override void Update(GameTime gameTime)
		{
			// If we're processing the enemy attack, this system should not accept input or retarget cards
			var phaseStateEntity = EntityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault();
			if (phaseStateEntity == null) return;
			var phase = phaseStateEntity.GetComponent<PhaseState>();
			bool isProcessing = phase.Sub == SubPhase.EnemyAttack;
			if (isProcessing)
			{
				base.Update(gameTime);
				return;
			}
			// Ensure all assigned block cards have UIElement so clicks can be detected
			var ensureList = EntityManager.GetEntitiesWithComponent<AssignedBlockCard>();
			foreach (var e in ensureList)
			{
				if (e.GetComponent<UIElement>() == null)
				{
					EntityManager.AddComponent(e, new UIElement { IsInteractable = true });
				}
			}

			base.Update(gameTime);
			var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
			// Process any returns after the main iteration to avoid collection-modified errors
			if (_pendingReturn.Count > 0)
			{
				for (int i = 0; i < _pendingReturn.Count; i++)
				{
					var card = _pendingReturn[i];
					bool isEquipment = card.GetComponent<EquippedEquipment>() != null;
					if (isEquipment)
					{
						// Clear assignment and return equipment to default zone
						EntityManager.RemoveComponent<AssignedBlockCard>(card);
						var zone = card.GetComponent<EquipmentZone>();
						if (zone == null) { zone = new EquipmentZone(); EntityManager.AddComponent(card, zone); }
						zone.Zone = EquipmentZoneType.Default;
						var ui = card.GetComponent<UIElement>();
						if (ui != null) { ui.IsInteractable = true; ui.IsHovered = false; ui.Tooltip = string.Empty; }
					}
					else
					{
						EventManager.Publish(new CardMoveRequested
						{
							Card = card,
							Deck = deckEntity,
							Destination = CardZoneType.Hand,
							Reason = "ReturnAfterAssignment"
						});
					}
				}
				_pendingReturn.Clear();
			}
			// Handle click against all assigned cards (topmost first)
			var abcEntities = EntityManager.GetEntitiesWithComponent<AssignedBlockCard>();
			var clickedCard = abcEntities.FirstOrDefault(e => e.GetComponent<UIElement>()?.IsClicked == true);
			if (clickedCard != null)
			{
				var abc = clickedCard.GetComponent<AssignedBlockCard>();
				abc.Phase = AssignedBlockCard.PhaseState.Returning;
				abc.Elapsed = 0f;
				var cardData = clickedCard.GetComponent<CardData>();
				EventManager.Publish(new BlockAssignmentRemoved
				{
					Card = clickedCard,
					DeltaBlock = -abc.BlockAmount,
					Color = abc.ColorKey
				});
			}
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			var abc = entity.GetComponent<AssignedBlockCard>();
			var t = entity.GetComponent<Transform>();
			if (abc == null) return;
			// Click to return to hand when idle on the banner
			var ui = entity.GetComponent<UIElement>();
			if (ui == null)
			{
				ui = new UIElement { IsInteractable = true };
				EntityManager.AddComponent(entity, ui);
			}
			bool click = ui.IsClicked;
			if (click && (abc.Phase == AssignedBlockCard.PhaseState.Idle || abc.Phase == AssignedBlockCard.PhaseState.Impact))
			{
				abc.Phase = AssignedBlockCard.PhaseState.Returning;
				abc.Elapsed = 0f;
				// Publish unassign so counters and damage update even if the top-level click handler didn't catch this card
				var enemy2 = EntityManager.GetEntitiesWithComponent<AttackIntent>().FirstOrDefault();
				var pa2 = enemy2?.GetComponent<AttackIntent>()?.Planned?.FirstOrDefault();
				var cd = entity.GetComponent<CardData>();
				if (pa2 != null)
				{
					EventManager.Publish(new BlockAssignmentRemoved
					{
						ContextId = pa2.ContextId,
						Card = entity,
						DeltaBlock = -abc.BlockAmount,
						Color = cd?.Color.ToString()
					});
				}
			}

			// Ensure bounds reflect where the card is currently drawn and control interactivity
			{
				var enemyTip = EntityManager.GetEntitiesWithComponent<AttackIntent>().FirstOrDefault();
				var paTip = enemyTip?.GetComponent<AttackIntent>()?.Planned?.FirstOrDefault();
				bool isForCurrentContext = paTip != null && abc.ContextId == paTip.ContextId;
				bool showDuringPhase = abc.Phase == AssignedBlockCard.PhaseState.Idle || abc.Phase == AssignedBlockCard.PhaseState.Impact || abc.Phase == AssignedBlockCard.PhaseState.Launch || abc.Phase == AssignedBlockCard.PhaseState.Pullback;
				bool shouldShowTooltip = isForCurrentContext && showDuringPhase;
				int cw = (int)(CardDrawWidth * abc.CurrentScale);
				int ch = (int)(CardDrawHeight * abc.CurrentScale);
				var rectNow = new Rectangle((int)(abc.CurrentPos.X - cw / 2f), (int)(abc.CurrentPos.Y - ch / 2f), cw, ch);

				// Always keep bounds in sync for cards in the current context; disable otherwise
				if (isForCurrentContext)
				{
					ui.Bounds = rectNow;
					ui.IsInteractable = (abc.Phase == AssignedBlockCard.PhaseState.Idle || abc.Phase == AssignedBlockCard.PhaseState.Impact) && !TransitionStateSingleton.IsActive;
				}
				else
				{
					ui.IsInteractable = false;
				}

				// Tooltip content management
				if (shouldShowTooltip)
				{
					ui.Tooltip = abc.Tooltip ?? string.Empty;
					ui.TooltipOffsetPx = 10;
					ui.TooltipPosition = TooltipPosition.Above;
				}
				else
				{
					ui.IsHovered = false;
					string tip = string.Empty;
					var cdReset = ui.Owner?.GetComponent<CardData>();
					if (cdReset != null && CardDefinitionCache.TryGet(cdReset.CardId, out var defReset) && defReset != null)
					{
						tip = defReset.tooltip ?? string.Empty;
					}
					ui.Tooltip = tip;
					ui.TooltipOffsetPx = 30;
				}
			}
			float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
			abc.Elapsed += dt;

			// Determine current context's index among assigned cards for layout
			var enemy = EntityManager.GetEntitiesWithComponent<AttackIntent>().FirstOrDefault();
			var pa = enemy?.GetComponent<AttackIntent>()?.Planned?.FirstOrDefault();
			if (pa == null) return;
			// Only animate if for this context; otherwise keep as-is
			bool forCurrent = abc.ContextId == pa.ContextId;
			int indexInContext = 0;
			int countInContext = 1;
			if (forCurrent)
			{
				var list = GetRelevantEntities()
					.Where(e => e.GetComponent<AssignedBlockCard>()?.ContextId == pa.ContextId)
					.OrderBy(e => e.GetComponent<AssignedBlockCard>().AssignedAtTicks) // oldest to newest left->right
					.ToList();
				indexInContext = list.FindIndex(e => e == entity);
				countInContext = list.Count;
			}

			// Compute slot target from banner anchor if available, else viewport center
			var anchorT = EntityManager.GetEntitiesWithComponent<EnemyAttackBannerAnchor>().FirstOrDefault()?.GetComponent<Transform>();
			Vector2 basePoint;
			if (anchorT != null)
			{
				basePoint = anchorT.Position;
			}
			else
			{
				basePoint = new Vector2(_graphicsDevice.Viewport.Width * 0.5f, _graphicsDevice.Viewport.Height * 0.5f);
			}
			var center = new Vector2(basePoint.X + AnchorOffsetX, basePoint.Y + AnchorOffsetY);
			float offsetIndex = indexInContext - (countInContext - 1) * 0.5f;
			// Place centers so that cards sit above the banner baseline
			float targetHalfH = CardDrawHeight * TargetScale * 0.5f;
			float slotY = center.Y - targetHalfH - AboveGap;
			var slotTarget = new Vector2(center.X + offsetIndex * SlotSpacingX, slotY);
			abc.TargetPos = slotTarget;

			switch (abc.Phase)
			{
				case AssignedBlockCard.PhaseState.Pullback:
				{
					float p = PullbackSeconds <= 0f ? 1f : MathHelper.Clamp(abc.Elapsed / PullbackSeconds, 0f, 1f);
					var back = abc.StartPos + new Vector2(-30, -20);
					abc.CurrentPos = Vector2.Lerp(abc.StartPos, back, p);
					abc.CurrentScale = MathHelper.Lerp(abc.StartScale, TargetScale * 0.8f, p);
					if (p >= 1f) { abc.Phase = AssignedBlockCard.PhaseState.Launch; abc.Elapsed = 0f; }
					break;
				}
				case AssignedBlockCard.PhaseState.Launch:
				{
					float p = LaunchSeconds <= 0f ? 1f : MathHelper.Clamp(abc.Elapsed / LaunchSeconds, 0f, 1f);
					float ease = 1f - (float)System.Math.Pow(1f - p, 3);
					abc.CurrentPos = Vector2.Lerp(abc.CurrentPos, abc.TargetPos, ease);
					abc.CurrentScale = MathHelper.Lerp(abc.CurrentScale, TargetScale, ease);
					if (p >= 1f) { abc.Phase = AssignedBlockCard.PhaseState.Impact; abc.Elapsed = 0f; }
					break;
				}
				case AssignedBlockCard.PhaseState.Impact:
				{
					float p = ImpactSeconds <= 0f ? 1f : MathHelper.Clamp(abc.Elapsed / ImpactSeconds, 0f, 1f);
					abc.CurrentPos = abc.TargetPos + new Vector2(0, (1f - p) * 6f);
					abc.CurrentScale = TargetScale * (1f + 0.08f * (1f - p));
					if (p >= 1f) { abc.Phase = AssignedBlockCard.PhaseState.Idle; abc.Elapsed = 0f; }
					break;
				}
				case AssignedBlockCard.PhaseState.Idle:
				{
					float slide = 1f - (float)System.Math.Exp(-10f * dt);
					abc.CurrentPos = Vector2.Lerp(abc.CurrentPos, abc.TargetPos, slide);
					abc.CurrentScale = MathHelper.Lerp(abc.CurrentScale, TargetScale, slide);
					break;
				}
				case AssignedBlockCard.PhaseState.Returning:
				{
					float p = ReturnSeconds <= 0f ? 1f : MathHelper.Clamp(abc.Elapsed / ReturnSeconds, 0f, 1f);
					float ease = 1f - (float)System.Math.Pow(1f - p, 3);
					Vector2 target = abc.ReturnTargetPos;
					abc.CurrentPos = Vector2.Lerp(abc.CurrentPos, target, ease);
					abc.CurrentScale = MathHelper.Lerp(abc.CurrentScale, 1f, ease);
					if (p >= 1f)
					{
						_pendingReturn.Add(entity);
					}
					break;
				}
			}

			// Reflect animation to Transform so any other draws match position/scale if needed
			if (t == null)
			{
				t = new Transform();
				EntityManager.AddComponent(entity, t);
			}
			t.Position = abc.CurrentPos;
			t.Scale = new Vector2(abc.CurrentScale, abc.CurrentScale);
		}

		public void Draw()
		{
			// Draw assigned cards for the current context
			var enemy = EntityManager.GetEntitiesWithComponent<AttackIntent>().FirstOrDefault();
			var pa = enemy?.GetComponent<AttackIntent>()?.Planned?.FirstOrDefault();
			if (pa == null) return;
			var list = GetRelevantEntities().Where(e => e.GetComponent<AssignedBlockCard>()?.ContextId == pa.ContextId).ToList();
			if (list.Count == 0) return;
			// Draw newest on top: reverse order for render
			for (int i = list.Count - 1; i >= 0; i--)
			{
				var card = list[i];
				var abc = card.GetComponent<AssignedBlockCard>();
				if (abc == null) continue;
				var pos = abc.CurrentPos;
				int cw = (int)(CardDrawWidth * abc.CurrentScale);
				int ch = (int)(CardDrawHeight * abc.CurrentScale);
				var rect = new Rectangle((int)(pos.X - cw / 2f), (int)(pos.Y - ch / 2f), cw, ch);
				// Colors now come directly from AssignedBlockCard
				Color bg = abc.DisplayBgColor;
				Color fg = abc.DisplayFgColor;
				bg = new Color(bg.R, bg.G, bg.B, (byte)System.Math.Clamp(AssignedBackgroundAlpha, 0, 255));
				int radius = System.Math.Max(0, AssignedCornerRadius);
				var rounded = GetRoundedRectTexture(rect.Width, rect.Height, radius);
				var center = new Vector2(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f);
				_spriteBatch.Draw(rounded, center, null, bg, 0f, new Vector2(rounded.Width / 2f, rounded.Height / 2f), Vector2.One, SpriteEffects.None, 0f);
				if (_font != null)
				{
					string text = $"{abc.BlockAmount}";
					float textScale = MathHelper.Clamp(BlockTextScale, 0.2f, 2.0f);
					var textSize = _font.MeasureString(text) * textScale;
					float tx = rect.X + (rect.Width - textSize.X) * 0.5f;
					float ty = rect.Y + (rect.Height - textSize.Y) * 0.5f;
					_spriteBatch.DrawString(_font, text, new Vector2(tx, ty), fg, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
					// If this assignment is equipment and idle (not moving), draw the equipment type icon centered above the rect
					if (abc.IsEquipment && abc.Phase == AssignedBlockCard.PhaseState.Idle && !string.IsNullOrWhiteSpace(abc.EquipmentType))
					{
						var tex = SafeLoadIcon(abc.EquipmentType);
						if (tex != null)
						{
							float iconH = System.Math.Max(8, EquipIconHeight) * abc.CurrentScale;
							float iconW = tex.Height > 0 ? iconH * (tex.Width / (float)tex.Height) : iconH;
							float gap = System.Math.Max(0, EquipIconGap);
							float iconX = rect.X + rect.Width * 0.5f - iconW * 0.5f + EquipIconOffsetX;
							float iconY = rect.Y - gap - iconH + EquipIconOffsetY;
							_spriteBatch.Draw(tex, new Rectangle((int)iconX, (int)iconY, (int)iconW, (int)iconH), Color.White);
						}
					}
				}
			}
		}

		private Texture2D GetRoundedRectTexture(int width, int height, int radius)
		{
			var key = (width, height, radius);
			if (_roundedRectCache.TryGetValue(key, out var tex)) return tex;
			var texture = Crusaders30XX.ECS.Rendering.RoundedRectTextureFactory.CreateRoundedRect(_graphicsDevice, width, height, radius);
			_roundedRectCache[key] = texture;
			return texture;
		}

		private Texture2D SafeLoadIcon(string type)
		{
			string key = (type ?? string.Empty).Trim().ToLowerInvariant();
			string assetName = key; // expects head.png, chest.png, arms.png, legs.png
			try { return _content.Load<Texture2D>(assetName); } catch { return null; }
		}
	}
}


