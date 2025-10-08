using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Utils
{
	public static class TextUtils
	{
		public static List<string> WrapText(SpriteFont font, string text, float scale, int maxWidth)
		{
			var lines = new List<string>();
			if (font == null)
			{
				lines.Add(string.Empty);
				return lines;
			}
			if (string.IsNullOrWhiteSpace(text))
			{
				lines.Add(string.Empty);
				return lines;
			}
			string[] rawLines = text.Replace("\r", string.Empty).Split('\n');
			foreach (var raw in rawLines)
			{
				string line = string.Empty;
				foreach (var word in raw.Split(' '))
				{
					string candidate = string.IsNullOrEmpty(line) ? word : (line + " " + word);
					float width = font.MeasureString(candidate).X * scale;
					if (width > maxWidth && !string.IsNullOrEmpty(line))
					{
						lines.Add(line);
						line = word;
					}
					else
					{
						line = candidate;
					}
				}
				lines.Add(line);
			}
			return lines;
		}
	}
}


