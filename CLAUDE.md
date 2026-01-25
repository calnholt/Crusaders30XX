# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```bash
# Build the project
dotnet build

# Run the game
dotnet run

# Publish for distribution
dotnet publish -c Release
```

This is a .NET 8.0 MonoGame DesktopGL project. Content assets are compiled via the MonoGame Content Builder pipeline (`Content/Content.mgcb`).

## Architecture

Crusaders30XX is a deckbuilder card game built with an Entity Component System (ECS) architecture.

### Core ECS (`ECS/Core/`)

- **Entity**: Container holding components (ID, Name, IsActive, component dictionary)
- **IComponent**: Base interface for all data containers
- **World**: Main ECS entry point aggregating EntityManager and SystemManager
- **System**: Abstract base class with `GetRelevantEntities()` and `UpdateEntity()` methods
- **EventManager**: Static pub-sub system for loose coupling between systems
- **EventQueue/EventQueueBridge**: Hybrid queue system separating rules-driven events from triggered events

### Key Directories

| Directory | Purpose |
|-----------|---------|
| `ECS/Components/` | Data containers (CardComponents, CombatComponents, etc.) |
| `ECS/Systems/` | Game logic |
| `ECS/Scenes/` | Scene-specific systems (BattleScene, WorldMapScene, ShopScene, etc.) |
| `ECS/Factories/` | Entity creation (CardFactory, EnemyFactory, etc.) |
| `ECS/Events/` | Event definitions for pub-sub communication |
| `ECS/Objects/` | Game entity definitions (Cards/, Enemies/, Equipment/, Medals/) |
| `ECS/Data/` | Data models, JSON loaders, save system |
| `ECS/Services/` | Business logic and calculations |
| `ECS/Singletons/` | Shared state managers (StateSingleton, FontSingleton) |
| `Content/Data/` | JSON game data (locations, decks, enemies) |

### Event Queue System

The game uses a hybrid event queue with two queues:
- **Rules Queue**: Mandatory events from core systems (phases, timers)
- **Trigger Queue**: Reactive events from abilities and conditions

Events have states: Pending → Resolving → Waiting → Complete. This ensures deterministic execution and supports multi-frame operations like animations.

### Game Flow

`Game1.cs` initializes the World, registers all systems, and runs the game loop. Systems are updated each frame via `World.Update()`.

### Card System

Cards have:
- Color costs: Red, White, Black, Any
- Card definitions in `ECS/Objects/Cards/` inheriting from `CardBase`

Combat uses block mechanics, enemy AI with planned attacks via `AttackIntent` and `PlannedAttack` components.

## Coding Standards

- Add `DebugEditable` and `DebugTab` attributes to systems with Draw functions
- Use imports, not fully-qualified names (e.g., avoid `Crusaders30XX.ECS.Data.Cards`)
- Prioritize readability over complexity

## Adding New Content

### New Card
1. Create class in `ECS/Objects/Cards/` inheriting from `CardBase`
2. Register in CardFactory if needed

### New System
1. Inherit from `ECS/Core/System`
2. Override `GetRelevantEntities()` and `UpdateEntity()`
3. Register with `world.AddSystem()` in `Game1.cs`
4. Add `DebugEditable`/`DebugTab` attributes if it has a Draw function

### New Component
1. Create class implementing `IComponent`
2. Add to entities via `world.AddComponent()`
