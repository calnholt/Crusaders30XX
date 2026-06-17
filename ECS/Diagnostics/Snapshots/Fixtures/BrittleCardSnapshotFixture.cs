using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Factories;
using Crusaders30XX.ECS.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.Diagnostics.Snapshots.Fixtures;

public sealed class BrittleCardSnapshotFixture : IDisplaySnapshotFixture
{
    public string Id => "brittle-card";
    public int WarmupFrames => 2;
    public string OutputFileName => _resolvedCardId ?? DefaultCardId;

    private const string DefaultCardId = "strike";
    private Entity _cardEntity;
    private Texture2D _pixel;
    private string _resolvedCardId = DefaultCardId;

    public void Setup(DisplaySnapshotContext ctx, string[] args)
    {

        string requestedId = args.Length > 0 ? args[0] : DefaultCardId;
        if (CardFactory.Create(requestedId) == null)
        {
            throw new DisplaySnapshotSetupException($"Unknown card id: '{requestedId}'");
        }

        _resolvedCardId = requestedId;
        DestroyCard(ctx);

        _cardEntity = EntityFactory.CreateCardFromDefinition(ctx.World.EntityManager, _resolvedCardId, CardData.CardColor.White);
        if (_cardEntity == null)
        {
            throw new DisplaySnapshotSetupException($"Failed to create card entity: '{_resolvedCardId}'");
        }

        var ui = _cardEntity.GetComponent<UIElement>();
        if (ui != null) ui.IsInteractable = false;

        ctx.World.EntityManager.AddComponent(_cardEntity, new Brittle());

        _pixel = new Texture2D(ctx.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        System.Console.WriteLine($"[DisplaySnapshot] Rendering brittle card: {_resolvedCardId}");
    }

    public void Draw(DisplaySnapshotContext ctx)
    {
        DrawBackdrop(ctx);

        EventManager.Publish(new CardRenderScaledEvent
        {
            Card = _cardEntity,
            Position = new Vector2(Game1.VirtualWidth / 2f, Game1.VirtualHeight / 2f + 40f),
            Scale = 1f
        });
    }

    private void DrawBackdrop(DisplaySnapshotContext ctx)
    {
        int vw = Game1.VirtualWidth;
        int vh = Game1.VirtualHeight;
        ctx.SpriteBatch.Draw(_pixel, new Rectangle(0, 0, vw, vh), new Color(28, 34, 48));

        const int stripeWidth = 80;
        for (int x = -vw; x < vw * 2; x += stripeWidth)
        {
            var stripe = new Rectangle(x, 0, stripeWidth / 2, vh);
            ctx.SpriteBatch.Draw(_pixel, stripe, new Color(58, 112, 146));
        }

        var cardBackdrop = new Rectangle(vw / 2 - 230, vh / 2 - 260, 460, 560);
        ctx.SpriteBatch.Draw(_pixel, cardBackdrop, new Color(176, 64, 88) * 0.35f);
    }

    private void DestroyCard(DisplaySnapshotContext ctx)
    {
        if (_cardEntity == null) return;

        ctx.World.EntityManager.DestroyEntity(_cardEntity.Id);
        _cardEntity = null;
    }
}
