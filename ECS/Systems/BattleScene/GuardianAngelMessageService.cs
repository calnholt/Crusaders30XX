using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Utils;
using Crusaders30XX.ECS.Data.Cards;
using System.Collections.Generic;
using System;

namespace Crusaders30XX.ECS.Systems
{
  internal static class GuardianAngelMessageService
  {
    public static string GetMessage(GuardianMessageType guardianMessageType)
    {
        var messages = GetMessages(guardianMessageType);
        return messages[Random.Shared.Next(0, messages.Count - 1)];
    }
    private static List<string> GetMessages(GuardianMessageType guardianMessageType)
    {
        switch (guardianMessageType)
        {
            case GuardianMessageType.StartOfBattle:
            {
                return [
                    "You don't scare us! :P",
                    "Here we go!",
                    "You've got this!",
                    "For Christ!"
                ];
            }
            case GuardianMessageType.ActionPhase:
            {
                return [
                    "We're not going to let you get away with that! ^^;"
                ];
            }
            case GuardianMessageType.Temperance:
            {
                return [
                    "Don't call it a comeback! ^_^",
                ];
            }
            default: return ["Whoops I forgot what I was gunna say :/"];
        }
    }
  }
}

public enum GuardianMessageType
{
    StartOfBattle,
    Ambush,
    Intimidate,
    ActionPhase,
    Attack,
    Burn,
    Frozen,
    BreakFrozen,
    Penanace,
    Win,
    DemonIdle,
    OgreIdle,
    SuccubusIdle,
    SpiderIdle,
    Temperance,
}