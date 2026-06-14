using Crusaders30XX.ECS.Core;

namespace Crusaders30XX.ECS.Input
{
    public enum CursorTargetKind
    {
        None,
        UI,
        Diagnostic,
    }

    public readonly record struct CursorTarget(
        Entity Entity,
        CursorTargetKind Kind,
        float Coverage)
    {
        public static CursorTarget None => new(null, CursorTargetKind.None, 0f);
        public bool HasTarget => Entity != null;
    }
}
