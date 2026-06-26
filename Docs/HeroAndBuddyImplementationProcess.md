# Hero and Buddy Implementation Process

This document records how hero power information and the matching hero buddy behavior are implemented in the project.

## Data Source

- Hero, hero power, and buddy metadata comes from `Assets/LearnHearthstone/Resources/Data/battlegroundsHeroes.json`.
- Runtime loading is handled by `HeroCatalogLoader`, producing `HeroCatalog`.
- Logic must use stable card ids:
  - `heroCardId` identifies the hero.
  - `heroPower.cardId` identifies the active or passive hero power.
  - `buddy.cardId` identifies the matching buddy.
- Display names are documentation and UI text only; they are not logic keys.

## Runtime Entry Points

- `MatchService` owns player commands and Tavern lifecycle.
- `HeroEffectEngine` owns card-id keyed hero power and buddy behavior.
- `TavernState.HeroEffectCounters` stores persistent per-hero state such as use counts, turn counters, discounts, and once-per-game flags.
- `HeroEffectImplementationRegistry` records whether each hero/buddy pair is `Implemented`, `FrameworkFirst`, `Deferred`, or otherwise visible.

Current dispatch events:

- `MatchStarted`
- `HeroPowerUsed`
- `CardBought`
- `CardPlayed`
- `TavernSpellCast`
- `MinionSold`
- `ShopRefreshed`
- `TurnStarted`
- `TurnEnded`

## Implementation Steps

1. Read the hero row from `battlegroundsHeroes.json` and record the hero power and buddy card ids.
2. Add card-id constants in `HeroEffectEngine`.
3. Decide which dispatch event owns the hero power behavior.
4. Add any needed counter keys to `HeroEffectEngine`.
5. Implement the hero power first, then the buddy reaction or enhancement.
6. If a generated card or spell is needed, create it in `HeroEffectEngine` and add playback behavior to `TavernSpellEngine` when it must be cast.
7. Add or update `HeroEffectImplementationRegistry` with an exact status and note.
8. Update `Docs/HeroPowerBuddyEffectsImplementationOrder.md` and `Docs/HeroPowerBuddyEffectsImplementationPlan.md`.
9. Add focused EditMode tests in `HeroPowerBuddyEffectTests`.
10. Run `HeroPowerBuddyEffectTests`, `HeroEffectImplementationRegistryTests`, and the JSON/document coverage audit.

## Handling Partial Mechanics

Do not leave unsupported behavior silent.

Use `FrameworkFirst` when the visible behavior needs a missing subsystem, such as:

- true two-target hero power commands;
- public Battlecry replay;
- per-card Tavern Frozen state;
- start-of-combat hero/buddy hooks;
- full multi-opponent lobby scheduling, even though single-opponent warband/history snapshots now exist;
- real combat-death or Deathrattle hooks.

When a proxy is implemented, the registry and docs must say exactly what the proxy does. Current examples:

- Shudderwock records a Battlecry replay proxy; Muckslinger reward is implemented.
- Vol'jin uses one explicit target plus a random friendly partner; Master Gadrin still needs start-of-combat support.
- Sindragosa uses whole-shop freeze and tagged frozen cards; exact smaller shop and single-card Frozen state remain future work.
- Deathrattle/combat-death buddies that lack a combat death hook use the Tavern sell event as the current death proxy.

## Verification Commands

Run hero/buddy behavior tests:

```powershell
$unity = 'D:\unity hub Editor\6000.4.10f1\Editor\Unity.exe'
$project = 'D:\unity project\Learn Heartstone'
$results = Join-Path $project 'TestResults-HeroPowerBuddyEffectTests.xml'
$log = Join-Path $project 'Logs-HeroPowerBuddyEffectTests.log'
Remove-Item -LiteralPath $results,$log -ErrorAction SilentlyContinue
$argString = "-batchmode -projectPath `"$project`" -runTests -testPlatform EditMode -testFilter `"HeroPowerBuddyEffectTests`" -testResults `"$results`" -logFile `"$log`""
$p = Start-Process -FilePath $unity -ArgumentList $argString -WindowStyle Hidden -PassThru
$p.WaitForExit()
```

Run registry tests:

```powershell
$results = Join-Path $project 'TestResults-HeroEffectImplementationRegistryTests.xml'
$log = Join-Path $project 'Logs-HeroEffectImplementationRegistryTests.log'
Remove-Item -LiteralPath $results,$log -ErrorAction SilentlyContinue
$argString = "-batchmode -projectPath `"$project`" -runTests -testPlatform EditMode -testFilter `"HeroEffectImplementationRegistryTests`" -testResults `"$results`" -logFile `"$log`""
$p = Start-Process -FilePath $unity -ArgumentList $argString -WindowStyle Hidden -PassThru
$p.WaitForExit()
```

Run coverage audit:

```powershell
@'
import json
from pathlib import Path
root = Path('.')
data = json.loads((root / 'Assets/LearnHearthstone/Resources/Data/battlegroundsHeroes.json').read_text(encoding='utf-8'))
heroes = data['heroes']
doc = (root / 'Docs/HeroPowerBuddyEffectsImplementationOrder.md').read_text(encoding='utf-8')
missing = [hero['name'] for hero in heroes if hero.get('name') and hero['name'] not in doc]
print(f"heroes={len(heroes)} documented={len(heroes)-len(missing)} missing={len(missing)}")
if missing:
    print('\n'.join(missing))
'@ | python -
```
