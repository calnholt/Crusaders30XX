using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Factories;
using Crusaders30XX.ECS.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.Diagnostics.Snapshots.Fixtures
{
    public sealed class QuestRewardModalSnapshotFixture : IDisplaySnapshotFixture
    {
        public string Id => "quest-reward-modal";
        public int WarmupFrames => 2;

        private QuestRewardModalDisplaySystem _modal;
        private QuestRewardSnapshotVariant _variant;
        private Texture2D _pixel;

        public string OutputFileName => _variant?.FileSlug ?? "quest-reward-modal";

        private static readonly Color BackdropColor = new(40, 44, 48);

        public void Setup(DisplaySnapshotContext ctx, string[] args)
        {
            CardDisplayToggle.UseV2 = true;
            _variant = QuestRewardSnapshotVariant.Parse(args);

            if (_variant.HasCardReward)
            {
                var parts = _variant.RewardCardKey.Split('|');
                var color = QuestRewardSnapshotVariant.ParseColor(parts[1]);
                var probe = EntityFactory.CreateCardFromDefinition(ctx.World.EntityManager, parts[0], color);
                if (probe == null)
                {
                    throw new DisplaySnapshotSetupException(
                        $"Failed to create reward card: '{_variant.RewardCardKey}'");
                }
                ctx.World.EntityManager.DestroyEntity(probe.Id);
            }

            _modal = new QuestRewardModalDisplaySystem(
                ctx.World.EntityManager,
                ctx.GraphicsDevice,
                ctx.SpriteBatch);
            ctx.World.AddSystem(_modal);

            _modal.Open(
                message: null,
                rewardGold: _variant.RewardGold,
                hasCardReward: _variant.HasCardReward,
                rewardCardKey: _variant.RewardCardKey);

            _pixel = new Texture2D(ctx.GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
        }

        public void Draw(DisplaySnapshotContext ctx)
        {
            int vw = Game1.VirtualWidth;
            int vh = Game1.VirtualHeight;
            ctx.SpriteBatch.Draw(_pixel, new Rectangle(0, 0, vw, vh), BackdropColor);
            _modal.Draw();
        }
    }
}
