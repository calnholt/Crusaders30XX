using System;
using System.Collections.Generic;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Medals
{
    public abstract class MedalBase : IDisposable
    {
        public string Id { get; set; }
        public string Name { get; set; } = "";
        public string Text { get; set; } = "";
        public EntityManager EntityManager { get; set; }
        public Entity MedalEntity { get; set; }
        
        public abstract void Initialize(EntityManager entityManager, Entity medalEntity);

        protected void EmitActivateEvent(){
            EventManager.Publish(new MedalActivateEvent { MedalEntity = MedalEntity });
        }

        public virtual void Activate(){
            Console.WriteLine($"[MedalBase] Activate: {Id}");
        }

        public virtual void Dispose()
        {
            Console.WriteLine($"[MedalBase] Dispose: {Id}");
        }
    }
}