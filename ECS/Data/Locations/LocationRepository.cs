using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Numerics;

namespace Crusaders30XX.ECS.Data.Locations
{
	public static class LocationRepository
	{
		public static Dictionary<string, LocationDefinition> LoadFromFolder(string folderAbsPath)
		{
			var map = new Dictionary<string, LocationDefinition>();
			if (!Directory.Exists(folderAbsPath)) return map;
			var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
			foreach (var file in Directory.GetFiles(folderAbsPath, "*.json"))
			{
				try
				{
					var json = File.ReadAllText(file);
					// Deserialize into DTO matching the JSON, then map to runtime definition types
					var dto = JsonSerializer.Deserialize<LocationFileDto>(json, opts);
					var def = Map(dto);
					if (def?.id != null) map[def.id] = def;
				}
				catch (System.Exception ex)
				{
					System.Console.WriteLine($"[LocationRepository] Failed to parse {file}: {ex.Message}");
				}
			}
			return map;
		}

		private static LocationDefinition Map(LocationFileDto dto)
		{
			if (dto == null) return null;
			var result = new LocationDefinition
			{
				id = dto.Id,
				name = dto.Name,
				pointsOfInterest = new List<PointOfInterestDefinition>()
			};
			if (dto.PointsOfInterest != null)
			{
				foreach (var poi in dto.PointsOfInterest)
				{
					var mappedPoi = new PointOfInterestDefinition
					{
						id = poi.Id,
						revealRadius = poi.RevealRadius,
						unrevealedRadius = poi.UnrevealedRadius,
						events = new List<LocationEventDefinition>()
					};
					if (poi.WorldPosition != null && poi.WorldPosition.Length >= 2)
					{
						mappedPoi.worldPosition = new Vector2(poi.WorldPosition[0], poi.WorldPosition[1]);
					}
					else
					{
						mappedPoi.worldPosition = new Vector2(0f, 0f);
					}
					if (poi.Events != null)
					{
						foreach (var e in poi.Events)
						{
							mappedPoi.events.Add(new LocationEventDefinition { id = e.Id, type = e.Type });
						}
					}
					result.pointsOfInterest.Add(mappedPoi);
				}
			}
			return result;
		}

		private class LocationFileDto
		{
			public string Id { get; set; }
			public string Name { get; set; }
			public List<PointOfInterestFileDto> PointsOfInterest { get; set; }
		}

		private class PointOfInterestFileDto
		{
			public string Id { get; set; }
			public float[] WorldPosition { get; set; }
			public int RevealRadius { get; set; }
			public int UnrevealedRadius { get; set; }
			public List<EventFileDto> Events { get; set; }
		}

		private class EventFileDto
		{
			public string Id { get; set; }
			public string Type { get; set; }
		}
	}
}


