using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Locations;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Services
{
	public static class PurchaseItemService
	{
		public struct PurchaseResult
		{
			public bool Success;
			public string ItemId;
			public int Spent;
			public int NewGold;
			public string LocationId;
			public string ShopName;
			public string Error;
		}

		public static bool CanAfford(int price)
		{
			try { return SaveCache.GetGold() >= price; }
			catch { return false; }
		}

		public static PurchaseResult TryPurchase(EntityManager entityManager, Entity itemEntity)
		{
			var result = new PurchaseResult { Success = false, ItemId = string.Empty, Spent = 0, NewGold = 0, LocationId = string.Empty, ShopName = string.Empty, Error = string.Empty };
			if (entityManager == null || itemEntity == null) { result.Error = "InvalidArgs"; return result; }

			var fs = itemEntity.GetComponent<ForSaleItem>();
			var ui = itemEntity.GetComponent<UIElement>();

			if (fs == null) { result.Error = "NotForSaleEntity"; return result; }
			if (fs.IsPurchased) { result.Error = "AlreadyPurchased"; return result; }

			int price = System.Math.Max(0, fs.Price);
			if (!CanAfford(price)) { result.Error = "InsufficientGold"; return result; }

			result.ItemId = fs.Id ?? string.Empty;
			result.Spent = price;
			result.ShopName = fs.SourceShopName ?? string.Empty;

			// Snapshot old gold for event payload
			int oldGold = 0;
			try { oldGold = SaveCache.GetGold(); } catch { oldGold = 0; }

			// Identify location id containing this shop/item for targeted JSON update
			string locationId = TryResolveLocationIdForShop(fs.Id, result.ShopName);
			result.LocationId = locationId ?? string.Empty;

			// Update save: subtract gold and add to collection
			if (!TrySpendGoldAndAddToCollection(fs.Id, price, out int newGold))
			{
				result.Error = "SaveUpdateFailed";
				return result;
			}
			result.NewGold = newGold;

			// Publish GoldChanged event so UI can update immediately
			try
			{
				EventManager.Publish(new GoldChanged
				{
					OldGold = oldGold,
					NewGold = newGold,
					Delta = newGold - oldGold,
					Reason = "Purchase"
				});
			}
			catch { }

			// Update location file to mark purchased (best-effort; non-fatal if it fails)
			TryMarkPurchasedInLocationJson(locationId, result.ShopName, fs.Id);

			// Refresh runtime caches so UI reflects new state
			try { LocationDefinitionCache.Reload(); } catch { }
			try { SaveCache.Reload(); } catch { }

			result.Success = true;
			return result;
		}

		private static bool TrySpendGoldAndAddToCollection(string itemId, int price, out int newGold)
		{
			newGold = 0;
			try
			{
				string savePath = ResolveSavePath();
				if (string.IsNullOrEmpty(savePath)) return false;
				var data = SaveRepository.Load(savePath) ?? new SaveFile();
				if (data.gold < price) return false;
				data.gold = System.Math.Max(0, data.gold - price);
				if (data.collection == null) data.collection = new System.Collections.Generic.List<string>();
				if (!string.IsNullOrWhiteSpace(itemId) && !data.collection.Contains(itemId))
				{
					data.collection.Add(itemId);
				}
				SaveRepository.Save(savePath, data);
				newGold = data.gold;
				return true;
			}
			catch
			{
				return false;
			}
		}

		private static string TryResolveLocationIdForShop(string itemId, string shopName)
		{
			try
			{
				var all = LocationDefinitionCache.GetAll();
				foreach (var kv in all)
				{
					var def = kv.Value;
					if (def?.pointsOfInterest == null) continue;
					foreach (var poi in def.pointsOfInterest)
					{
						if (poi?.type != PointOfInterestType.Shop) continue;
						if (!string.IsNullOrWhiteSpace(shopName) && !string.Equals(poi.name ?? string.Empty, shopName, StringComparison.OrdinalIgnoreCase)) continue;
						if (poi.forSale != null && poi.forSale.Any(f => string.Equals(f.id, itemId, StringComparison.OrdinalIgnoreCase)))
						{
							return kv.Key;
						}
					}
				}
			}
			catch { }
			return null;
		}

		private static bool TryMarkPurchasedInLocationJson(string locationId, string shopName, string itemId)
		{
			try
			{
				string locationsFolder = ResolveLocationsFolder();
				if (string.IsNullOrEmpty(locationsFolder) || !Directory.Exists(locationsFolder)) return false;

				// Try targeted file first if we know the location id
				if (!string.IsNullOrEmpty(locationId))
				{
					string file = Path.Combine(locationsFolder, locationId + ".json");
					if (File.Exists(file) && TryUpdateLocationFile(file, shopName, itemId)) return true;
				}

				// Fallback: scan all location files
				foreach (var file in Directory.GetFiles(locationsFolder, "*.json"))
				{
					if (TryUpdateLocationFile(file, shopName, itemId)) return true;
				}
			}
			catch { }
			return false;
		}

		private static bool TryUpdateLocationFile(string fileAbsPath, string shopName, string itemId)
		{
			try
			{
				var json = File.ReadAllText(fileAbsPath);
				var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = true };
				var node = JsonNode.Parse(json);
				if (node == null) return false;

				var pois = node["PointsOfInterest"] as JsonArray;
				if (pois == null) return false;

				foreach (var poiNode in pois.OfType<JsonNode>())
				{
					string name = poiNode?["Name"]?.GetValue<string>();
					if (!string.IsNullOrWhiteSpace(shopName) && !string.Equals(name ?? string.Empty, shopName, StringComparison.OrdinalIgnoreCase)) continue;

					var fsArray = poiNode?["ForSale"] as JsonArray;
					if (fsArray == null) continue;

					foreach (var itemNode in fsArray.OfType<JsonNode>())
					{
						string id = itemNode?["Id"]?.GetValue<string>();
						if (string.Equals(id ?? string.Empty, itemId ?? string.Empty, StringComparison.OrdinalIgnoreCase))
						{
							itemNode["IsPurchased"] = true;
							var newJson = node.ToJsonString(opts);
							File.WriteAllText(fileAbsPath, newJson);
							return true;
						}
					}
				}
			}
			catch { }
			return false;
		}

		private static string ResolveSavePath()
		{
			string root = FindProjectRootContaining("Crusaders30XX.csproj");
			return string.IsNullOrEmpty(root) ? string.Empty : Path.Combine(root, "ECS", "Data", "save_file.json");
		}

		private static string ResolveLocationsFolder()
		{
			string root = FindProjectRootContaining("Crusaders30XX.csproj");
			return string.IsNullOrEmpty(root) ? string.Empty : Path.Combine(root, "ECS", "Data", "Locations");
		}

		private static string FindProjectRootContaining(string filename)
		{
			try
			{
				var dir = new DirectoryInfo(System.AppContext.BaseDirectory);
				for (int i = 0; i < 6 && dir != null; i++)
				{
					var candidate = Path.Combine(dir.FullName, filename);
					if (File.Exists(candidate)) return dir.FullName;
					dir = dir.Parent;
				}
			}
			catch { }
			return null;
		}
	}
}

