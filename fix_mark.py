import re

with open("ECS/Scenes/BattleScene/MarkManagementSystem.cs", 'r') as f:
    content = f.read()

replacements = [
    ('Console.WriteLine("[MarkManagementSystem] Marked card held until PlayerEnd - applying 1 penance.");',
     'LoggingService.Append("MarkManagementSystem.OnChangeBattlePhase.PlayerEnd", new System.Text.Json.Nodes.JsonObject { ["message"] = "marked card held until PlayerEnd, applying 1 penance" });'),
    
    ('Console.WriteLine($"[MarkManagementSystem] Mark moved to {cardData?.Card?.CardId ?? "unknown"} with effect {newEffect}");',
     'LoggingService.Append("MarkManagementSystem.OnCardBlocked", new System.Text.Json.Nodes.JsonObject { ["cardId"] = cardData?.Card?.CardId ?? "unknown", ["newEffect"] = newEffect.ToString() });'),
    
    ('Console.WriteLine("[MarkManagementSystem] No eligible card to move mark to - mark disappears.");',
     'LoggingService.Append("MarkManagementSystem.OnCardBlocked", new System.Text.Json.Nodes.JsonObject { ["message"] = "no eligible card to move mark to, mark disappears" });'),
    
    ('Console.WriteLine("[MarkManagementSystem] Marked card pledged - applying 1 penance.");',
     'LoggingService.Append("MarkManagementSystem.OnPledgeAdded", new System.Text.Json.Nodes.JsonObject { ["message"] = "marked card pledged, applying 1 penance" });'),
    
    ('Console.WriteLine("[MarkManagementSystem] No cards in hand to mark.");',
     'LoggingService.Append("MarkManagementSystem.ApplyNewMark", new System.Text.Json.Nodes.JsonObject { ["message"] = "no cards in hand to mark" });'),
    
    ('Console.WriteLine("[MarkManagementSystem] No eligible cards to mark.");',
     'LoggingService.Append("MarkManagementSystem.ApplyNewMark", new System.Text.Json.Nodes.JsonObject { ["message"] = "no eligible cards to mark" });'),
    
    ('Console.WriteLine($"[MarkManagementSystem] Marked card: {cardData?.Card?.CardId ?? "unknown"} with effect {effect}");',
     'LoggingService.Append("MarkManagementSystem.ApplyNewMark", new System.Text.Json.Nodes.JsonObject { ["cardId"] = cardData?.Card?.CardId ?? "unknown", ["effect"] = effect.ToString() });'),
    
    ('Console.WriteLine("[MarkManagementSystem] Applying penalty: Lose 1 HP");',
     'LoggingService.Append("MarkManagementSystem.ApplyMarkEffect", new System.Text.Json.Nodes.JsonObject { ["effect"] = "Lose1HP" });'),
    
    ('Console.WriteLine("[MarkManagementSystem] Applying penalty: Lose 2 HP");',
     'LoggingService.Append("MarkManagementSystem.ApplyMarkEffect", new System.Text.Json.Nodes.JsonObject { ["effect"] = "Lose2HP" });'),
    
    ('Console.WriteLine("[MarkManagementSystem] Applying penalty: Gain 1 Penance");',
     'LoggingService.Append("MarkManagementSystem.ApplyMarkEffect", new System.Text.Json.Nodes.JsonObject { ["effect"] = "Gain1Penance" });'),
    
    ('Console.WriteLine("[MarkManagementSystem] Applying penalty: Gain 2 Bleed");',
     'LoggingService.Append("MarkManagementSystem.ApplyMarkEffect", new System.Text.Json.Nodes.JsonObject { ["effect"] = "Gain2Bleed" });'),
    
    ('Console.WriteLine("[MarkManagementSystem] Applying penalty: Gain 1 Burn");',
     'LoggingService.Append("MarkManagementSystem.ApplyMarkEffect", new System.Text.Json.Nodes.JsonObject { ["effect"] = "Gain1Burn" });'),
]

for old, new in replacements:
    if old in content:
        content = content.replace(old, new)
    else:
        print(f"Warning: Could not find '{old[:60]}'")

with open("ECS/Scenes/BattleScene/MarkManagementSystem.cs", 'w') as f:
    f.write(content)

print("Fixed MarkManagementSystem.cs")
