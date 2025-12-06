using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Rendering
{
    public class BloodshotOverlay
    {
        private readonly Effect _effect;

        public bool IsAvailable => _effect != null;

        public float Time { get; set; }

        public BloodshotOverlay(Effect effect)
        {
            _effect = effect;
        }

        public void Begin(SpriteBatch spriteBatch)
        {
            if (_effect == null) return;

            _effect.CurrentTechnique = _effect.Techniques["SpriteDrawing"];

            Viewport vp = spriteBatch.GraphicsDevice.Viewport;
            Matrix projection = Matrix.CreateOrthographicOffCenter(0, vp.Width, vp.Height, 0, 0, 1);

            _effect.Parameters["MatrixTransform"]?.SetValue(projection);
            _effect.Parameters["ViewportSize"]?.SetValue(new Vector2(vp.Width, vp.Height));
            _effect.Parameters["Time"]?.SetValue(Time);

            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.Opaque,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                RasterizerState.CullNone,
                _effect
            );
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D source)
        {
            if (_effect == null || source == null) return;
            Rectangle bounds = spriteBatch.GraphicsDevice.Viewport.Bounds;
            spriteBatch.Draw(source, bounds, Color.White);
        }

        public void End(SpriteBatch spriteBatch)
        {
            if (_effect == null) return;
            spriteBatch.End();
        }
    }
}
