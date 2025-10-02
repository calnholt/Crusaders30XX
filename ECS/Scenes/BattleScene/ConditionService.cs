using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Data.Attacks;

namespace Crusaders30XX.ECS.Systems
{
	public static class ConditionService
	{
		public static bool Evaluate(ConditionNode node, string contextId, EntityManager entityManager, Entity attacker = null, Entity target = null)
		{
			if (node == null) return false;
			return EvaluateNode(node, contextId, entityManager, attacker, target);
		}

		private static bool EvaluateNode(ConditionNode node, string contextId, EntityManager entityManager, Entity attacker, Entity target)
		{
			switch (node.kind)
			{
				case "All":
					if (node.children == null || node.children.Length == 0) return true;
					foreach (var c in node.children)
					{
						if (!EvaluateNode(c, contextId, entityManager, attacker, target)) return false;
					}
					return true;
				case "Any":
					if (node.children == null || node.children.Length == 0) return false;
					foreach (var c in node.children)
					{
						if (EvaluateNode(c, contextId, entityManager, attacker, target)) return true;
					}
					return false;
				case "Not":
					if (node.children == null || node.children.Length != 1) return false;
					return !EvaluateNode(node.children[0], contextId, entityManager, attacker, target);
				case "Leaf":
					return EvaluateLeaf(node, contextId, entityManager, attacker, target);
				default:
					return false;
			}
		}

		private static bool EvaluateLeaf(ConditionNode node, string contextId, EntityManager entityManager, Entity attacker, Entity target)
		{
			string type = node.leafType ?? string.Empty;
			switch (type)
			{
				case "PlayColorAtLeastN":
					return Leaf_PlayColorAtLeastN(node.@params, contextId, entityManager);
				case "PlayAtLeastN":
					return Leaf_PlayAtLeastN(node.@params, contextId, entityManager);
				case "OnHit":
					return Leaf_OnHit(node.@params, contextId, entityManager);
				case "BlockN":
					return Leaf_BlockN(node.@params, contextId, entityManager);
				default:
					return false;
			}
		}

		private static bool Leaf_PlayAtLeastN(Dictionary<string, string> parameters, string contextId, EntityManager entityManager)
		{
			if (parameters == null) return false;
			if (!parameters.TryGetValue("n", out var nStr)) return false;
			if (!int.TryParse(nStr, out var n)) return false;
			// Use EnemyAttackProgress counters
			foreach (var e in entityManager.GetEntitiesWithComponent<EnemyAttackProgress>())
			{
				var p = e.GetComponent<EnemyAttackProgress>();
				if (p != null && p.ContextId == contextId)
				{
					int valueTyped = p.PlayedCards;
					return valueTyped >= n;
				}
			}
			return false;
		}

		private static bool Leaf_PlayColorAtLeastN(Dictionary<string, string> parameters, string contextId, EntityManager entityManager)
		{
			if (parameters == null) return false;
			if (!parameters.TryGetValue("color", out var colorStr)) return false;
			if (!parameters.TryGetValue("n", out var nStr)) return false;
			if (!int.TryParse(nStr, out var n)) return false;
			string color = NormalizeColorKey(colorStr);
			// Use EnemyAttackProgress counters
			foreach (var e in entityManager.GetEntitiesWithComponent<EnemyAttackProgress>())
			{
				var p = e.GetComponent<EnemyAttackProgress>();
				if (p != null && p.ContextId == contextId)
				{
					int typedVal = color switch
					{
						"Red" => p.PlayedRed,
						"White" => p.PlayedWhite,
						"Black" => p.PlayedBlack,
						_ => 0
					};
					return typedVal >= n;
				}
			}
			return false;
		}

		private static bool Leaf_OnHit(Dictionary<string, string> parameters, string contextId, EntityManager entityManager)
		{
			var progress = entityManager.GetEntitiesWithComponent<EnemyAttackProgress>().FirstOrDefault().GetComponent<EnemyAttackProgress>();
			return progress.AssignedBlockTotal + progress.AegisTotal >= progress.BaseDamage;
		}

		private static bool Leaf_BlockN(Dictionary<string, string> parameters, string contextId, EntityManager entityManager)
		{
			if (parameters == null) return false;
			if (!parameters.TryGetValue("n", out var nStr)) return false;
			if (!int.TryParse(nStr, out var n)) return false;
			foreach (var e in entityManager.GetEntitiesWithComponent<EnemyAttackProgress>())
			{
				var p = e.GetComponent<EnemyAttackProgress>();
				if (p != null && p.ContextId == contextId)
				{
					return p.AssignedBlockTotal >= n;
				}
			}
			return false;
		}

		private static string NormalizeColorKey(string color)
		{
			string c = (color ?? string.Empty).Trim().ToLowerInvariant();
			switch (c)
			{
				case "r":
				case "red": return "Red";
				case "w":
				case "white": return "White";
				case "b":
				case "black": return "Black";
				default: return char.ToUpperInvariant(color[0]) + color.Substring(1);
			}
		}
	}
}


