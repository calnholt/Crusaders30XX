using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Rendering
{
    /// <summary>
    /// Factory for generating rounded-rectangle textures.
    /// No internal caching; callers can cache per-size as needed.
    /// </summary>
    public static class RoundedRectTextureFactory
    {
        public static Texture2D CreateRoundedRect(GraphicsDevice graphicsDevice, int width, int height, int radius)
        {
            var texture = new Texture2D(graphicsDevice, width, height);
            Color[] data = new Color[width * height];
            int r = System.Math.Max(0, radius);
            int r2 = r * r;
            int w = width;
            int h = height;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool inside = true;
                    if (x < r && y < r)
                    {
                        int dx = r - x - 1;
                        int dy = r - y - 1;
                        inside = (dx * dx + dy * dy) <= r2;
                    }
                    else if (x >= w - r && y < r)
                    {
                        int dx = x - (w - r);
                        int dy = r - y - 1;
                        inside = (dx * dx + dy * dy) <= r2;
                    }
                    else if (x < r && y >= h - r)
                    {
                        int dx = r - x - 1;
                        int dy = y - (h - r);
                        inside = (dx * dx + dy * dy) <= r2;
                    }
                    else if (x >= w - r && y >= h - r)
                    {
                        int dx = x - (w - r);
                        int dy = y - (h - r);
                        inside = (dx * dx + dy * dy) <= r2;
                    }

                    data[y * w + x] = inside ? Color.White : Color.Transparent;
                }
            }

            texture.SetData(data);
            return texture;
        }
    }
}

