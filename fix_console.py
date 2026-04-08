import re
import sys

files_to_fix = [
    ("ECS/Scenes/BattleScene/TutorialDisplaySystem.cs", [
        ('Console.WriteLine($"[TutorialDisplaySystem] Started: {_currentTutorial.key}");', 'LoggingService.Append("TutorialDisplaySystem.OnTutorialStarted", new System.Text.Json.Nodes.JsonObject { ["tutorialKey"] = _currentTutorial.key });'),
        ('Console.WriteLine("[TutorialDisplaySystem] Cleaned up");', 'LoggingService.Append("TutorialDisplaySystem.CleanUp", new System.Text.Json.Nodes.JsonObject { ["message"] = "cleaned up" });'),
        ('Console.WriteLine("[TutorialDisplaySystem] Created continue button");', 'LoggingService.Append("TutorialDisplaySystem.CreateContinueButton", new System.Text.Json.Nodes.JsonObject { ["message"] = "created continue button" });'),
    ]),
    ("ECS/Scenes/BattleScene/PhaseCoordinatorSystem.cs", [
        ('Console.WriteLine($"[PhaseCoordinatorSystem] Incremented turn number: {oldTurn} -> {ps.TurnNumber} (previous sub: {ps.Sub})");', 'LoggingService.Append("PhaseCoordinatorSystem.OnChangePhaseFromEnemy", new System.Text.Json.Nodes.JsonObject { ["oldTurn"] = oldTurn, ["newTurn"] = ps.TurnNumber, ["previousSub"] = ps.Sub.ToString() });'),
        ('Console.WriteLine($"[PhaseCoordinatorSystem] Not incrementing turn (coming from StartBattle, turn={ps.TurnNumber})");', 'LoggingService.Append("PhaseCoordinatorSystem.OnChangePhaseFromEnemy", new System.Text.Json.Nodes.JsonObject { ["message"] = "Not incrementing turn (coming from StartBattle)", ["turn"] = ps.TurnNumber });'),
    ]),
    ("ECS/Scenes/BattleScene/EnemyDisplaySystem.cs", [
        ('System.Console.WriteLine($"[EnemyDisplaySystem] StartEnemyAttackAnimation context={evt.ContextId}");', 'LoggingService.Append("EnemyDisplaySystem.OnStartEnemyAttackAnimation", new System.Text.Json.Nodes.JsonObject { ["contextId"] = evt.ContextId });'),
    ]),
    ("ECS/Scenes/BattleScene/PledgeDisplaySystem.cs", [
        ('Console.WriteLine($"[PledgeDisplaySystem] Hovered card: adding preview pledge to {card.GetComponent<CardData>()?.Card.CardId ?? "unknown"}");', 'LoggingService.Append("PledgeDisplaySystem.OnCardHovered", new System.Text.Json.Nodes.JsonObject { ["cardId"] = card.GetComponent<CardData>()?.Card.CardId ?? "unknown", ["action"] = "adding preview pledge" });'),
        ('Console.WriteLine($"[PledgeDisplaySystem] Unhovered card: removing preview pledge from {card.GetComponent<CardData>()?.Card.CardId ?? "unknown"}");', 'LoggingService.Append("PledgeDisplaySystem.OnCardUnhovered", new System.Text.Json.Nodes.JsonObject { ["cardId"] = card.GetComponent<CardData>()?.Card.CardId ?? "unknown", ["action"] = "removing preview pledge" });'),
    ]),
    ("ECS/Scenes/BattleScene/SealManagementSystem.cs", [
        ('Console.WriteLine($"[SealManagementSystem] Card {cardData?.Card.CardId ?? "unknown"} has been sealed!");', 'LoggingService.Append("SealManagementSystem.OnApplySeal", new System.Text.Json.Nodes.JsonObject { ["cardId"] = cardData?.Card.CardId ?? "unknown", ["message"] = "card has been sealed" });'),
        ('Console.WriteLine($"[SealManagementSystem] Card {cardData?.Card.CardId ?? "unknown"} has been sealed (from draw pile)!");', 'LoggingService.Append("SealManagementSystem.OnCardMoved", new System.Text.Json.Nodes.JsonObject { ["cardId"] = cardData?.Card.CardId ?? "unknown", ["message"] = "card has been sealed from draw pile" });'),
        ('Console.WriteLine($"[SealManagementSystem] Card {cardData?.Card.CardId ?? "unknown"} seals modified by {evt.Delta}, now {sealedComp.Seals}");', 'LoggingService.Append("SealManagementSystem.OnModifySeals", new System.Text.Json.Nodes.JsonObject { ["cardId"] = cardData?.Card.CardId ?? "unknown", ["delta"] = evt.Delta, ["newSealCount"] = sealedComp.Seals });'),
        ('Console.WriteLine($"[SealManagementSystem] Sealed card {cardData?.Card.CardId ?? "unknown"} shuffled into draw pile!");', 'LoggingService.Append("SealManagementSystem.OnCardMoved", new System.Text.Json.Nodes.JsonObject { ["cardId"] = cardData?.Card.CardId ?? "unknown", ["message"] = "sealed card shuffled into draw pile" });'),
    ]),
]

for filepath, replacements in files_to_fix:
    try:
        with open(filepath, 'r') as f:
            content = f.read()
        
        for old, new in replacements:
            if old in content:
                content = content.replace(old, new)
            else:
                print(f"Warning: Could not find '{old[:50]}...' in {filepath}")
        
        with open(filepath, 'w') as f:
            f.write(content)
        print(f"Fixed {filepath}")
    except Exception as e:
        print(f"Error processing {filepath}: {e}")

