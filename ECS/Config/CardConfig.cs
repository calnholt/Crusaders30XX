using System;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Config
{
    /// <summary>
    /// Centralized configuration for card dimensions and positioning
    /// </summary>
    public static class CardConfig
    {
        // Global UI scale derived from viewport height (relative to 1080p)
        private const float BASELINE_HEIGHT = 1080f;
        public static float UIScale { get; private set; } = 1.0f;

        public static void SetScaleFromViewportHeight(int viewportHeight)
        {
            // Clamp to avoid degenerate sizes on very small windows
            float scale = Math.Max(0.4f, Math.Min(1.0f, viewportHeight / BASELINE_HEIGHT));
            UIScale = scale;
        }

        // Base (reference) dimensions at 1080p
        private const int BASE_CARD_WIDTH = 250;
        private const int BASE_CARD_HEIGHT = 350;
        private const int BASE_CARD_OFFSET_Y_EXTRA = 25;
        private const int BASE_HIGHLIGHT_BORDER_THICKNESS = 5;
        private const int BASE_CARD_GAP = -20;
        // Hand layout defaults moved to HandDisplaySystem with debug controls
        private const int BASE_CARD_BORDER_THICKNESS = 3;
        private const int BASE_CARD_CORNER_RADIUS = 18;
        // Draw pile config (kept)
        private const int BASE_TEXT_MARGIN_X = 16;
        private const int BASE_TEXT_MARGIN_Y = 16;
        private const float BASE_NAME_SCALE = 0.7f;
        private const float BASE_COST_SCALE = 0.6f;
        private const float BASE_DESCRIPTION_SCALE = 0.4f;
        private const float BASE_BLOCK_SCALE = 0.5f;
        private const float BASE_BLOCK_NUMBER_SCALE = 0.9f;
        private const int BASE_BLOCK_NUMBER_MARGIN_X = 14;
        private const int BASE_BLOCK_NUMBER_MARGIN_Y = 12;

        // Scaled properties
        public static int CARD_WIDTH => (int)Math.Round(BASE_CARD_WIDTH * UIScale);
        public static int CARD_HEIGHT => (int)Math.Round(BASE_CARD_HEIGHT * UIScale);
        
        // Card positioning offsets (relative to transform position)
        public static int CARD_OFFSET_X => CARD_WIDTH / 2;
        public static int CARD_OFFSET_Y => (CARD_HEIGHT / 2) + (int)Math.Round(BASE_CARD_OFFSET_Y_EXTRA * UIScale);
        
        // Highlight border dimensions (slightly larger than card)
        public static int HIGHLIGHT_BORDER_THICKNESS => (int)Math.Max(1, Math.Round(BASE_HIGHLIGHT_BORDER_THICKNESS * UIScale));
        public static int HIGHLIGHT_HEIGHT => CARD_HEIGHT + (HIGHLIGHT_BORDER_THICKNESS * 2);
        public static int HIGHLIGHT_OFFSET_X => CARD_OFFSET_X + HIGHLIGHT_BORDER_THICKNESS;
        public static int HIGHLIGHT_OFFSET_Y => CARD_OFFSET_Y + HIGHLIGHT_BORDER_THICKNESS;
        // Highlight glow settings
        
        // Hand layout settings
        // Distance between adjacent card centers = CARD_WIDTH + CARD_GAP
        public static int CARD_GAP => (int)Math.Round(BASE_CARD_GAP * UIScale);
        public static float CARD_SPACING => CARD_WIDTH + CARD_GAP;
        // Hand layout settings relocated to HandDisplaySystem

        public static float DegToRad(float deg) => (float)(Math.PI / 180.0) * deg;

        // Tweening (movement smoothing)
        // Higher = snappier interpolation toward target per second
        // Tween speed relocated to HandDisplaySystem
        
        // Card border thickness for visual
        public static int CARD_BORDER_THICKNESS => (int)Math.Max(1, Math.Round(BASE_CARD_BORDER_THICKNESS * UIScale));
        public static int CARD_CORNER_RADIUS => (int)Math.Max(2, Math.Round(BASE_CARD_CORNER_RADIUS * UIScale));


        // Text layout settings
        public static int TEXT_MARGIN_X => (int)Math.Round(BASE_TEXT_MARGIN_X * UIScale);
        public static int TEXT_MARGIN_Y => (int)Math.Round(BASE_TEXT_MARGIN_Y * UIScale);

        // Text scales
        public static float NAME_SCALE => BASE_NAME_SCALE * UIScale;
        public static float COST_SCALE => BASE_COST_SCALE * UIScale;
        public static float DESCRIPTION_SCALE => BASE_DESCRIPTION_SCALE * UIScale;
        public static float BLOCK_SCALE => BASE_BLOCK_SCALE * UIScale;

        // Block number bottom-right settings (margins kept; text layout moved to systems)
        public static float BLOCK_NUMBER_SCALE => BASE_BLOCK_NUMBER_SCALE * UIScale;
        public static int BLOCK_NUMBER_MARGIN_X => (int)Math.Round(BASE_BLOCK_NUMBER_MARGIN_X * UIScale);
        public static int BLOCK_NUMBER_MARGIN_Y => (int)Math.Round(BASE_BLOCK_NUMBER_MARGIN_Y * UIScale);
        
        // All per-card layout rects now computed by CardDisplaySystem
    }
}