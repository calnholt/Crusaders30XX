using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Displays equipped equipment icons (head/chest/arms/legs) on the left side of the screen,
	/// grouped by type in vertical order: Head, Chest, Arms, Legs. Multiple items of the same
	/// type are drawn in a row for that type.
	/// </summary>
	public class EquipmentDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly ContentManager _content;
		private readonly Dictionary<string, Texture2D> _iconCache = new();

		// Layout constants (pixels)
		public int LeftMargin { get; set; } = 30;
		public int TopMargin { get; set; } = 120;
		public int IconSize { get; set; } = 48;
		public int ColGap { get; set; } = 8;
		public int RowGap { get; set; } = 12;

		public EquipmentDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, ContentManager content)
			: base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			_content = content;
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<Player>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		public void Draw()
		{
			var player = GetRelevantEntities().FirstOrDefault();
			if (player == null) return;

			// Gather equipment for this player
			var equipment = EntityManager.GetEntitiesWithComponent<EquippedEquipment>()
				.Where(e => e.GetComponent<EquippedEquipment>().EquippedOwner == player)
				.Select(e => e.GetComponent<EquippedEquipment>())
				.ToList();

			if (equipment.Count == 0) return;

			// Group and order types
			string[] order = new[] { "Head", "Chest", "Arms", "Legs" };
			int y = TopMargin;
			foreach (var type in order)
			{
				var items = equipment.Where(eq => string.Equals(eq.EquipmentType, type, StringComparison.OrdinalIgnoreCase)).ToList();
				if (items.Count == 0) continue;
				// Draw items in a row
				int x = LeftMargin;
				foreach (var item in items)
				{
					var tex = GetOrLoadIcon(type);
					if (tex != null)
					{
						var dest = new Rectangle(x, y, IconSize, IconSize);
						_spriteBatch.Draw(tex, dest, Color.White);
					}
					x += IconSize + ColGap;
				}
				y += IconSize + RowGap;
			}
		}

		private Texture2D GetOrLoadIcon(string type)
		{
			string key = type.ToLowerInvariant();
			if (_iconCache.TryGetValue(key, out var t) && t != null) return t;
			string assetName = key; // assumes head.png, chest.png, arms.png, legs.png in Content root
			try
			{
				var tex = _content.Load<Texture2D>(assetName);
				_iconCache[key] = tex;
				return tex;
			}
			catch
			{
				System.Console.WriteLine($"[EquipmentDisplaySystem] Missing icon for type '{type}' (expected content asset '{assetName}')");
				_iconCache[key] = null;
				return null;
			}
		}
	}
}


