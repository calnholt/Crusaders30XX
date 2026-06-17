using System;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Services
{
    public static class CardGeometryService
    {
        public static CardGeometrySettings GetSettings(EntityManager entityManager)
        {
            return entityManager
                .GetEntitiesWithComponent<CardGeometrySettings>()
                .FirstOrDefault()
                ?.GetComponent<CardGeometrySettings>();
        }

        public static Rectangle GetVisualRect(EntityManager entityManager, Vector2 position, float scale = 1f)
        {
            return GetVisualRect(GetSettings(entityManager), position, scale);
        }

        public static Rectangle GetVisualRect(CardGeometrySettings settings, Vector2 position, float scale = 1f)
        {
            int width = settings?.CardWidth ?? CardGeometrySettings.DefaultWidth;
            int height = settings?.CardHeight ?? CardGeometrySettings.DefaultHeight;
            int offsetYExtra = settings?.CardOffsetYExtra ?? CardGeometrySettings.DefaultOffsetYExtra;

            int scaledWidth = (int)Math.Round(width * scale);
            int scaledHeight = (int)Math.Round(height * scale);
            int scaledOffsetY = (int)Math.Round(offsetYExtra * scale);

            return new Rectangle(
                (int)position.X - scaledWidth / 2,
                (int)position.Y - (scaledHeight / 2 + scaledOffsetY),
                scaledWidth,
                scaledHeight);
        }

        public static Vector2 GetVisualCenter(EntityManager entityManager, Vector2 position, float scale = 1f)
        {
            return GetVisualCenter(GetSettings(entityManager), position, scale);
        }

        public static Vector2 GetVisualCenter(CardGeometrySettings settings, Vector2 position, float scale = 1f)
        {
            var rect = GetVisualRect(settings, position, scale);
            return new Vector2(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f);
        }
    }
}
