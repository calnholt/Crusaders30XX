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
        private const float BASE_HAND_BOTTOM_MARGIN = 150f;
        private const float BASE_HAND_FAN_MAX_ANGLE_DEG = 5f;
        private const float BASE_HAND_FAN_RADIUS = 0f;
        private const float BASE_HAND_FAN_CURVE_OFFSET = 0f;
        private const float BASE_HAND_HOVER_LIFT = 10f;
        private const float BASE_HAND_HOVER_SCALE = 1.0f;
        private const int BASE_HAND_Z_BASE = 100;
        private const int BASE_HAND_Z_STEP = 1;
        private const int BASE_HAND_Z_HOVER_BOOST = 1000;
        private const int BASE_CARD_BORDER_THICKNESS = 3;
        private const int BASE_CARD_CORNER_RADIUS = 18;
        private const int BASE_DRAW_PILE_WIDTH = 60;
        private const int BASE_DRAW_PILE_HEIGHT = 80;
        private const int BASE_DRAW_PILE_MARGIN = 20;
        private const float BASE_DRAW_PILE_TEXT_SCALE = 0.8f;
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
        public static int HIGHLIGHT_WIDTH => CARD_WIDTH + (HIGHLIGHT_BORDER_THICKNESS * 2);
        public static int HIGHLIGHT_HEIGHT => CARD_HEIGHT + (HIGHLIGHT_BORDER_THICKNESS * 2);
        public static int HIGHLIGHT_OFFSET_X => CARD_OFFSET_X + HIGHLIGHT_BORDER_THICKNESS;
        public static int HIGHLIGHT_OFFSET_Y => CARD_OFFSET_Y + HIGHLIGHT_BORDER_THICKNESS;
        // Highlight glow settings
        public const int HIGHLIGHT_GLOW_LAYERS = 10;          // number of glow shells
        public const float HIGHLIGHT_GLOW_SPREAD = 0.01f;    // expansion per layer (relative scale)
        public const float HIGHLIGHT_MAX_ALPHA = 0.6f;       // maximum alpha cap per glow layer
        public static readonly Color HIGHLIGHT_GLOW_COLOR = Color.Gold; // base glow color
        
        // Hand layout settings
        // Distance between adjacent card centers = CARD_WIDTH + CARD_GAP
        public static int CARD_GAP => (int)Math.Round(BASE_CARD_GAP * UIScale);
        public static float CARD_SPACING => CARD_WIDTH + CARD_GAP;
        public static float HAND_BOTTOM_MARGIN => BASE_HAND_BOTTOM_MARGIN * UIScale; // pixels from bottom of screen

        // Hand fan (splay) settings
        public static float HAND_FAN_MAX_ANGLE_DEG => BASE_HAND_FAN_MAX_ANGLE_DEG;   // max tilt angle at edges (not scaled)
        public static float HAND_FAN_RADIUS => BASE_HAND_FAN_RADIUS * UIScale;         // controls vertical arc curvature
        public static float HAND_FAN_CURVE_OFFSET => BASE_HAND_FAN_CURVE_OFFSET * UIScale;     // additional vertical offset
        public static float HAND_HOVER_LIFT => BASE_HAND_HOVER_LIFT * UIScale;          // pixels to lift hovered card
        public static float HAND_HOVER_SCALE => BASE_HAND_HOVER_SCALE;        // reserved for future scale-up
        public static int HAND_Z_BASE => BASE_HAND_Z_BASE;
        public static int HAND_Z_STEP => BASE_HAND_Z_STEP;
        public static int HAND_Z_HOVER_BOOST => BASE_HAND_Z_HOVER_BOOST;

        public static float DegToRad(float deg) => (float)(Math.PI / 180.0) * deg;

        // Tweening (movement smoothing)
        // Higher = snappier interpolation toward target per second
        public const float HAND_TWEEN_SPEED = 12f;
        
        // Card border thickness for visual
        public static int CARD_BORDER_THICKNESS => (int)Math.Max(1, Math.Round(BASE_CARD_BORDER_THICKNESS * UIScale));
        public static int CARD_CORNER_RADIUS => (int)Math.Max(2, Math.Round(BASE_CARD_CORNER_RADIUS * UIScale));

        // Draw pile display settings
        public static int DRAW_PILE_WIDTH => (int)Math.Round(BASE_DRAW_PILE_WIDTH * UIScale);
        public static int DRAW_PILE_HEIGHT => (int)Math.Round(BASE_DRAW_PILE_HEIGHT * UIScale);
        public static int DRAW_PILE_MARGIN => (int)Math.Round(BASE_DRAW_PILE_MARGIN * UIScale);
        public static float DRAW_PILE_TEXT_SCALE => BASE_DRAW_PILE_TEXT_SCALE * UIScale;

        // Text layout settings
        public static int TEXT_MARGIN_X => (int)Math.Round(BASE_TEXT_MARGIN_X * UIScale);
        public static int TEXT_MARGIN_Y => (int)Math.Round(BASE_TEXT_MARGIN_Y * UIScale);

        // Text scales
        public static float NAME_SCALE => BASE_NAME_SCALE * UIScale;
        public static float COST_SCALE => BASE_COST_SCALE * UIScale;
        public static float DESCRIPTION_SCALE => BASE_DESCRIPTION_SCALE * UIScale;
        public static float BLOCK_SCALE => BASE_BLOCK_SCALE * UIScale;

        // Text offsets relative to card's top-left corner
        public static int NAME_OFFSET_X => TEXT_MARGIN_X;
        public static int NAME_OFFSET_Y => TEXT_MARGIN_Y;
        public static int COST_OFFSET_X => TEXT_MARGIN_X;
        public static int COST_OFFSET_Y => TEXT_MARGIN_Y + (int)Math.Round(34 * UIScale); // below name
        public static int DESCRIPTION_OFFSET_X => TEXT_MARGIN_X;
        public static int DESCRIPTION_OFFSET_Y => TEXT_MARGIN_Y + (int)Math.Round(84 * UIScale); // below cost
        // Block number bottom-right settings
        public static float BLOCK_NUMBER_SCALE => BASE_BLOCK_NUMBER_SCALE * UIScale;
        public static int BLOCK_NUMBER_MARGIN_X => (int)Math.Round(BASE_BLOCK_NUMBER_MARGIN_X * UIScale);
        public static int BLOCK_NUMBER_MARGIN_Y => (int)Math.Round(BASE_BLOCK_NUMBER_MARGIN_Y * UIScale);
        
        /// <summary>
        /// Gets a rectangle for the card visual positioned at the given point
        /// </summary>
        public static Rectangle GetCardVisualRect(Vector2 position)
        {
            return new Rectangle(
                (int)position.X - CARD_OFFSET_X, 
                (int)position.Y - CARD_OFFSET_Y, 
                CARD_WIDTH, 
                CARD_HEIGHT
            );
        }
        
        /// <summary>
        /// Gets a rectangle for the card hitbox positioned at the given point
        /// </summary>
        public static Rectangle GetCardBounds(Vector2 position)
        {
            return GetCardVisualRect(position); // Hitbox matches visual exactly
        }
        
        /// <summary>
        /// Gets a rectangle for the card highlight positioned at the given point
        /// </summary>
        public static Rectangle GetCardHighlightRect(Vector2 position)
        {
            return new Rectangle(
                (int)position.X - HIGHLIGHT_OFFSET_X, 
                (int)position.Y - HIGHLIGHT_OFFSET_Y, 
                HIGHLIGHT_WIDTH, 
                HIGHLIGHT_HEIGHT
            );
        }

        // Text absolute positions computed from card position
        public static Vector2 GetNameTextPosition(Vector2 position)
        {
            var rect = GetCardVisualRect(position);
            return new Vector2(rect.X + NAME_OFFSET_X, rect.Y + NAME_OFFSET_Y);
        }

        public static Vector2 GetCostTextPosition(Vector2 position)
        {
            var rect = GetCardVisualRect(position);
            return new Vector2(rect.X + COST_OFFSET_X, rect.Y + COST_OFFSET_Y);
        }

        public static Vector2 GetDescriptionTextPosition(Vector2 position)
        {
            var rect = GetCardVisualRect(position);
            return new Vector2(rect.X + DESCRIPTION_OFFSET_X, rect.Y + DESCRIPTION_OFFSET_Y);
        }

        public static Vector2 GetBlockTextPosition(Vector2 position)
        {
            var rect = GetCardVisualRect(position);
            return new Vector2(rect.X + TEXT_MARGIN_X, rect.Bottom - 60);
        }

        // Computes bottom-left anchored block number top-left position given measured size
        public static Vector2 GetBlockNumberPosition(Vector2 position, Vector2 measuredTextSize)
        {
            var rect = GetCardVisualRect(position);
            float x = rect.Left + BLOCK_NUMBER_MARGIN_X;
            float y = rect.Bottom - BLOCK_NUMBER_MARGIN_Y - measuredTextSize.Y;
            return new Vector2(x, y);
        }
    }
}