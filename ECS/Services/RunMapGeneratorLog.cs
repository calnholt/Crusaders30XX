#if DEBUG
using System;
using System.Globalization;
using System.IO;
using Crusaders30XX.ECS.Data.Locations;
using Crusaders30XX.ECS.Data.Save;

namespace Crusaders30XX.ECS.Services
{
	public static class RunMapGeneratorLog
	{
		private const string LogFileName = "run_map_generator.log";
		private const string HeaderPrefix = "generator_version=";

		public static void Append(RunMapSpreadMetrics metrics)
		{
			string directory = SaveCache.GetSaveDirectory();
			if (string.IsNullOrEmpty(directory)) return;

			try
			{
				if (!Directory.Exists(directory))
				{
					Directory.CreateDirectory(directory);
				}

				string path = Path.Combine(directory, LogFileName);
				EnsureVersionedHeader(path);
				File.AppendAllText(path, metrics.ToLogLine() + Environment.NewLine);
			}
			catch (Exception ex)
			{
				System.Console.WriteLine($"[RunMapGeneratorLog] Failed to write log: {ex.Message}");
			}
		}

		private static void EnsureVersionedHeader(string path)
		{
			int fileVersion = ReadFileVersion(path);
			if (fileVersion >= LocationMapConstants.MapGeneratorVersion && File.Exists(path))
			{
				return;
			}

			string header = HeaderPrefix + LocationMapConstants.MapGeneratorVersion.ToString(CultureInfo.InvariantCulture)
				+ Environment.NewLine;
			File.WriteAllText(path, header);
		}

		private static int ReadFileVersion(string path)
		{
			if (!File.Exists(path)) return 0;

			try
			{
				using var reader = new StreamReader(path);
				string line = reader.ReadLine();
				if (string.IsNullOrWhiteSpace(line) || !line.StartsWith(HeaderPrefix, StringComparison.Ordinal))
				{
					return 0;
				}

				string versionText = line.Substring(HeaderPrefix.Length).Trim();
				return int.TryParse(versionText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : 0;
			}
			catch
			{
				return 0;
			}
		}
	}
}
#endif
