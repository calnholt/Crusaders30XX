using System;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;

namespace Crusaders30XX.ECS.Objects.Equipment
{
  public abstract class EquipmentBase : IDisposable
  {
    public string Id { get; set; }
    public string Name { get; set; }
    public string Text { get; set; }
    public EntityManager EntityManager { get; set; }
    public int Block { get; set; }
    public int BlockUses { get; set; }
    public string Color { get; set; }
    public EquipmentSlot Slot { get; set; }
    public Entity EquipmentEntity { get; set; }

    public virtual void Initialize(EntityManager entityManager, Entity equipmentEntity) { }

    public abstract void Activate();

    protected void EmitActivateEvent(){
      EventManager.Publish(new EquipmentActivateEvent { EquipmentEntity = EquipmentEntity });
    }

    public virtual void Dispose()
    {
      Console.WriteLine($"[EquipmentBase] Dispose: {Id}");
    }

    public Func<bool> CanActivate { get; protected set; } = () => true;

  }


}