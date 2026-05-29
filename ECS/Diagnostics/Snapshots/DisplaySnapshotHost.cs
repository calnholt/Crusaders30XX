using System;
using System.IO;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.Diagnostics.Snapshots
{
    public sealed class DisplaySnapshotHost
    {
        private readonly Microsoft.Xna.Framework.Game _game;
        private readonly DisplaySnapshotLaunchOptions _options;
        private readonly GraphicsDevice _graphicsDevice;
        private readonly ContentManager _content;

        private IDisplaySnapshotFixture _fixture;
        private DisplaySnapshotContext _ctx;
        private int _frameCount;

        public bool IsActive => _fixture != null;
        public bool ShouldSkipGlobalOverlays => IsActive;

        private DisplaySnapshotHost(
            Microsoft.Xna.Framework.Game game,
            DisplaySnapshotLaunchOptions options,
            GraphicsDevice graphicsDevice,
            ContentManager content)
        {
            _game = game;
            _options = options;
            _graphicsDevice = graphicsDevice;
            _content = content;
        }

        public static DisplaySnapshotHost TryCreate(
            DisplaySnapshotLaunchOptions options,
            Microsoft.Xna.Framework.Game game,
            GraphicsDevice graphicsDevice,
            ContentManager content)
        {
            if (options == null) return null;
            return new DisplaySnapshotHost(game, options, graphicsDevice, content);
        }

        public void OnGameReady(World world, Entity sceneEntity, SpriteBatch spriteBatch)
        {
            if (!DisplaySnapshotRegistry.TryGet(_options.FixtureId, out _fixture))
            {
                Console.Error.WriteLine($"[DisplaySnapshot] Unknown fixture: '{_options.FixtureId}'");
                Environment.Exit(1);
            }

            _ctx = new DisplaySnapshotContext
            {
                World = world,
                GraphicsDevice = _graphicsDevice,
                SpriteBatch = spriteBatch,
                Content = _content,
                SceneEntity = sceneEntity
            };

            var sceneState = sceneEntity.GetComponent<SceneState>();
            sceneState.Current = SceneId.Snapshot;

            try
            {
                _fixture.Setup(_ctx, _options.Args);
            }
            catch (DisplaySnapshotSetupException ex)
            {
                Console.Error.WriteLine($"[DisplaySnapshot] {ex.Message}");
                Environment.Exit(1);
            }

            _frameCount = 0;
            Console.WriteLine($"[DisplaySnapshot] Fixture '{_fixture.Id}' ready");
        }

        public void DrawScene(SpriteBatch spriteBatch)
        {
            _ctx = new DisplaySnapshotContext
            {
                World = _ctx.World,
                GraphicsDevice = _ctx.GraphicsDevice,
                SpriteBatch = spriteBatch,
                Content = _ctx.Content,
                SceneEntity = _ctx.SceneEntity
            };
            _fixture.Draw(_ctx);
        }

        /// <returns>True if capture ran and the game is exiting (caller should skip backbuffer present).</returns>
        public bool TickAfterDraw(RenderTarget2D sceneRt)
        {
            _frameCount++;
            if (_frameCount < _fixture.WarmupFrames) return false;

            var dir = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..", "..", "..",
                "debug", "snapshots", _fixture.Id);
            Directory.CreateDirectory(dir);

            var fileName = _fixture.OutputFileName;
            if (!fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                fileName += ".png";
            }

            var filePath = Path.Combine(dir, fileName);
            using (var stream = File.Create(filePath))
            {
                sceneRt.SaveAsPng(stream, sceneRt.Width, sceneRt.Height);
            }

            Console.WriteLine($"[DisplaySnapshot] Saved: {Path.GetFullPath(filePath)}");
            _graphicsDevice.SetRenderTarget(null);
            _game.Exit();
            return true;
        }
    }
}
