using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Factories;
using Crusaders30XX.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
    [DebugTab("Card Debug Scene")]
    public class CardDebugSceneSystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly string _requestedCardId;

        private List<Entity> _cardEntities = new List<Entity>();
        private string _resolvedCardId;
        private int _frameCount;
        private Texture2D _pixel;

        public bool ReadyToCapture { get; private set; }
        public string ResolvedCardId => _resolvedCardId;

        // Layout constants
        private const int CardWidth = 268;
        private const int CardHeight = 377;
        private const int CardGap = 40;
        private const int VirtualWidth = 1920;
        private const int VirtualHeight = 1080;

        private static readonly Color BackgroundColor = new Color(144, 238, 144); // light green

        public CardDebugSceneSystem(EntityManager em, GraphicsDevice gd, SpriteBatch sb, string requestedCardId)
            : base(em)
        {
            _graphicsDevice = gd;
            _spriteBatch = sb;
            _requestedCardId = requestedCardId;

            _pixel = new Texture2D(gd, 1, 1);
            _pixel.SetData(new[] { Color.White });

            EventManager.Subscribe<LoadSceneEvent>(e =>
            {
                if (e.Scene != SceneId.CardDebug) return;
                SetupCards();
            });

            EventManager.Subscribe<DeleteCachesEvent>(_ =>
            {
                DestroyCards();
            });
        }

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            yield break;
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
        }

        public void Draw()
        {
            // Draw light green background
            _spriteBatch.Draw(_pixel, new Rectangle(0, 0, VirtualWidth, VirtualHeight), BackgroundColor);

            // Render each card via the event system
            int totalWidth = CardWidth * 3 + CardGap * 2;
            int startX = (VirtualWidth - totalWidth) / 2;
            int startY = (VirtualHeight - CardHeight) / 2;

            for (int i = 0; i < _cardEntities.Count; i++)
            {
                var card = _cardEntities[i];
                var pos = new Vector2(startX + i * (CardWidth + CardGap), startY);
                EventManager.Publish(new CardRenderScaledEvent
                {
                    Card = card,
                    Position = pos,
                    Scale = 1f
                });
            }

            _frameCount++;
            if (_frameCount >= 2)
            {
                ReadyToCapture = true;
            }
        }

        private void SetupCards()
        {
            DestroyCards();

            // Resolve card ID
            if (!string.IsNullOrEmpty(_requestedCardId))
            {
                _resolvedCardId = _requestedCardId;
            }
            else
            {
                var allCards = CardFactory.GetAllCards();
                var nonWeapons = allCards.Values.Where(c => !c.IsWeapon).ToList();
                _resolvedCardId = nonWeapons[Random.Shared.Next(nonWeapons.Count)].CardId;
            }

            Console.WriteLine($"[CardDebug] Rendering card: {_resolvedCardId}");

            // Create 3 cards: White, Red, Black
            var colors = new[] { CardData.CardColor.White, CardData.CardColor.Red, CardData.CardColor.Black };
            foreach (var color in colors)
            {
                var entity = EntityFactory.CreateCardFromDefinition(EntityManager, _resolvedCardId, color);
                if (entity != null)
                {
                    // Make non-interactive so no hover effects
                    var ui = entity.GetComponent<UIElement>();
                    if (ui != null) ui.IsInteractable = false;

                    _cardEntities.Add(entity);
                }
            }

            _frameCount = 0;
            ReadyToCapture = false;
        }

        private void DestroyCards()
        {
            foreach (var entity in _cardEntities)
            {
                EntityManager.DestroyEntity(entity.Id);
            }
            _cardEntities.Clear();
        }
    }
}
