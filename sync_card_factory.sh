#!/bin/bash
# Script to sync CardFactory.cs with all card classes

set -e

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CARDS_DIR="$PROJECT_ROOT/ECS/Objects/Cards"
FACTORY_FILE="$PROJECT_ROOT/ECS/Factories/CardFactory.cs"

echo "Scanning card files..."

# Check if CardFactory.cs exists
if [ ! -f "$FACTORY_FILE" ]; then
    echo "Error: CardFactory.cs not found!" >&2
    exit 1
fi

# Use Python for reliable parsing and file manipulation
python3 << 'PYTHON_SCRIPT'
import re
import os
import sys
import glob
from collections import OrderedDict

project_root = os.environ.get('PROJECT_ROOT', '.')
cards_dir = os.path.join(project_root, 'ECS', 'Objects', 'Cards')
factory_file = os.path.join(project_root, 'ECS', 'Factories', 'CardFactory.cs')

# Scan all card files
cards = {}
card_files = glob.glob(os.path.join(cards_dir, '*.cs'))

for file_path in card_files:
    filename = os.path.basename(file_path)
    if filename in ['CardBase.cs', 'CardExecuteEnum.cs']:
        continue
    
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # Extract class name
        class_match = re.search(r'public class (\w+)\s*:\s*CardBase', content)
        if not class_match:
            continue
        
        class_name = class_match.group(1)
        
        # Extract CardId
        card_id_match = re.search(r'CardId\s*=\s*"([^"]+)"', content)
        if not card_id_match:
            continue
        
        card_id = card_id_match.group(1)
        cards[card_id] = class_name
        print(f"  Found: {card_id} -> {class_name}")
    except Exception as e:
        print(f"Warning: Could not parse {filename}: {e}", file=sys.stderr)
        continue

print(f"\nFound {len(cards)} cards total.")

# Read CardFactory.cs
try:
    with open(factory_file, 'r', encoding='utf-8') as f:
        factory_content = f.read()
except Exception as e:
    print(f"Error reading CardFactory.cs: {e}", file=sys.stderr)
    exit(1)

# Extract existing entries from Create() method
existing_create = {}
switch_match = re.search(r'return cardId switch\s*\{([^}]+)\}', factory_content, re.DOTALL)
if switch_match:
    switch_content = switch_match.group(1)
    for line in switch_content.split('\n'):
        match = re.search(r'"([^"]+)"\s*=>\s*new\s+(\w+)\(\)', line)
        if match:
            existing_create[match.group(1)] = match.group(2)

# Extract existing entries from GetAllCards() method
existing_getall = {}
dict_match = re.search(r'return new Dictionary<string, CardBase>\s*\{([^}]+)\}', factory_content, re.DOTALL)
if dict_match:
    dict_content = dict_match.group(1)
    for line in dict_content.split('\n'):
        match = re.search(r'\{\s*"([^"]+)",\s*new\s+(\w+)\(\)\s*\}', line)
        if match:
            existing_getall[match.group(1)] = match.group(2)

print(f"Existing Create() entries: {len(existing_create)}")
print(f"Existing GetAllCards() entries: {len(existing_getall)}")

# Find missing entries
missing_create = {k: v for k, v in cards.items() if k not in existing_create}
missing_getall = {k: v for k, v in cards.items() if k not in existing_getall}

if not missing_create and not missing_getall:
    print("\nAll cards are already in CardFactory.cs!")
else:
    print(f"\nMissing entries in Create(): {len(missing_create)}")
    print(f"Missing entries in GetAllCards(): {len(missing_getall)}")
    
    print("\nUpdating CardFactory.cs...")
    
    # Sort all card IDs alphabetically
    sorted_card_ids = sorted(cards.keys())
    
    # Build new switch cases
    new_switch_cases = []
    for card_id in sorted_card_ids:
        class_name = cards[card_id]
        new_switch_cases.append(f'                "{card_id}" => new {class_name}(),')
    new_switch_cases.append('                _ => null')
    new_switch_content = '\n'.join(new_switch_cases)
    
    # Helper function to find matching closing brace
    def find_matching_brace(content, start_pos):
        brace_count = 0
        i = start_pos
        while i < len(content):
            if content[i] == '{':
                brace_count += 1
            elif content[i] == '}':
                brace_count -= 1
                if brace_count == 0:
                    return i
            i += 1
        return -1
    
    # Replace Create() switch statement
    switch_start = factory_content.find('return cardId switch')
    if switch_start != -1:
        brace_pos = factory_content.find('{', switch_start)
        if brace_pos != -1:
            closing_brace = find_matching_brace(factory_content, brace_pos)
            if closing_brace != -1:
                # Find the semicolon after the closing brace
                semicolon_pos = factory_content.find(';', closing_brace)
                if semicolon_pos != -1:
                    before = factory_content[:brace_pos + 1]
                    after = factory_content[closing_brace:]
                    factory_content = before + '\n' + new_switch_content + '\n            ' + after
    
    # Build new dictionary entries
    new_dict_entries = []
    for i, card_id in enumerate(sorted_card_ids):
        class_name = cards[card_id]
        if i < len(sorted_card_ids) - 1:
            new_dict_entries.append(f'                {{ "{card_id}", new {class_name}() }},')
        else:
            new_dict_entries.append(f'                {{ "{card_id}", new {class_name}() }}')
    new_dict_content = '\n'.join(new_dict_entries)
    
    # Replace GetAllCards() dictionary
    dict_start = factory_content.find('return new Dictionary<string, CardBase>')
    if dict_start != -1:
        brace_pos = factory_content.find('{', dict_start)
        if brace_pos != -1:
            closing_brace = find_matching_brace(factory_content, brace_pos)
            if closing_brace != -1:
                # Find the semicolon after the closing brace
                semicolon_pos = factory_content.find(';', closing_brace)
                if semicolon_pos != -1:
                    before = factory_content[:brace_pos + 1]
                    after = factory_content[closing_brace:]
                    factory_content = before + '\n' + new_dict_content + '\n            ' + after
    
    # Write updated file
    try:
        with open(factory_file, 'w', encoding='utf-8') as f:
            f.write(factory_content)
        print("CardFactory.cs updated successfully!")
    except Exception as e:
        print(f"Error writing CardFactory.cs: {e}", file=sys.stderr)
        exit(1)

PYTHON_SCRIPT

if [ $? -ne 0 ]; then
    echo "Error occurred during script execution."
    exit 1
fi

echo ""
echo "Running dotnet run..."
cd "$PROJECT_ROOT"
dotnet run
