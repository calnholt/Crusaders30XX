using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Systems
{
    public static class CardDisplayToggle
    {
        private static bool _useV2 = true;
        public static bool UseV2
        {
            get => _useV2;
            set
            {
                if (_useV2 != value)
                {
                    _useV2 = value;
                    EventManager.Publish(new CardDisplayToggleChangedEvent { UseV2 = value });
                }
            }
        }
    }
}
