using System;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Factories;
using Crusaders30XX.ECS.Rendering;
using Crusaders30XX.ECS.Services;
using Crusaders30XX.ECS.Singletons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	internal static class ClimbSceneDrawHelpers
	{
		public static readonly Color Black0 = new Color(5, 5, 5);
		public static readonly Color Black1 = new Color(10, 10, 10);
		public static readonly Color Black2 = new Color(20, 20, 20);
		public static readonly Color Black3 = new Color(30, 30, 30);
		public static readonly Color Black4 = new Color(42, 42, 42);
		public static readonly Color White1 = Color.White;
		public static readonly Color White2 = new Color(240, 236, 230);
		public static readonly Color White3 = new Color(200, 192, 184);
		public static readonly Color Red3 = new Color(196, 30, 58);
		public static readonly Color Red2 = new Color(255, 77, 94);
		public static readonly Color RedDim = new Color(160, 0, 0);

		public static void DrawBorder(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, Color color, int thickness = 1)
		{
			if (rect.Width <= 0 || rect.Height <= 0 || thickness <= 0) return;
			spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
			spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
			spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
			spriteBatch.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
		}

		public static void DrawVerticalGradient(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, Color top, Color bottom, int strips = 16)
		{
			strips = Math.Max(1, strips);
			for (int i = 0; i < strips; i++)
			{
				int y0 = rect.Y + rect.Height * i / strips;
				int y1 = rect.Y + rect.Height * (i + 1) / strips;
				float t = strips <= 1 ? 0f : i / (float)(strips - 1);
				spriteBatch.Draw(pixel, new Rectangle(rect.X, y0, rect.Width, Math.Max(1, y1 - y0)), Color.Lerp(top, bottom, t));
			}
		}

		public static void DrawResourceIcon(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Texture2D pixel, Vector2 position, ClimbResourceType type, int size, Color color)
		{
			size = Math.Max(4, size);
			var rect = new Rectangle((int)position.X, (int)position.Y, size, size);
			switch (type)
			{
				case ClimbResourceType.Red:
					var circle = PrimitiveTextureFactory.GetAntiAliasedCircle(graphicsDevice, size / 2);
					spriteBatch.Draw(circle, rect, color);
					break;
				case ClimbResourceType.White:
					var triangle = PrimitiveTextureFactory.GetEquilateralTriangle(graphicsDevice, size);
					spriteBatch.Draw(triangle, rect, color);
					break;
				case ClimbResourceType.Black:
					spriteBatch.Draw(pixel, rect, Black0);
					DrawBorder(spriteBatch, pixel, rect, color, 2);
					break;
			}
		}

		public static void DrawHourglassIcon(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, Color frame, Color sand, bool filled)
		{
			DrawBorder(spriteBatch, pixel, rect, frame, 1);
			int middleY = rect.Y + rect.Height / 2;
			spriteBatch.Draw(pixel, new Rectangle(rect.X + 1, middleY, Math.Max(1, rect.Width - 2), 1), frame * 0.7f);
			if (!filled) return;
			spriteBatch.Draw(pixel, new Rectangle(rect.X + 2, rect.Y + 2, Math.Max(1, rect.Width - 4), Math.Max(1, rect.Height / 2 - 3)), sand);
			spriteBatch.Draw(pixel, new Rectangle(rect.X + 2, middleY + 2, Math.Max(1, rect.Width - 4), Math.Max(1, rect.Height / 2 - 4)), sand);
		}

		public static void DrawShopMarkerIcon(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, Color color)
		{
			spriteBatch.Draw(pixel, new Rectangle(rect.X + rect.Width / 5, rect.Y + rect.Height / 3, rect.Width * 3 / 5, rect.Height / 2), color);
			spriteBatch.Draw(pixel, new Rectangle(rect.X + rect.Width / 4, rect.Y + rect.Height / 5, rect.Width / 2, rect.Height / 5), color);
			DrawBorder(spriteBatch, pixel, rect, color, 1);
		}

		public static void DrawText(SpriteBatch spriteBatch, string text, Vector2 position, float scale, Color color)
		{
			if (string.IsNullOrEmpty(text)) return;
			spriteBatch.DrawString(FontSingleton.ContentFont, ToAscii(text), position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
		}

		public static string ToAscii(string text)
		{
			if (string.IsNullOrEmpty(text)) return string.Empty;
			var chars = text.ToCharArray();
			for (int i = 0; i < chars.Length; i++)
			{
				if (chars[i] < 32 || chars[i] > 126) chars[i] = '?';
			}
			return new string(chars);
		}

		public static string ResolveShopTitle(ClimbShopSlotSave slot)
		{
			if (slot == null) return "Empty";
			if (string.Equals(slot.kind, ClimbShopSlotKinds.Medal, StringComparison.OrdinalIgnoreCase))
				return MedalFactory.Create(slot.itemId)?.Name ?? "Medal";
			if (string.Equals(slot.kind, ClimbShopSlotKinds.Equipment, StringComparison.OrdinalIgnoreCase))
				return EquipmentFactory.Create(slot.itemId)?.Name ?? "Equipment";
			if (string.Equals(slot.kind, ClimbShopSlotKinds.Upgrade, StringComparison.OrdinalIgnoreCase))
				return ResolveCardName(slot.cardKey, fallback: "Upgrade");
			if (string.Equals(slot.kind, ClimbShopSlotKinds.Replacement, StringComparison.OrdinalIgnoreCase))
				return ResolveCardName(slot.cardKey, fallback: "New Card");
			return "Empty";
		}

		public static string ResolveCardName(string cardKey, string fallback)
		{
			if (!RunDeckService.TryParseCardKey(cardKey, out var cardId, out _, out var isUpgraded)) return fallback;
			var card = CardFactory.Create(cardId);
			if (card == null) return fallback;
			card.IsUpgraded = isUpgraded;
			return card.DisplayName;
		}

		public static string ResolveShopLabel(ClimbShopSlotSave slot)
		{
			if (slot == null) return string.Empty;
			if (string.Equals(slot.kind, ClimbShopSlotKinds.Medal, StringComparison.OrdinalIgnoreCase)) return "Medal";
			if (string.Equals(slot.kind, ClimbShopSlotKinds.Equipment, StringComparison.OrdinalIgnoreCase)) return "Gear";
			if (string.Equals(slot.kind, ClimbShopSlotKinds.Upgrade, StringComparison.OrdinalIgnoreCase)) return "Upgrade";
			if (string.Equals(slot.kind, ClimbShopSlotKinds.Replacement, StringComparison.OrdinalIgnoreCase)) return "Replace";
			return string.Empty;
		}

		public static string FormatResources(ClimbResourceSave resources, string empty = "0")
		{
			resources ??= new ClimbResourceSave { red = 0, white = 0, black = 0 };
			if (resources.red == 0 && resources.white == 0 && resources.black == 0) return empty;
			return $"R{resources.red} W{resources.white} B{resources.black}";
		}
	}
}
