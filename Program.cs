string debugCardId = null;
bool isCardDebug = false;

if (args.Length >= 1 && args[0] == "card-debug")
{
    isCardDebug = true;
    debugCardId = args.Length >= 2 ? args[1] : null;
}

using var game = new Crusaders30XX.Game1();
if (isCardDebug)
{
    game.CardDebugMode = true;
    game.CardDebugId = debugCardId;
}
game.Run();
