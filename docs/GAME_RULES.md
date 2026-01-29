# Game Rules

## Overview

Crusaders30XX is a deckbuilder card game where players battle enemies using a hand of cards for both blocking attacks and taking offensive actions.

## Turn Structure

### 1. Start of Battle Phase

- Some enemy passives may trigger at battle start
- Player draws cards up to their **max hand size (4)**

### 2. Block Phase

- **Enemy attacks first** with one or more attacks
- Player can see the **number of attacks queued**, but not what the attacks are
- Player may assign cards from hand to block incoming attacks
- Cards have block values, typically **2-4**
- Some enemy attacks have additional effects that trigger when they deal damage or have special blocking conditions
- Player decides how to block (or whether to block at all)
- All attacks resolve

### 3. Action Phase

- Player gains **1 Action Point (AP)**
- Player may play:
  - **One card** that costs an action point
  - **Any number of free action cards** in any order
- Some cards have **discard costs** requiring the player to discard cards of a specific or any color
- Player can end their turn at any time

### 4. Pledge Phase

If the player:
- Does **not** already have a pledged card, AND
- Ended their action phase with card(s) in hand

Then the player **may pledge a card** from hand.

**Pledge rules:**
- A pledged card does not count towards max hand size
- A pledged card **must be used as an action** (cannot be used to block or pay for another card's cost)

### 5. Draw Phase

- Player draws up to their max hand size

### 6. Enemy Turn

- Enemy takes their turn
- Loop returns to Block Phase

## Win/Lose Conditions

- **Victory:** Enemy HP reaches 0
- **Defeat:** Player HP reaches 0
- **No reshuffle:** Your deck does not reshuffle when it empties

## Card Colors

There are three card colors, each with distinct blocking mechanics:

| Color | Block Bonus | Resource Gained |
|-------|-------------|-----------------|
| **Red** | Standard | Courage |
| **White** | Standard | Temperance |
| **Black** | +1 block | None |

### Red Cards & Courage

- Blocking with red cards grants **Courage**
- Courage is a resource that cards can spend or check thresholds against

### White Cards & Temperance

- Blocking with white cards grants **Temperance**
- When you reach your **Temperance threshold**, your equipped temperance ability auto-triggers

### Black Cards

- Block for **+1 compared to red/white counterparts**
- Do not grant any resource when blocking

## Deck Building Rules

- Each card exists in all three colors (red, white, black variants)
- Maximum of **2 copies** of the same card name in your deck

## Core Strategy

The central puzzle each turn is **maximizing value from your hand**:

- You don't want to carry cards to the next turn (unless pledged) because you draw to max hand size
- Balance blocking vs. saving cards for actions
- Consider resource generation (Courage/Temperance) when choosing which cards to block with
- Manage your pledge slot for cards you want to guarantee playing
