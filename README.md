# Crusaders30XX - ECS Deckbuilder Card Game

A deckbuilder card game built with MonoGame using Entity Component System (ECS) architecture.

## Architecture Overview

This project implements a robust ECS (Entity Component System) architecture designed specifically for card games. The architecture provides:

- **Separation of Concerns**: Data (Components), Logic (Systems), and Identity (Entities) are cleanly separated
- **Modularity**: Easy to add new card types, game mechanics, and systems
- **Performance**: Efficient entity queries and component management
- **Scalability**: Can handle complex card interactions and game states

## Core ECS Structure

### Entities
Entities are simple containers that hold components. They have:
- Unique ID
- Name
- Active/Inactive state
- Component collection

### Components
Components hold data and should be lightweight. Key components include:

#### Card Components
- `CardData`: Basic card information (name, description, cost, type, rarity)

#### Game State Components
- `Player`: Player stats (health, energy, block, gold)
- `Enemy`: Enemy stats and AI state
- `Deck`: Deck management (draw pile, discard pile, hand, exhaust pile)
- `GameState`: Overall game state (phase, turn, player turn)

#### Rendering Components
- `Transform`: Position, rotation, scale, z-order
- `Sprite`: Texture, tint, visibility
- `UIElement`: UI bounds, interaction state
- `Animation`: Animation state and timing

### Systems
Systems contain the logic that operates on entities with specific components:

#### Core Systems
- `DeckManagementSystem`: Handles shuffling, drawing, discarding
- `CombatSystem`: Manages damage, block, and combat mechanics
- `RenderingSystem`: Handles sprite rendering and animations
- `InputSystem`: Processes user input and UI interactions

## Project Structure

```
Crusaders30XX/
├── ECS/
│   ├── Core/
│   │   ├── Entity.cs              # Base entity class
│   │   ├── IComponent.cs          # Component interface
│   │   ├── EntityManager.cs       # Entity lifecycle management
│   │   ├── System.cs              # Base system class
│   │   ├── SystemManager.cs       # System coordination
│   │   └── World.cs               # Main ECS entry point
│   ├── Components/
│   │   └── CardComponents.cs      # All game components
│   ├── Systems/
│   │   └── CardGameSystems.cs     # Game-specific systems
│   └── Factories/
│       └── EntityFactory.cs       # Entity creation helpers
├── Game1.cs                       # Main game class
└── README.md                      # This file
```

## Usage Examples

### Creating a Card
```csharp
var card = EntityFactory.CreateCard(world, "Fireball", "Deal 8 damage", 2, 
    CardData.CardType.Attack, CardData.CardRarity.Uncommon);
```

### Creating a Player
```csharp
var player = EntityFactory.CreatePlayer(world, "Hero");
```

### Adding a System
```csharp
var customSystem = new CustomSystem(world.EntityManager);
world.AddSystem(customSystem);
```

### Querying Entities
```csharp
// Get all entities with a specific component
var cards = world.GetEntitiesWithComponent<CardData>();

// Get all entities with multiple components
var playableCards = world.EntityManager.GetEntitiesWithComponents(
    typeof(CardData), typeof(CardInPlay));
```

## Card Game Features

### Card Types
- **Attack**: Deal damage to enemies
- **Skill**: Provide utility effects
- **Power**: Provide ongoing effects
- **Curse**: Negative effects
- **Status**: Temporary effects

### Rarity System
- **Common**: Basic cards
- **Uncommon**: Better than common
- **Rare**: Powerful cards
- **Legendary**: Most powerful cards

### Deck Management
- Draw pile shuffling
- Hand size limits
- Discard pile management
- Exhaust pile for removed cards

### Combat System
- Damage calculation with block
- Energy management
- Turn-based combat
- Enemy AI framework

## Extending the System

### Adding New Components
1. Create a new class implementing `IComponent`
2. Add the component to entities using `world.AddComponent()`
3. Create systems that operate on the new component

### Adding New Systems
1. Inherit from `Core.System`
2. Override `GetRelevantEntities()` to specify which entities to process
3. Override `UpdateEntity()` to implement the system logic
4. Register the system with `world.AddSystem()`

### Adding New Card Types
1. Extend `CardData.CardType` enum
2. Create card creation methods in `EntityFactory`
3. Add card-specific logic to relevant systems

## Performance Considerations

- Components are stored in dictionaries for O(1) access
- Entity queries are cached for efficiency
- Systems only process relevant entities
- Component addition/removal is optimized

## Future Enhancements

- **Card Effects System**: Scriptable card effects
- **AI System**: Enemy AI and card playing
- **Save/Load System**: Game state persistence
- **Networking**: Multiplayer support
- **Modding Support**: Plugin system for custom cards
- **Visual Effects**: Particle systems and animations
- **Sound System**: Audio integration
- **UI Framework**: Advanced UI components

## Getting Started

1. Build the project
2. Run the game
3. The game will create a basic setup with:
   - A player entity
   - A deck with basic cards
   - Some enemy entities
   - UI elements

## Contributing

When adding new features:
1. Follow the ECS pattern
2. Keep components data-only
3. Put logic in systems
4. Use the factory pattern for entity creation
5. Document new components and systems

## License

This project is open source and available under the MIT License. "# Crusaders30XX" 
