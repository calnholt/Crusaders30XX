using System;

namespace Crusaders30XX.Diagnostics.Snapshots
{
    public sealed class DisplaySnapshotLaunchOptions
    {
        public string FixtureId { get; init; }
        public string[] Args { get; init; } = Array.Empty<string>();

        public static bool TryParse(string[] args, out DisplaySnapshotLaunchOptions options)
        {
            options = null;
            if (args == null || args.Length < 2 || !string.Equals(args[0], "snapshot", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            options = new DisplaySnapshotLaunchOptions
            {
                FixtureId = args[1],
                Args = args.Length > 2 ? args[2..] : Array.Empty<string>()
            };
            return true;
        }
    }
}
