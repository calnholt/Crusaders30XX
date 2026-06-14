using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Input
{
    public interface IPlayerInputSource
    {
        PlayerInputFrame Capture(
            bool isWindowActive,
            Rectangle renderDestination,
            int virtualWidth,
            int virtualHeight);

        void SetVibration(float lowFrequency, float highFrequency);
    }
}
