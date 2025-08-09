using System;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Config
{
    /// <summary>
    /// Centralized configuration for card dimensions and positioning
    /// </summary>
    public static class CardConfig
    {
        // Card Visual Dimensions
        public const int CARD_WIDTH = 250;
        public const int CARD_HEIGHT = 350;
        
        // Card positioning offsets (relative to transform position)
        public const int CARD_OFFSET_X = CARD_WIDTH / 2;   // 100 - center the card horizontally
        public const int CARD_OFFSET_Y = (CARD_HEIGHT / 2) + 25;  // 175 - center vertically with slight offset
        
        // Highlight border dimensions (slightly larger than card)
        public const int HIGHLIGHT_BORDER_THICKNESS = 5;
        public const int HIGHLIGHT_WIDTH = CARD_WIDTH + (HIGHLIGHT_BORDER_THICKNESS * 2);   // 210
        public const int HIGHLIGHT_HEIGHT = CARD_HEIGHT + (HIGHLIGHT_BORDER_THICKNESS * 2); // 310
        public const int HIGHLIGHT_OFFSET_X = CARD_OFFSET_X + HIGHLIGHT_BORDER_THICKNESS;   // 105
        public const int HIGHLIGHT_OFFSET_Y = CARD_OFFSET_Y + HIGHLIGHT_BORDER_THICKNESS;   // 180
        
        // Hand layout settings
        // Distance between adjacent card centers = CARD_WIDTH + CARD_GAP
        public const int CARD_GAP = -20; // editable gap between cards' edges
        public const float CARD_SPACING = (float)(CARD_WIDTH + CARD_GAP);
        public const float HAND_BOTTOM_MARGIN = 150f; // pixels from bottom of screen

        // Hand fan (splay) settings
        public const float HAND_FAN_MAX_ANGLE_DEG = 5f;   // max tilt angle at edges
        public const float HAND_FAN_RADIUS = 0f;         // controls vertical arc curvature
        public const float HAND_FAN_CURVE_OFFSET = 0f;     // additional vertical offset
        public const float HAND_HOVER_LIFT = 10f;          // pixels to lift hovered card
        public const float HAND_HOVER_SCALE = 1.0f;        // reserved for future scale-up
        public const int HAND_Z_BASE = 100;
        public const int HAND_Z_STEP = 1;
        public const int HAND_Z_HOVER_BOOST = 1000;

        public static float DegToRad(float deg) => (float)(Math.PI / 180.0) * deg;

        // Tweening (movement smoothing)
        // Higher = snappier interpolation toward target per second
        public const float HAND_TWEEN_SPEED = 12f;
        
        // Card border thickness for visual
        public const int CARD_BORDER_THICKNESS = 3;

        // Draw pile display settings
        public const int DRAW_PILE_WIDTH = 120;
        public const int DRAW_PILE_HEIGHT = 160;
        public const int DRAW_PILE_MARGIN = 20;
        public const float DRAW_PILE_TEXT_SCALE = 0.8f;

        // Text layout settings
        public const int TEXT_MARGIN_X = 16;
        public const int TEXT_MARGIN_Y = 16;

        // Text scales
        public const float NAME_SCALE = 0.7f;
        public const float COST_SCALE = 0.6f;
        public const float DESCRIPTION_SCALE = 0.4f;
        public const float BLOCK_SCALE = 0.5f;

        // Text offsets relative to card's top-left corner
        public const int NAME_OFFSET_X = TEXT_MARGIN_X;
        public const int NAME_OFFSET_Y = TEXT_MARGIN_Y;
        public const int COST_OFFSET_X = TEXT_MARGIN_X;
        public const int COST_OFFSET_Y = TEXT_MARGIN_Y + 34; // below name
        public const int DESCRIPTION_OFFSET_X = TEXT_MARGIN_X;
        public const int DESCRIPTION_OFFSET_Y = TEXT_MARGIN_Y + 84; // below cost
        // Block number bottom-right settings
        public const float BLOCK_NUMBER_SCALE = 0.9f;
        public const int BLOCK_NUMBER_MARGIN_X = 14;
        public const int BLOCK_NUMBER_MARGIN_Y = 12;
        
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