using System;
using System.Collections.Generic;

namespace Crusaders30XX.Diagnostics.Snapshots
{
    public enum DisplaySnapshotBaselineMode
    {
        None,
        Verify,
        Accept
    }

    public sealed class DisplaySnapshotLaunchOptions
    {
        public const string VerifyFlag = "--verify";
        public const string AcceptFlag = "--accept";

        public string FixtureId { get; init; }
        public string[] Args { get; init; } = Array.Empty<string>();
        public DisplaySnapshotBaselineMode BaselineMode { get; init; }

        public static bool TryParse(string[] args, out DisplaySnapshotLaunchOptions options)
        {
            options = null;
            if (args == null || args.Length < 2 || !string.Equals(args[0], "snapshot", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var fixtureArgs = new List<string>();
            bool verify = false;
            bool accept = false;

            for (int i = 2; i < args.Length; i++)
            {
                if (string.Equals(args[i], VerifyFlag, StringComparison.OrdinalIgnoreCase))
                {
                    verify = true;
                }
                else if (string.Equals(args[i], AcceptFlag, StringComparison.OrdinalIgnoreCase))
                {
                    accept = true;
                }
                else
                {
                    fixtureArgs.Add(args[i]);
                }
            }

            if (verify && accept)
            {
                throw new DisplaySnapshotSetupException(
                    $"{VerifyFlag} and {AcceptFlag} are mutually exclusive");
            }

            options = new DisplaySnapshotLaunchOptions
            {
                FixtureId = args[1],
                Args = fixtureArgs.ToArray(),
                BaselineMode = verify
                    ? DisplaySnapshotBaselineMode.Verify
                    : accept
                        ? DisplaySnapshotBaselineMode.Accept
                        : DisplaySnapshotBaselineMode.None
            };
            return true;
        }
    }
}
