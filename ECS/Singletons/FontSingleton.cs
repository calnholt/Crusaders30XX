using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Singletons
{
	public static class FontSingleton
	{
		private static SpriteFont _titleFont;
		private static SpriteFont _contentFont;
		private static readonly object _lock = new object();

		public static SpriteFont TitleFont => _titleFont;
		public static SpriteFont ContentFont => _contentFont;

		public static void Initialize(ContentManager content)
		{
			lock (_lock)
			{
				_titleFont = content.Load<SpriteFont>("Fonts/NewRocker");
				_contentFont = content.Load<SpriteFont>("Fonts/NewRocker");
			}
		}
	}
}

