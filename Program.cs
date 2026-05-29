using Crusaders30XX.Diagnostics.Snapshots;

DisplaySnapshotLaunchOptions snapshotOptions = null;
if (DisplaySnapshotLaunchOptions.TryParse(args, out var parsed))
{
    snapshotOptions = parsed;
}

using var game = new Crusaders30XX.Game1(snapshotOptions);
game.Run();
