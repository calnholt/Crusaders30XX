@echo off
REM Script to sync CardFactory.cs with all card classes
setlocal enabledelayedexpansion

echo Scanning card files...

REM Use PowerShell to scan and update
powershell -ExecutionPolicy Bypass -Command ^
"$ErrorActionPreference = 'Stop'; ^
$projectRoot = Get-Location; ^
$cardsDir = Join-Path $projectRoot 'ECS\Objects\Cards'; ^
$factoryFile = Join-Path $projectRoot 'ECS\Factories\CardFactory.cs'; ^
^
if (-not (Test-Path $factoryFile)) { ^
    Write-Host 'Error: CardFactory.cs not found!' -ForegroundColor Red; ^
    exit 1; ^
} ^
^
Write-Host 'Scanning card files...' -ForegroundColor Cyan; ^
$cards = @{}; ^
$cardFiles = Get-ChildItem -Path $cardsDir -Filter '*.cs' -File; ^
^
foreach ($file in $cardFiles) { ^
    if ($file.Name -eq 'CardBase.cs' -or $file.Name -eq 'CardExecuteEnum.cs') { continue; } ^
    $content = Get-Content $file.FullName -Raw; ^
    if ($content -match 'public class (\w+)\s*:\s*CardBase') { ^
        $className = $Matches[1]; ^
        if ($content -match 'CardId\s*=\s*\"([^\"]+)\"') { ^
            $cardId = $Matches[1]; ^
            $cards[$cardId] = $className; ^
            Write-Host \"  Found: $cardId -> $className\" -ForegroundColor Green; ^
        } ^
    } ^
} ^
^
Write-Host \"`nFound $($cards.Count) cards total.\" -ForegroundColor Cyan; ^
^
Write-Host 'Reading CardFactory.cs...' -ForegroundColor Cyan; ^
$factoryContent = Get-Content $factoryFile -Raw; ^
^
Write-Host 'Extracting existing entries...' -ForegroundColor Cyan; ^
$existingCreate = @{}; ^
$existingGetAll = @{}; ^
^
if ($factoryContent -match '(?s)return cardId switch\s*\{([^}]+)\}') { ^
    $switchContent = $Matches[1]; ^
    $switchContent -split '`n' | ForEach-Object { ^
        if ($_ -match '\"([^\"]+)\"\s*=>\s*new\s+(\w+)\(\)') { ^
            $existingCreate[$Matches[1]] = $Matches[2]; ^
        } ^
    } ^
} ^
^
if ($factoryContent -match '(?s)return new Dictionary<string, CardBase>\s*\{([^}]+)\}') { ^
    $dictContent = $Matches[1]; ^
    $dictContent -split '`n' | ForEach-Object { ^
        if ($_ -match '\{\s*\"([^\"]+)\",\s*new\s+(\w+)\(\)\s*\}') { ^
            $existingGetAll[$Matches[1]] = $Matches[2]; ^
        } ^
    } ^
} ^
^
Write-Host \"Existing Create() entries: $($existingCreate.Count)\" -ForegroundColor Yellow; ^
Write-Host \"Existing GetAllCards() entries: $($existingGetAll.Count)\" -ForegroundColor Yellow; ^
^
$missingCreate = @{}; ^
$missingGetAll = @{}; ^
foreach ($cardId in $cards.Keys) { ^
    if (-not $existingCreate.ContainsKey($cardId)) { ^
        $missingCreate[$cardId] = $cards[$cardId]; ^
    } ^
    if (-not $existingGetAll.ContainsKey($cardId)) { ^
        $missingGetAll[$cardId] = $cards[$cardId]; ^
    } ^
} ^
^
if ($missingCreate.Count -eq 0 -and $missingGetAll.Count -eq 0) { ^
    Write-Host '`nAll cards are already in CardFactory.cs!' -ForegroundColor Green; ^
} else { ^
    Write-Host \"`nMissing entries in Create(): $($missingCreate.Count)\" -ForegroundColor Yellow; ^
    Write-Host \"Missing entries in GetAllCards(): $($missingGetAll.Count)\" -ForegroundColor Yellow; ^
    ^
    Write-Host '`nUpdating CardFactory.cs...' -ForegroundColor Cyan; ^
    ^
    REM Build sorted list of all cards (existing + missing) ^
    $allCards = @{}; ^
    foreach ($cardId in $cards.Keys) { ^
        $allCards[$cardId] = $cards[$cardId]; ^
    } ^
    $sortedCardIds = $allCards.Keys | Sort-Object; ^
    ^
    REM Rebuild Create() switch statement ^
    $newSwitchCases = @(); ^
    foreach ($cardId in $sortedCardIds) { ^
        $className = $allCards[$cardId]; ^
        $newSwitchCases += \"                `\"$cardId`\" => new $className(),\"; ^
    } ^
    $newSwitchContent = $newSwitchCases -join \"`n\"; ^
    $newSwitchContent += \"`n                _ => null\"; ^
    ^
    $factoryContent = [regex]::Replace($factoryContent, '(?s)(return cardId switch\s*\{)([^}]+)(\})', { param($m) $m.Groups[1].Value + \"`n$newSwitchContent`n            \" + $m.Groups[3].Value }); ^
    ^
    REM Rebuild GetAllCards() dictionary ^
    $newDictEntries = @(); ^
    foreach ($cardId in $sortedCardIds) { ^
        $className = $allCards[$cardId]; ^
        $newDictEntries += \"                { `\"$cardId`\", new $className() }\"; ^
    } ^
    $newDictContent = $newDictEntries -join \",`n\"; ^
    ^
    $factoryContent = [regex]::Replace($factoryContent, '(?s)(return new Dictionary<string, CardBase>\s*\{)([^}]+)(\})', { param($m) $m.Groups[1].Value + \"`n$newDictContent`n            \" + $m.Groups[3].Value }); ^
    ^
    Set-Content -Path $factoryFile -Value $factoryContent -NoNewline; ^
    Write-Host 'CardFactory.cs updated successfully!' -ForegroundColor Green; ^
} ^
^
Write-Host '`nRunning dotnet run...' -ForegroundColor Cyan; ^
Set-Location $projectRoot; ^
dotnet run"

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo Error occurred during execution.
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo Script completed successfully!
pause

