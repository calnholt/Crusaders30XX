using System;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Factories;
using Crusaders30XX.ECS.Objects.Medals;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Services
{
	public static class RunMedalService
	{
		public static Entity AcquireAndEquip(EntityManager entityManager, string medalId)
		{
			if (entityManager == null || string.IsNullOrWhiteSpace(medalId)) return null;

			var player = entityManager.GetEntity("Player");
			if (player == null || !player.IsActive) return null;

			var medal = MedalFactory.Create(medalId);
			if (medal == null) return null;

			var medalEntity = entityManager.CreateEntity($"Medal_{medalId}_{Guid.NewGuid():N}");
			medal.Initialize(entityManager, medalEntity);
			entityManager.AddComponent(medalEntity, new EquippedMedal { EquippedOwner = player, Medal = medal });
			entityManager.AddComponent(medalEntity, ParallaxLayer.GetUIParallaxLayer());
			entityManager.AddComponent(medalEntity, new UIElement { IsInteractable = false });
			entityManager.AddComponent(medalEntity, new Transform { Position = Vector2.Zero, ZOrder = 10001 });
			if (!medalEntity.HasComponent<DontDestroyOnLoad>())
			{
				entityManager.AddComponent(medalEntity, new DontDestroyOnLoad());
			}

			medal.OnAcquire();
			return medalEntity;
		}
	}
}
