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
using System;
using Crusaders30XX.ECS.Singletons;
using Crusaders30XX.ECS.Factories;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Assigned Block Display")]
	public class AssignedBlockCardsDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly ContentManager _content;
		private readonly SpriteFont _font = FontSingleton.ContentFont;
		private readonly Texture2D _pixel;
		private readonly System.Collections.Generic.Dictionary<(int w, int h, int r), Texture2D> _roundedRectCache = new();
		private readonly List<Entity> _pendingReturn = new();

		[DebugEditable(DisplayName = "Anchor Offset X", Step = 2, Min = -1000, Max = 1000)]
		public int AnchorOffsetX { get; set; } = 0;
		[DebugEditable(DisplayName = "Anchor Offset Y", Step = 2, Min = -1000, Max = 1000)]
		public int AnchorOffsetY { get; set; } = -75;
		[DebugEditable(DisplayName = "Slot Spacing X", Step = 2, Min = 10, Max = 200)]
		public int SlotSpacingX { get; set; } = 70;
		[DebugEditable(DisplayName = "Card Draw W", Step = 2, Min = 20, Max = 300)]
		public int CardDrawWidth { get; set; } = 100;
		[DebugEditable(DisplayName = "Card Draw H", Step = 2, Min = 20, Max = 400)]
		public int CardDrawHeight { get; set; } = 130;
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

		public AssignedBlockCardsDisplaySystem(EntityManager em, GraphicsDevice gd, SpriteBatch sb, ContentManager content) : base(em)
		{
			_graphicsDevice = gd;
			_spriteBatch = sb;
			_content = content;
			_pixel = new Texture2D(gd, 1, 1);
			_pixel.SetData(new[] { Color.White });
			EventManager.Subscribe<UnassignCardAsBlockRequested>(OnUnassignCardAsBlockRequested);
			EventManager.Subscribe<BlockAssignmentAdded>(OnBlockAssignmentAdded);
			EventManager.Subscribe<BlockAssignmentRemoved>(OnBlockAssignmentRemoved);
			EventManager.Subscribe<CardMoved>(OnCardMoved);
		}

		private void OnUnassignCardAsBlockRequested(UnassignCardAsBlockRequested evt)
		{
			Console.WriteLine($"[AssignedBlockCardsDisplaySystem] OnUnassignCardAsBlockRequested: {evt.CardEntity.Id}");
			var abc = evt.CardEntity.GetComponent<AssignedBlockCard>();
			// Immediately move B HotKey to the previous assigned (if available)
			var hk = evt.CardEntity.GetComponent<HotKey>();
			if (hk != null && hk.Button == FaceButton.B)
			{
				EntityManager.RemoveComponent<HotKey>(evt.CardEntity);
				AssignHotKeyToPrevious(evt.CardEntity, abc?.ContextId, abc?.AssignedAtTicks ?? long.MaxValue);
			}
			abc.Phase = AssignedBlockCard.PhaseState.Returning;
			abc.Elapsed = 0f;
			var cardData = evt.CardEntity.GetComponent<CardData>();
			EventManager.Publish(new BlockAssignmentRemoved
			{
				Card = evt.CardEntity,
				DeltaBlock = -abc.BlockAmount,
				Color = abc.ColorKey,
				ContextId = abc.ContextId
			});
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

			// Self-heal: ensure B hotkey is on the newest assigned card for the current context
			{
				var enemyTip = EntityManager.GetEntitiesWithComponent<AttackIntent>().FirstOrDefault();
				var paTip = enemyTip?.GetComponent<AttackIntent>()?.Planned?.FirstOrDefault();
				if (paTip != null && !string.IsNullOrEmpty(paTip.ContextId))
				{
					MaintainLatestHotKeyForContext(paTip.ContextId);
				}
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
					ui.IsInteractable = (abc.Phase == AssignedBlockCard.PhaseState.Idle || abc.Phase == AssignedBlockCard.PhaseState.Impact) && !StateSingleton.IsActive;
				}
				else
				{
					ui.IsInteractable = false;
				}

				// Tooltip content management
				// For non-equipment cards, ensure card tooltip override while assigned for the current context
				bool isCardAssignment = !abc.IsEquipment;
				if (isCardAssignment && isForCurrentContext && showDuringPhase)
				{
					// Capture original tooltip config once
					var backup = entity.GetComponent<TooltipOverrideBackup>();
					if (backup == null)
					{
						var ctExisting = entity.GetComponent<CardTooltip>();
						backup = new TooltipOverrideBackup
						{
							OriginalType = ui?.TooltipType ?? TooltipType.Text,
							OriginalPosition = ui?.TooltipPosition ?? TooltipPosition.Above,
							OriginalOffsetPx = ui?.TooltipOffsetPx ?? 30,
							HadCardTooltip = (ctExisting != null),
							OriginalCardTooltipId = ctExisting?.CardId ?? string.Empty
						};
						EntityManager.AddComponent(entity, backup);
					}
					// Apply override: show full card tooltip below
					var cd = entity.GetComponent<CardData>();
					if (ui != null)
					{
						ui.TooltipType = TooltipType.Card;
						ui.TooltipPosition = TooltipPosition.Below;
						ui.TooltipOffsetPx = 10;
					}
					var ct = entity.GetComponent<CardTooltip>();
					if (ct == null)
					{
						EntityManager.AddComponent(entity, new CardTooltip { CardId = cd?.Card.CardId ?? string.Empty });
					}
					else
					{
						ct.CardId = cd?.Card.CardId ?? string.Empty;
					}
				}

				if (shouldShowTooltip)
				{
					// Equipment still uses text tooltip above
					if (abc.IsEquipment)
					{
						ui.Tooltip = abc.Tooltip ?? string.Empty;
						ui.TooltipOffsetPx = 10;
						ui.TooltipPosition = TooltipPosition.Above;
					}
					// Cards: no text tooltip; card tooltip override already applied above
				}
				else
				{
					ui.IsHovered = false;
					// For equipment, maintain/reset the text tooltip when not showing
					if (abc.IsEquipment)
					{
						string tip = string.Empty;
						var cdReset = ui.Owner?.GetComponent<CardData>();
						var card = CardFactory.Create(cdReset?.Card.CardId ?? string.Empty);
						if (card != null)
						{
							tip = card.Tooltip ?? string.Empty;
						}
						ui.Tooltip = tip;
						ui.TooltipOffsetPx = 30;
					}
					// Cards: do not write text tooltip while assigned
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
			var anchorEntity = EntityManager.GetEntitiesWithComponent<EnemyAttackBannerAnchor>().FirstOrDefault();
			if (anchorEntity == null)
			{
				anchorEntity = EntityManager.CreateEntity("EnemyAttackBannerAnchor");
				EntityManager.AddComponent(anchorEntity, new EnemyAttackBannerAnchor());
				EntityManager.AddComponent(anchorEntity, new Transform());
				EntityManager.AddComponent(anchorEntity, ParallaxLayer.GetUIParallaxLayer());
				EntityManager.AddComponent(anchorEntity, new UIElement { Bounds = new Rectangle(0, 0, 1, 1), IsInteractable = false });
			}
			var anchorT = anchorEntity.GetComponent<Transform>();
			var anchorUi = anchorEntity.GetComponent<UIElement>();
			Vector2 basePoint;
			bool uiBoundsValid = anchorUi != null && anchorUi.Bounds.Width >= 16 && anchorUi.Bounds.Height >= 16;
			if (uiBoundsValid)
			{
				// Align above the banner panel: use its top-center as the anchor baseline
				basePoint = new Vector2(anchorUi.Bounds.Center.X, anchorUi.Bounds.Top);
			}
			else
			{
				// Fallback to the anchor transform (parallax-adjusted) or viewport center
				basePoint = anchorT?.Position ?? new Vector2(Game1.VirtualWidth * 0.5f, Game1.VirtualHeight * 0.5f);
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
					if (p >= 1f) { abc.Phase = AssignedBlockCard.PhaseState.Idle; abc.Elapsed = 0f; MaintainLatestHotKeyForContext(pa.ContextId); }
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

		private void OnBlockAssignmentAdded(BlockAssignmentAdded evt)
		{
			if (evt == null || string.IsNullOrEmpty(evt.ContextId)) return;
			// Immediately remove the previous HotKey (if exists) for this context
			RemovePreviousForContext(evt.ContextId, evt.Card);
		}

		private void OnBlockAssignmentRemoved(BlockAssignmentRemoved evt)
		{
			if (evt == null || string.IsNullOrEmpty(evt.ContextId)) return;
			// If removal pertains to equipment, ensure its hotkey is removed immediately
			if (evt.Card != null && evt.Card.GetComponent<EquippedEquipment>() != null)
			{
				var hk = evt.Card.GetComponent<HotKey>();
				if (hk != null) { EntityManager.RemoveComponent<HotKey>(evt.Card); }
			}
			// Restore tooltip settings for cards (ignore equipment)
			if (evt.Card != null && evt.Card.GetComponent<EquippedEquipment>() == null)
			{
				var backup = evt.Card.GetComponent<TooltipOverrideBackup>();
				var ui = evt.Card.GetComponent<UIElement>();
				if (backup != null && ui != null)
				{
					ui.TooltipType = backup.OriginalType;
					ui.TooltipPosition = backup.OriginalPosition;
					ui.TooltipOffsetPx = backup.OriginalOffsetPx;
					var ct = evt.Card.GetComponent<CardTooltip>();
					if (backup.HadCardTooltip)
					{
						if (ct == null) { EntityManager.AddComponent(evt.Card, new CardTooltip { CardId = backup.OriginalCardTooltipId ?? string.Empty }); }
						else { ct.CardId = backup.OriginalCardTooltipId ?? string.Empty; }
					}
					else
					{
						if (ct != null) { EntityManager.RemoveComponent<CardTooltip>(evt.Card); }
					}
					EntityManager.RemoveComponent<TooltipOverrideBackup>(evt.Card);
				}
			}
			MaintainLatestHotKeyForContext(evt.ContextId);
		}

		private void OnCardMoved(CardMoved evt)
		{
			if (evt == null) return;
			if (evt.From == CardZoneType.AssignedBlock)
			{
				var hk = evt.Card?.GetComponent<HotKey>();
				if (hk != null && hk.Button == FaceButton.B)
				{
					// Remove B and reassign immediately to previous (if available)
					EntityManager.RemoveComponent<HotKey>(evt.Card);
					AssignHotKeyToPrevious(evt.Card, evt.ContextId, evt.Card.GetComponent<AssignedBlockCard>()?.AssignedAtTicks ?? long.MaxValue);
				}
				if (!string.IsNullOrEmpty(evt.ContextId))
				{
					MaintainLatestHotKeyForContext(evt.ContextId);
				}
			}
		}

		private void AssignHotKeyToPrevious(Entity removed, string contextId, long removedAssignedAt)
		{
			if (string.IsNullOrEmpty(contextId)) return;
			var prev = EntityManager.GetEntitiesWithComponent<AssignedBlockCard>()
				.Where(e => {
					if (e == removed) return false;
					var a = e.GetComponent<AssignedBlockCard>();
					return a != null && a.ContextId == contextId && a.Phase == AssignedBlockCard.PhaseState.Idle && a.AssignedAtTicks <= removedAssignedAt;
				})
				.OrderByDescending(e => e.GetComponent<AssignedBlockCard>().AssignedAtTicks)
				.FirstOrDefault();
			if (prev == null) return;
			var hk = prev.GetComponent<HotKey>();
			if (hk == null) EntityManager.AddComponent(prev, new HotKey { Button = FaceButton.B });
			else hk.Button = FaceButton.B;
		}

		private void MaintainLatestHotKeyForContext(string contextId)
		{
			if (string.IsNullOrEmpty(contextId)) return;

			// Clean up: remove B hotkeys from entities that are no longer assigned block items
			foreach (var e in EntityManager.GetEntitiesWithComponent<HotKey>().ToList())
			{
				var hk = e.GetComponent<HotKey>();
				if (hk == null || hk.Button != FaceButton.B) continue;
				var abc = e.GetComponent<AssignedBlockCard>();
				if (abc == null || abc.ContextId != contextId || abc.Phase != AssignedBlockCard.PhaseState.Idle)
				{
					var ui = e.GetComponent<UIElement>();
					if (ui != null && ui.EventType == UIElementEventType.UnassignCardAsBlock)
					{
						EntityManager.RemoveComponent<HotKey>(e);
					}
				}
			}

			// Apply B only to the newest Idle assignment for this context
			var candidates = EntityManager.GetEntitiesWithComponent<AssignedBlockCard>()
				.Where(ent => {
					var a = ent.GetComponent<AssignedBlockCard>();
					return a != null && a.ContextId == contextId && a.Phase == AssignedBlockCard.PhaseState.Idle;
				})
				.OrderBy(ent => ent.GetComponent<AssignedBlockCard>().AssignedAtTicks)
				.ToList();
			var newest = candidates.LastOrDefault();
			foreach (var ent in candidates)
			{
				var hk = ent.GetComponent<HotKey>();
				if (ent == newest)
				{
					if (hk == null)
					{
						EntityManager.AddComponent(ent, new HotKey { Button = FaceButton.B });
					}
					else
					{
						hk.Button = FaceButton.B;
					}
				}
				else
				{
					if (hk != null && hk.Button == FaceButton.B)
					{
						EntityManager.RemoveComponent<HotKey>(ent);
					}
				}
			}
		}

		private void RemovePreviousForContext(string contextId, Entity exclude)
		{
			// Find the previous (most recent) idle assignment other than the new one and remove its HotKey
			var prev = EntityManager.GetEntitiesWithComponent<AssignedBlockCard>()
				.Where(e => {
					if (e == exclude) return false;
					var a = e.GetComponent<AssignedBlockCard>();
					return a != null && a.ContextId == contextId && a.Phase == AssignedBlockCard.PhaseState.Idle;
				})
				.OrderByDescending(e => e.GetComponent<AssignedBlockCard>().AssignedAtTicks)
				.FirstOrDefault();
			if (prev == null) return;
			var hkPrev = prev.GetComponent<HotKey>();
			if (hkPrev != null && hkPrev.Button == FaceButton.B)
			{
				EntityManager.RemoveComponent<HotKey>(prev);
			}
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
			var texture = Rendering.RoundedRectTextureFactory.CreateRoundedRect(_graphicsDevice, width, height, radius);
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


