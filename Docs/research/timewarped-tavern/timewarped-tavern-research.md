# Timewarped Tavern Data Research

Generated: 2026-06-27T01:47:35.416Z

## Sources

- Firestone static card data: https://static.firestoneapp.com/data/cards/cards_enUS.gz.json
- Firestone zhCN static card data: https://static.firestoneapp.com/data/cards/cards_zhCN.gz.json
- HearthstoneJSON fallback/search data: https://api.hearthstonejson.com/v1/latest/enUS/cards.json
- Card art URL pattern: https://static.zerotoheroes.com/hearthstone/cardart/256x/{cardId}.jpg

## Filter

Current Firestone list:
`type == "Minion" && premium != true && mechanics includes "BACON_TIMEWARPED" && isBaconPool == true`.

All Firestone Timewarped minions:
`type == "Minion" && premium != true && mechanics includes "BACON_TIMEWARPED"`.

## Counts

- Current Firestone pool: 125
- All Firestone Timewarped minions in static card data: 158
- HearthstoneJSON name/text fallback hits: 159

## Current Firestone Timewarped Minions

| Card ID | Name | Tier | Stats | Tribe | Text |
| --- | --- | ---: | --- | --- | --- |
| `BG34_Giant_591` | Timewarped Acolyte | 3 | 4/6 | MURLOC | At the start of your turn, spin the Wheel of<br>Yogg-Saron. |
| `BG34_Giant_009` | Timewarped Alleycat | 3 | 7/7 | BEAST | At the end of your turn, summon a Tabbycat with this minion's stats. |
| `BG34_Giant_007` | Timewarped Annoy-o-Tron | 3 | 6/6 | MECH | <b>Taunt</b><br><b>Divine Shield</b><br><b>Reborn</b> |
| `BG34_Giant_212` | Timewarped Archer | 3 | 4/9 | NAGA | <b>Spellcraft:</b> Give a minion +12 Attack. |
| `BG34_Giant_071` | Timewarped Bassgill | 3 | 7/4 | MURLOC | [x]<b>Deathrattle:</b> Summon the<br>highest-Health minion from<br>your hand and give it <b>Divine<br> Shield</b> for this combat only. |
| `BG34_Giant_201` | Timewarped Boar | 3 | 1/1 | BEAST | [x]Whenever every third friendly<br>Timewarped Boar dies, get<br>a random Golden Beast.<br><i>(0 left!)</i> |
| `BG34_Giant_594` | Timewarped Botani | 3 | 8/6 | NONE | At the end of your turn,<br>get a random minion<br>of your Tier. |
| `BG34_Giant_001` | Timewarped Busker | 3 | 4/2 | PIRATE | <b>Battlecry and Deathrattle:</b> Gain 1 Gold next turn. |
| `BG34_Giant_679` | Timewarped Chimera | 3 | 1/8 | ALL | Whenever this takes damage, give a friendly minion of each type +2/+1 permanently. |
| `BG34_Giant_210` | Timewarped Commander | 3 | 5/5 | NAGA | [x]<b>Spellcraft:</b> Give a minion<br>+2/+2 for each friendly<br>Naga. |
| `BG34_Giant_302` | Timewarped Copter | 3 | 4/6 | MECH | <b>Divine Shield</b><br><b>Avenge (3):</b> Get a<br>random Mech. |
| `BG34_Giant_012` | Timewarped Cyclone | 3 | 6/1 | ELEMENTAL | <b>Divine Shield</b><br> <b>Windfury</b><br> <b>Reborn</b> |
| `BG34_Giant_081` | Timewarped Deathswarmer | 3 | 1/9 | UNDEAD | [x]Whenever this takes<br>damage, your Undead have<br>+1 Attack this game<br><i>(wherever they are)</i>. |
| `BG34_Giant_583` | Timewarped Devourer | 3 | 5/5 | DEMON | [x]At the start of your turn,<br>consume the Demon to<br>the right to gain its stats<br>and 3 Gold. |
| `BG34_Giant_029` | Timewarped Dragonling | 3 | 3/3 | DRAGON | [x]<b>Start of Combat:</b> Give this<br>minion and its neighbors<br> stats equal to your Tier. |
| `BG34_Giant_038` | Timewarped Elise | 3 | 6/7 | NONE | After you <b>Refresh</b> 5 times, make the highest-Tier minion in the Tavern Golden. <i>(5 left!)</i> |
| `BG34_Giant_332` | Timewarped Embalmer | 3 | 10/10 | UNDEAD | [x]One minion you summon<br>each turn gains <b>Reborn</b>.<br><i>(1 left!)</i> |
| `BG34_Giant_590` | Timewarped Festergut | 3 | 7/3 | UNDEAD | [x]<b>Deathrattle:</b> Summon<br>and get a random<br>Undead Creation. |
| `BG34_Giant_305` | Timewarped Geomancer | 3 | 2/9 | QUILBOAR | <b>Avenge (5):</b> Get a<br><b>Blood Gem</b>. Your <b>Blood Gems</b> give an extra +1/+1 this game. |
| `BG34_Giant_041` | Timewarped Greenskeeper | 3 | 5/9 | DRAGON | [x]<b>Rally:</b> Trigger your<br>right-most <b>Battlecry</b> and<br><b>Deathrattle</b>. |
| `BG34_Giant_593` | Timewarped Henchman | 3 | 5/7 | NONE | [x]After you kill a second<br>minion each combat,<br>get a plain copy of it. |
| `BG34_Giant_581` | Timewarped Hyena | 3 | 3/5 | BEAST | Whenever a friendly Beast dies, gain +2/+2 permanently. |
| `BG34_Giant_306` | Timewarped Jazzer | 3 | 5/3 | QUILBOAR | [x]<b>Deathrattle:</b> Your<br><b>Blood Gems</b> give an extra<br>+{1} Health this game. |
| `BG34_Giant_584` | Timewarped Kil'rek | 3 | 4/7 | DEMON | <b>Taunt</b><br><b>Deathrattle:</b> Get a<br>random Demon. |
| `BG34_Giant_031` | Timewarped Leapfrogger | 3 | 3/3 | BEAST | [x]<b>Taunt</b>, <b>Reborn</b><br><b>Deathrattle:</b> Give a friendly<br>Beast +1/+1 and<br>this <b>Deathrattle</b>. |
| `BG34_Giant_602` | Timewarped Lei | 3 | 3/5 | NONE | [x]At the start of your turn,<br>get the <b>Buddy</b> of your<br>Hero Power. |
| `BG34_Giant_066` | Timewarped Lubber | 3 | 5/7 | ELEMENTAL, PIRATE | [x]The Tavern always offers<br>1 extra Tavern spells.<br>Your Tavern spells<br>give an extra +1/+1. |
| `BG34_Giant_598` | Timewarped Mothership | 3 | 5/7 | MECH | <b>Avenge (4):</b> Get a random Protoss minion. |
| `BG34_Giant_207` | Timewarped Murcules | 3 | 10/5 | MURLOC | [x]<b>Divine Shield</b><br>Whenever this kills a minion,<br>give the left-most minion in<br>your hand +4/+4. |
| `BG34_Giant_074t` | Timewarped Nellie's Ship | 3 | 2/6 | BEAST, PIRATE | [x]At the start of each turn,<br><b>Discover</b> a Pirate to crew the<br>ship. <b>Deathrattle:</b> Summon<br> and get that Pirate. |
| `BG34_Giant_208` | Timewarped Pagle | 3 | 8/6 | PIRATE | [x]Once per combat, when this<br>attacks and kills a minion,<br>get a Triple Reward. |
| `BG34_Giant_211` | Timewarped Pashmar | 3 | 9/10 | NAGA | [x]<b>Avenge (3):</b> Get a random<br><b>Spellcraft</b> spell and<br>Tavern spell. |
| `BG34_Giant_204` | Timewarped Pillager | 3 | 4/3 | UNDEAD | [x]<b>Taunt</b>, <b>Reborn</b><br><b>Deathrattle:</b> Get a<br>Tavern Coin. |
| `BG34_Giant_069` | Timewarped Piper | 3 | 2/8 | QUILBOAR | [x]Whenever this takes damage,<br>your <b>Blood Gems</b> give an<br>extra +1 Attack this game.<br><i>({2} times per combat.)</i> |
| `BG34_Giant_580` | Timewarped Ragnaros | 3 | 8/8 | ELEMENTAL | <b>Start of Combat:</b> Deal this minion's Attack to the highest-Health enemy minion. |
| `BG34_Giant_082` | Timewarped Recycler | 3 | 2/7 | UNDEAD | <b>Avenge (4):</b> Increase your maximum Gold by (1). |
| `BG34_Giant_091` | Timewarped Red Whelp | 3 | 4/6 | DRAGON | [x]<b>Start of Combat:</b> Deal 3<br>damage to two random<br>enemy minions. <i>(Improves<br> after you play a Dragon!)</i> |
| `BG34_Giant_300` | Timewarped Rewinder | 3 | 5/4 | DEMON | [x]After your hero takes<br>damage, rewind it and<br>give your Demons<br>+{2} Health. |
| `BG34_Giant_589` | Timewarped Sailor | 3 | 5/4 | PIRATE | <b>Divine Shield</b><br>The Tavern offers an extra Pirate whenever it is <b>Refreshed</b>. |
| `BG34_Giant_304` | Timewarped Sapper | 3 | 10/6 | NAGA | [x]<b>Taunt</b><br><b>Deathrattle:</b> Get a<br>Spitescale Special. |
| `BG34_Giant_202` | Timewarped Saurolisk | 3 | 4/4 | BEAST | After you trigger a <b>Deathrattle</b>, gain +3/+2 permanently. |
| `BG34_Giant_017` | Timewarped Scourfin | 3 | 7/7 | MURLOC | [x]<b><b>Taunt</b>.</b> <b>Deathrattle:</b> Give a<br>random minion in your hand<br>+7/+7 and summon it for<br>this combat only. |
| `BG34_Giant_067` | Timewarped Sellemental | 3 | 8/8 | ELEMENTAL | [x]At the end of your turn,<br>get a Sellemental. |
| `BG34_Giant_209` | Timewarped Sensei | 3 | 6/6 | MECH | At the end of your turn, give adjacent Mechs +3/+3. |
| `BG34_Giant_360` | Timewarped Shadowdancer | 3 | 6/5 | DEMON | [x]<b>Taunt</b><br>At the end of your turn, cast<br>Staff of Enrichment. |
| `BG34_Giant_072` | Timewarped Skipper | 3 | 5/6 | MURLOC | [x]After you sell a Tier 2<br>minion, get a random<br>Tier 1 minion. |
| `BG34_Giant_586` | Timewarped Snow Elemental | 3 | 6/5 | ELEMENTAL | [x]The Tavern offers an extra<br><b>Frozen</b> Elemental<br> whenever it is <b>Refreshed</b>. |
| `BG34_Giant_582` | Timewarped Sporebat | 3 | 9/2 | BEAST | <b>Taunt</b><br><b>Deathrattle:</b> Get a random Tavern spell that costs (2) or more. |
| `BG34_Giant_078` | Timewarped Thorncaller | 3 | 5/3 | QUILBOAR | <b>Battlecry and Deathrattle:</b> Get a Blood Gem Barrage. |
| `BG34_Giant_604` | Timewarped Tipper | 3 | 6/8 | NONE | [x]If you have any unspent Gold<br>at the end of your turn,<br>increase your maximum<br>Gold by 1. |
| `BG34_Giant_605` | Timewarped Traveler | 3 | 4/8 | NONE | <b>Avenge (4):</b> Get a random 1-Cost card from the Minor <b>Timewarp</b>. |
| `BG34_Giant_585` | Timewarped Vaelastrasz | 3 | 6/6 | DRAGON | <b>Rally:</b> Get a random Dragon. |
| `BG34_Giant_064` | Timewarped Whelp Smuggler | 3 | 3/8 | NONE | [x]Whenever a friendly<br>minion gains Attack,<br>give it +{1} Health. |
| `BG34_Giant_039` | Timewarped Winner | 3 | 6/6 | NONE | [x]<b>Stealth</b><br>At the start of your turn, if this<br>minion survived last combat,<br>get a Triple Reward. |
| `BG34_Giant_671` | Timewarped Zerus | 3 | 6/6 | NONE | [x]Once per turn, choose from<br>2 Minor <b>Timewarped</b> minions<br>to transform into. Keep this<br>minion's stats. |
| `BG34_PreMadeChamp_083` | Timewarped Anub'arak | 5 | 12/8 | UNDEAD | After you play an Undead, your Undead have an extra +3 Attack this game. |
| `BG34_Giant_596` | Timewarped Archimonde | 5 | 5/5 | DEMON | [x]After your hero takes<br>damage, rewind it and<br>reduce the Cost of your next<br>Tavern spell by (1). |
| `BG34_Giant_801` | Timewarped Astrogill | 5 | 5/5 | MURLOC | While this is in your hand, after a different friendly Murloc gains stats, gain +3/+2. |
| `BG34_PreMadeChamp_078` | Timewarped Bandit | 5 | 7/13 | QUILBOAR | [x]At the start of your turn,<br>discard a spell for this to<br>play 4 <b>Blood Gems</b> on<br>all your minions. |
| `BG34_Giant_777` | Timewarped Behemoth | 5 | 9/11 | ELEMENTAL | [x]<b>Taunt</b><br>After you buy an Elemental,<br>gain its stats. |
| `BG34_PreMadeChamp_076` | Timewarped Bloodbinder | 5 | 12/8 | QUILBOAR | [x]At the start of your turn, get<br>5 <b>Blood Gems</b>. They also<br>count as Tavern spells. |
| `BG34_Giant_102` | Timewarped Bonker | 5 | 7/14 | QUILBOAR | [x]<b>Windfury</b><br><b>Rally:</b> This plays 2<br>permanent <b>Blood Gems</b> on<br> all your other minions. |
| `BG34_PreMadeChamp_091` | Timewarped Calligrapher | 5 | 12/14 | DEMON | [x]<b>Battlecry, Deathrattle,<br>and Rally:</b> Get a random<br>Tavern spell. |
| `BG34_Giant_618` | Timewarped Caretaker | 5 | 5/5 | UNDEAD | [x]<b>Deathrattle:</b> Summon five 1/1<br>Skeletons. Any that don't fit<br>give your Undead +1 Attack this<br> game <i>(wherever they are)</i>. |
| `BG34_PreMadeChamp_200` | Timewarped Centurion | 5 | 8/8 | DRAGON | [x]After you cast a Tavern spell,<br>get an extra copy of it.<br><i>(3 times per turn.)</i> |
| `BG34_Giant_042` | Timewarped Chameleon | 5 | 6/15 | BEAST | <b>Start of Combat:</b> Transform into a copy of the minion to the left of this. |
| `BG34_PreMadeChamp_090` | Timewarped Clefthoof | 5 | 1/10 | BEAST | [x]At the end of your turn,<br>give your Beasts +2/+2<br>and deal 1 damage to<br>them, three times. |
| `BG34_Giant_680` | Timewarped Collector | 5 | 12/12 | PIRATE | [x]Also damages adjacent<br>minions. <b>Rally:</b> If you control<br>4 Golden minions, gain<br><b>Divine Shield</b>. |
| `BG34_Giant_654` | Timewarped Deadstomper | 5 | 6/12 | UNDEAD, BEAST | [x]After you summon a minion,<br>give your minions<br>+4 Attack permanently. |
| `BG34_PreMadeChamp_020` | Timewarped Duskmaw | 5 | 6/14 | DRAGON | <b>Avenge (1):</b> Give your Dragons +6/+{2}. |
| `BG34_Giant_034` | Timewarped Geist | 5 | 10/6 | UNDEAD | <b>Deathrattle:</b> Your Tavern spells give an extra +2/+2 this game. |
| `BG34_Giant_644` | Timewarped Gemsplitter | 5 | 5/10 | QUILBOAR | [x]<b>Divine Shield</b>. After a friendly<br>minion loses <b>Divine Shield</b>,<br>your <b>Blood Gems</b> give an extra<br>+1 Attack this game. |
| `BG34_Giant_609` | Timewarped Ghoul-acabra | 5 | 6/15 | UNDEAD, BEAST | [x]After you trigger a<br><b>Deathrattle</b>, give your<br>minions +3/+2<br>permanently. |
| `BG34_Giant_035` | Timewarped Glowscale | 5 | 6/12 | NAGA | <b>Taunt</b><br><b>Spellcraft:</b> Give a minion <b>Divine Shield</b>. |
| `BG34_Giant_342` | Timewarped Hag | 5 | 6/8 | UNDEAD | [x]<b>Start of Combat:</b> Give the<br>Undead to the right <b>Reborn</b> and<br>"This is <b>Reborn</b> with full Health<br>and enchantments". |
| `BG34_Giant_370` | Timewarped Hawkstrider | 5 | 8/4 | BEAST | [x]<b>Start of Combat:</b> Trigger<br>all friendly <b>Deathrattles</b>. |
| `BG34_Giant_015` | Timewarped Hooktail | 5 | 6/12 | DRAGON, PIRATE | [x]Whenever you cast a<br>Tavern spell, give your<br>minions +2/+2. |
| `BG34_Giant_040` | Timewarped Ichoron | 5 | 7/4 | ELEMENTAL | [x]<b>Divine Shield</b><br>Whenever you play a minion,<br>give it <b>Divine Shield</b>. |
| `BG34_Giant_674` | Timewarped Icky Imp | 5 | 12/12 | DEMON | <b>Deathrattle:</b> Summon 2 Imps with this minion's maximum stats. |
| `BG34_Giant_597` | Timewarped Immortal | 5 | 8/8 | MECH | <b>Start of Combat:</b> Gain the stats of adjacent minions. |
| `BG34_PreMadeChamp_013` | Timewarped Imp-filtrator | 5 | 13/7 | DEMON | [x]After you spend {4} Gold,<br>give minions in the Tavern<br>+{2}/+{3} this game.<br><i>(8 Gold left!)</i> |
| `BG34_Giant_120` | Timewarped Interpreter | 5 | 6/8 | MECH | [x]Whenever you play or<br><b>Magnetize</b> a Mech, give<br>your Mechs +3/+3. |
| `BG34_PreMadeChamp_004` | Timewarped Jungle King | 5 | 8/12 | BEAST | [x]<b>Stealth</b><br>After you summon a Beast,<br>give it +4/+3. Improves<br>after you cast a spell. |
| `BG34_Giant_313` | Timewarped Kil'jaeden | 5 | 7/7 | DEMON | [x]The Tavern offers two extra<br>Demons with +7/+{0}<br>whenever it is <b><b>Refreshed</b>.</b><br> <i>(Upgrades each turn!)</i> |
| `BG34_Giant_678` | Timewarped Lava Lurker | 5 | 8/9 | NAGA | [x]After you cast a <b>Spellcraft</b><br>spell from hand on a minion,<br>also cast a permanent copy<br> on this. <i>(Twice per turn.)</i> |
| `BG34_Giant_608` | Timewarped Lil' Quilboar | 5 | 5/5 | QUILBOAR | [x]<b>Reborn</b><br><b>Deathrattle:</b> This plays 3<br><b>Blood Gems</b> on all your<br>Quilboar. |
| `BG34_Giant_683` | Timewarped Lucky Egg | 5 | 2/2 | NONE | [x]In two turns, choose from <br>three Golden Tier 7 minions <br>to transform into.<br><i>(2 turns left!)</i> |
| `BG34_Giant_006` | Timewarped Molten Rock | 5 | 7/7 | ELEMENTAL | After you play an Elemental, gain +1/+1 and improve this. |
| `BG34_Giant_321` | Timewarped Mrrrglr | 5 | 8/8 | MURLOC | [x]<b>Start of Combat:</b> Give<br>adjacent Murlocs the stats<br>of all the minions in<br>your hand. |
| `BG34_Giant_318` | Timewarped Murk-Eye | 5 | 12/5 | MURLOC | At the end of your turn, trigger all friendly <b>Battlecries</b>. |
| `BG34_Giant_206` | Timewarped Murky | 5 | 5/5 | MURLOC | [x]At the end of your turn,<br>gain +2/+2. <i>(Improved<br>by each <b>Battlecry</b> you've<br>triggered this game!)</i> |
| `BG34_Giant_320` | Timewarped Mystic | 5 | 6/6 | MURLOC | [x]After you sell 3 Murlocs,<br>your Tavern spells give an<br>extra +{2}/+{3} this game.<br><i>(3 left!)</i> |
| `BG34_Giant_684` | Timewarped Mythrax | 5 | 8/9 | ALL | <b>Start of Combat:</b> Gain the stats of 3 friendly minions of different types <i>(except Timewarped Mythrax)</i>. |
| `BG34_Giant_205` | Timewarped Nalaa | 5 | 12/12 | NONE | [x]Whenever you cast a spell,<br>give a friendly minion<br>of each type +4/+3. |
| `BG34_Giant_687` | Timewarped Nest Swarmer | 5 | 7/7 | BEAST | [x]<b>Battlecry, Deathrattle, and<br>Rally:</b> Your Beetles have<br>+{2}/+{3} this game. Summon<br>a 2/2 Beetle. |
| `BG34_Giant_309` | Timewarped Nine Frogs | 5 | 12/12 | BEAST | [x]After you buy a minion, get a<br>random Tavern spell from<br>the same Tier. <i>(9 left!)</i> |
| `BG34_Giant_032` | Timewarped Nomi | 5 | 10/10 | NONE | After you play an Elemental, give minions in the Tavern +4/+3 this game. |
| `BG34_PreMadeChamp_011` | Timewarped Overfiend | 5 | 7/13 | DEMON | [x]After you buy a card,<br>give your Demons<br>+4/+4. |
| `BG34_Giant_319` | Timewarped Painter | 5 | 4/9 | MURLOC | [x]At the end of your turn, give<br>adjacent minions +3/+2.<br>After you play a card from Tier 3<br>or below, improve this. |
| `BG34_Giant_327` | Timewarped Peggy | 5 | 9/5 | PIRATE | Whenever a card is added to your hand, give your Pirates +1/+1. |
| `BG34_Giant_322` | Timewarped Pioneer | 5 | 4/13 | NAGA | After you <b>Refresh</b> 3 times, get a random <b>Spellcraft</b> spell. <i>(3 left!)</i> |
| `BG34_PreMadeChamp_067` | Timewarped Plunderer | 5 | 15/5 | PIRATE | <b>Deathrattle:</b> Increase your maximum Gold by 2. |
| `BG34_Giant_314` | Timewarped Poet | 5 | 6/7 | DRAGON | [x]<b>Divine Shield</b><br>All your Dragons keep<br><b><b>Bonus Keyword</b>s</b> and stats<br>gained in combat. |
| `BG34_PreMadeChamp_022` | Timewarped Prismscale | 5 | 8/12 | DRAGON | <b>Avenge (2):</b> Get an Azerite Empowerment. |
| `BG34_Giant_088` | Timewarped Promo-Drake | 5 | 6/6 | DRAGON | [x]<b>Start of Combat:</b> Give<br>your minions +{3}/+{3}.<br>At the end of your turn,<br>improve this. |
| `BG34_Giant_330` | Timewarped Radio Star | 5 | 1/1 | UNDEAD | [x]<b>Deathrattle:</b> Get a copy of<br>the enemy minion that killed<br>this with full Health<br>and enchantments. |
| `BG34_PreMadeChamp_065` | Timewarped Raider | 5 | 14/6 | PIRATE | After you play a card from Tier 4 or above, give your Pirates +3/+2. |
| `BG34_Giant_325` | Timewarped Riplash | 5 | 13/5 | NAGA | <b>Deathrattle:</b> Get a copy of the last Tavern spell you cast. |
| `BG34_Giant_333` | Timewarped Scout | 5 | 7/7 | NONE | [x]When you sell this, get 1<br>random minions from<br>Tier 7. <i>(Improves<br>each turn!)</i> |
| `BG34_Giant_110` | Timewarped Sea Glass | 5 | 10/8 | ELEMENTAL | [x]<b>Divine Shield</b><br><b>Rally:</b> Double this minion's<br>stats. <i>(2 times per combat.)</i> |
| `BG34_Giant_323` | Timewarped Secretary | 5 | 5/11 | NAGA | [x]After you cast 2 <b>Spellcraft</b><br>spells, get a random<br>Tavern spell. <i>(2 left!)</i> |
| `BG34_Giant_311` | Timewarped Shivarra | 5 | 4/2 | DEMON | [x]Whenever a minion is<br>consumed, this gains<br>its stats. |
| `BG34_PreMadeChamp_058` | Timewarped Siren | 5 | 6/14 | NAGA | [x]After you play a Naga, give<br>all your Naga +6/+10. |
| `BG34_PreMadeChamp_049` | Timewarped Squallfin | 5 | 6/14 | MURLOC | [x]After you play a Murloc,<br>give minions in your hand<br>and board +2/+2. |
| `BG34_Giant_675` | Timewarped Stone Drake | 5 | 6/6 | DRAGON, ELEMENTAL | [x]<b>Start of Combat:</b> Gain the<br>stats of all the minions<br>you sold this turn.<br><i>(0/0)</i> |
| `BG34_PreMadeChamp_031` | Timewarped Stormcloud | 5 | 11/9 | ELEMENTAL | [x]<b>Deathrattle and Avenge (3):</b><br>Get a Tavern Tempest. |
| `BG34_PreMadeChamp_032` | Timewarped Substrate | 5 | 8/8 | ELEMENTAL | [x]<b>Divine Shield</b><br>At the end of your turn,<br>get a Temperature Shift. |
| `BG34_Giant_324` | Timewarped Summoner | 5 | 6/9 | NAGA, ELEMENTAL | <b>Spellcraft:</b> Choose a minion. Transform all minions in the Tavern into ones of its type, keeping Tiers. |
| `BG34_Giant_686` | Timewarped Swirler | 5 | 9/9 | ELEMENTAL | Your Elementals give an extra +3/+3 this game. |
| `BG34_Giant_595` | Timewarped Tamuzo | 5 | 5/5 | BEAST | [x]After you summon a<br>minion in combat,<br>double its stats. |
| `BG34_Giant_328` | Timewarped Tide Razor | 5 | 12/8 | NONE | <b>Deathrattle:</b> Summon and get 4 random Pirates. |
| `BG34_Giant_676` | Timewarped Trumpeter | 5 | 7/8 | ELEMENTAL | [x]After you sell 5<br>Elementals, get a random<br>Elemental. <i>(5 left!)</i> |
| `BG34_Giant_677` | Timewarped Wargear | 5 | 5/5 | MECH | <b>Magnetic</b><br>After you <b>Magnetize</b> this, double the target's stats. |
| `BG34_Giant_331` | Timewarped Warghoul | 5 | 9/3 | UNDEAD | [x]<b>Taunt.</b> <b>Deathrattle:</b> Trigger<br>an adjacent minion's<br><b>Deathrattle</b> <i>(except<br> Timewarped Warghoul)</i>. |
| `BG34_Giant_599` | Timewarped Whirl-O-Tron | 5 | 7/5 | MECH | [x]<b>Start of Combat:</b> Copy your<br>two left-most <b>Deathrattles</b><br><i>(except other<br>Whirl-O-Trons)</i>. |

## All Firestone Timewarped Minions

| Card ID | Name | Tier | Stats | Tribe | Text |
| --- | --- | ---: | --- | --- | --- |
| `BG34_Giant_591` | Timewarped Acolyte | 3 | 4/6 | MURLOC | At the start of your turn, spin the Wheel of<br>Yogg-Saron. |
| `BG34_Giant_009` | Timewarped Alleycat | 3 | 7/7 | BEAST | At the end of your turn, summon a Tabbycat with this minion's stats. |
| `BG34_Giant_007` | Timewarped Annoy-o-Tron | 3 | 6/6 | MECH | <b>Taunt</b><br><b>Divine Shield</b><br><b>Reborn</b> |
| `BG34_Giant_212` | Timewarped Archer | 3 | 4/9 | NAGA | <b>Spellcraft:</b> Give a minion +12 Attack. |
| `BG34_Giant_071` | Timewarped Bassgill | 3 | 7/4 | MURLOC | [x]<b>Deathrattle:</b> Summon the<br>highest-Health minion from<br>your hand and give it <b>Divine<br> Shield</b> for this combat only. |
| `BG34_Giant_201` | Timewarped Boar | 3 | 1/1 | BEAST | [x]Whenever every third friendly<br>Timewarped Boar dies, get<br>a random Golden Beast.<br><i>(0 left!)</i> |
| `BG34_Giant_594` | Timewarped Botani | 3 | 8/6 | NONE | At the end of your turn,<br>get a random minion<br>of your Tier. |
| `BG34_Giant_001` | Timewarped Busker | 3 | 4/2 | PIRATE | <b>Battlecry and Deathrattle:</b> Gain 1 Gold next turn. |
| `BG34_Giant_679` | Timewarped Chimera | 3 | 1/8 | ALL | Whenever this takes damage, give a friendly minion of each type +2/+1 permanently. |
| `BG34_Giant_210` | Timewarped Commander | 3 | 5/5 | NAGA | [x]<b>Spellcraft:</b> Give a minion<br>+2/+2 for each friendly<br>Naga. |
| `BG34_Giant_302` | Timewarped Copter | 3 | 4/6 | MECH | <b>Divine Shield</b><br><b>Avenge (3):</b> Get a<br>random Mech. |
| `BG34_Giant_012` | Timewarped Cyclone | 3 | 6/1 | ELEMENTAL | <b>Divine Shield</b><br> <b>Windfury</b><br> <b>Reborn</b> |
| `BG34_Giant_081` | Timewarped Deathswarmer | 3 | 1/9 | UNDEAD | [x]Whenever this takes<br>damage, your Undead have<br>+1 Attack this game<br><i>(wherever they are)</i>. |
| `BG34_Giant_583` | Timewarped Devourer | 3 | 5/5 | DEMON | [x]At the start of your turn,<br>consume the Demon to<br>the right to gain its stats<br>and 3 Gold. |
| `BG34_Giant_029` | Timewarped Dragonling | 3 | 3/3 | DRAGON | [x]<b>Start of Combat:</b> Give this<br>minion and its neighbors<br> stats equal to your Tier. |
| `BG34_Giant_038` | Timewarped Elise | 3 | 6/7 | NONE | After you <b>Refresh</b> 5 times, make the highest-Tier minion in the Tavern Golden. <i>(5 left!)</i> |
| `BG34_Giant_332` | Timewarped Embalmer | 3 | 10/10 | UNDEAD | [x]One minion you summon<br>each turn gains <b>Reborn</b>.<br><i>(1 left!)</i> |
| `BG34_Giant_590` | Timewarped Festergut | 3 | 7/3 | UNDEAD | [x]<b>Deathrattle:</b> Summon<br>and get a random<br>Undead Creation. |
| `BG34_Giant_305` | Timewarped Geomancer | 3 | 2/9 | QUILBOAR | <b>Avenge (5):</b> Get a<br><b>Blood Gem</b>. Your <b>Blood Gems</b> give an extra +1/+1 this game. |
| `BG34_Giant_041` | Timewarped Greenskeeper | 3 | 5/9 | DRAGON | [x]<b>Rally:</b> Trigger your<br>right-most <b>Battlecry</b> and<br><b>Deathrattle</b>. |
| `BG34_Giant_593` | Timewarped Henchman | 3 | 5/7 | NONE | [x]After you kill a second<br>minion each combat,<br>get a plain copy of it. |
| `BG34_Giant_581` | Timewarped Hyena | 3 | 3/5 | BEAST | Whenever a friendly Beast dies, gain +2/+2 permanently. |
| `BG34_Giant_306` | Timewarped Jazzer | 3 | 5/3 | QUILBOAR | [x]<b>Deathrattle:</b> Your<br><b>Blood Gems</b> give an extra<br>+{1} Health this game. |
| `BG34_Giant_584` | Timewarped Kil'rek | 3 | 4/7 | DEMON | <b>Taunt</b><br><b>Deathrattle:</b> Get a<br>random Demon. |
| `BG34_Giant_031` | Timewarped Leapfrogger | 3 | 3/3 | BEAST | [x]<b>Taunt</b>, <b>Reborn</b><br><b>Deathrattle:</b> Give a friendly<br>Beast +1/+1 and<br>this <b>Deathrattle</b>. |
| `BG34_Giant_602` | Timewarped Lei | 3 | 3/5 | NONE | [x]At the start of your turn,<br>get the <b>Buddy</b> of your<br>Hero Power. |
| `BG34_Giant_066` | Timewarped Lubber | 3 | 5/7 | ELEMENTAL, PIRATE | [x]The Tavern always offers<br>1 extra Tavern spells.<br>Your Tavern spells<br>give an extra +1/+1. |
| `BG34_Giant_598` | Timewarped Mothership | 3 | 5/7 | MECH | <b>Avenge (4):</b> Get a random Protoss minion. |
| `BG34_Giant_207` | Timewarped Murcules | 3 | 10/5 | MURLOC | [x]<b>Divine Shield</b><br>Whenever this kills a minion,<br>give the left-most minion in<br>your hand +4/+4. |
| `BG34_Giant_074t` | Timewarped Nellie's Ship | 3 | 2/6 | BEAST, PIRATE | [x]At the start of each turn,<br><b>Discover</b> a Pirate to crew the<br>ship. <b>Deathrattle:</b> Summon<br> and get that Pirate. |
| `BG34_Giant_208` | Timewarped Pagle | 3 | 8/6 | PIRATE | [x]Once per combat, when this<br>attacks and kills a minion,<br>get a Triple Reward. |
| `BG34_Giant_211` | Timewarped Pashmar | 3 | 9/10 | NAGA | [x]<b>Avenge (3):</b> Get a random<br><b>Spellcraft</b> spell and<br>Tavern spell. |
| `BG34_Giant_204` | Timewarped Pillager | 3 | 4/3 | UNDEAD | [x]<b>Taunt</b>, <b>Reborn</b><br><b>Deathrattle:</b> Get a<br>Tavern Coin. |
| `BG34_Giant_069` | Timewarped Piper | 3 | 2/8 | QUILBOAR | [x]Whenever this takes damage,<br>your <b>Blood Gems</b> give an<br>extra +1 Attack this game.<br><i>({2} times per combat.)</i> |
| `BG34_Giant_580` | Timewarped Ragnaros | 3 | 8/8 | ELEMENTAL | <b>Start of Combat:</b> Deal this minion's Attack to the highest-Health enemy minion. |
| `BG34_Giant_082` | Timewarped Recycler | 3 | 2/7 | UNDEAD | <b>Avenge (4):</b> Increase your maximum Gold by (1). |
| `BG34_Giant_091` | Timewarped Red Whelp | 3 | 4/6 | DRAGON | [x]<b>Start of Combat:</b> Deal 3<br>damage to two random<br>enemy minions. <i>(Improves<br> after you play a Dragon!)</i> |
| `BG34_Giant_300` | Timewarped Rewinder | 3 | 5/4 | DEMON | [x]After your hero takes<br>damage, rewind it and<br>give your Demons<br>+{2} Health. |
| `BG34_Giant_589` | Timewarped Sailor | 3 | 5/4 | PIRATE | <b>Divine Shield</b><br>The Tavern offers an extra Pirate whenever it is <b>Refreshed</b>. |
| `BG34_Giant_304` | Timewarped Sapper | 3 | 10/6 | NAGA | [x]<b>Taunt</b><br><b>Deathrattle:</b> Get a<br>Spitescale Special. |
| `BG34_Giant_202` | Timewarped Saurolisk | 3 | 4/4 | BEAST | After you trigger a <b>Deathrattle</b>, gain +3/+2 permanently. |
| `BG34_Giant_017` | Timewarped Scourfin | 3 | 7/7 | MURLOC | [x]<b><b>Taunt</b>.</b> <b>Deathrattle:</b> Give a<br>random minion in your hand<br>+7/+7 and summon it for<br>this combat only. |
| `BG34_Giant_067` | Timewarped Sellemental | 3 | 8/8 | ELEMENTAL | [x]At the end of your turn,<br>get a Sellemental. |
| `BG34_Giant_209` | Timewarped Sensei | 3 | 6/6 | MECH | At the end of your turn, give adjacent Mechs +3/+3. |
| `BG34_Giant_360` | Timewarped Shadowdancer | 3 | 6/5 | DEMON | [x]<b>Taunt</b><br>At the end of your turn, cast<br>Staff of Enrichment. |
| `BG34_Giant_072` | Timewarped Skipper | 3 | 5/6 | MURLOC | [x]After you sell a Tier 2<br>minion, get a random<br>Tier 1 minion. |
| `BG34_Giant_586` | Timewarped Snow Elemental | 3 | 6/5 | ELEMENTAL | [x]The Tavern offers an extra<br><b>Frozen</b> Elemental<br> whenever it is <b>Refreshed</b>. |
| `BG34_Giant_582` | Timewarped Sporebat | 3 | 9/2 | BEAST | <b>Taunt</b><br><b>Deathrattle:</b> Get a random Tavern spell that costs (2) or more. |
| `BG34_Giant_078` | Timewarped Thorncaller | 3 | 5/3 | QUILBOAR | <b>Battlecry and Deathrattle:</b> Get a Blood Gem Barrage. |
| `BG34_Giant_604` | Timewarped Tipper | 3 | 6/8 | NONE | [x]If you have any unspent Gold<br>at the end of your turn,<br>increase your maximum<br>Gold by 1. |
| `BG34_Giant_605` | Timewarped Traveler | 3 | 4/8 | NONE | <b>Avenge (4):</b> Get a random 1-Cost card from the Minor <b>Timewarp</b>. |
| `BG34_Giant_585` | Timewarped Vaelastrasz | 3 | 6/6 | DRAGON | <b>Rally:</b> Get a random Dragon. |
| `BG34_Giant_064` | Timewarped Whelp Smuggler | 3 | 3/8 | NONE | [x]Whenever a friendly<br>minion gains Attack,<br>give it +{1} Health. |
| `BG34_Giant_039` | Timewarped Winner | 3 | 6/6 | NONE | [x]<b>Stealth</b><br>At the start of your turn, if this<br>minion survived last combat,<br>get a Triple Reward. |
| `BG34_Giant_671` | Timewarped Zerus | 3 | 6/6 | NONE | [x]Once per turn, choose from<br>2 Minor <b>Timewarped</b> minions<br>to transform into. Keep this<br>minion's stats. |
| `BG34_PreMadeChamp_083` | Timewarped Anub'arak | 5 | 12/8 | UNDEAD | After you play an Undead, your Undead have an extra +3 Attack this game. |
| `BG34_Giant_596` | Timewarped Archimonde | 5 | 5/5 | DEMON | [x]After your hero takes<br>damage, rewind it and<br>reduce the Cost of your next<br>Tavern spell by (1). |
| `BG34_Giant_801` | Timewarped Astrogill | 5 | 5/5 | MURLOC | While this is in your hand, after a different friendly Murloc gains stats, gain +3/+2. |
| `BG34_PreMadeChamp_078` | Timewarped Bandit | 5 | 7/13 | QUILBOAR | [x]At the start of your turn,<br>discard a spell for this to<br>play 4 <b>Blood Gems</b> on<br>all your minions. |
| `BG34_Giant_777` | Timewarped Behemoth | 5 | 9/11 | ELEMENTAL | [x]<b>Taunt</b><br>After you buy an Elemental,<br>gain its stats. |
| `BG34_PreMadeChamp_076` | Timewarped Bloodbinder | 5 | 12/8 | QUILBOAR | [x]At the start of your turn, get<br>5 <b>Blood Gems</b>. They also<br>count as Tavern spells. |
| `BG34_Giant_102` | Timewarped Bonker | 5 | 7/14 | QUILBOAR | [x]<b>Windfury</b><br><b>Rally:</b> This plays 2<br>permanent <b>Blood Gems</b> on<br> all your other minions. |
| `BG34_PreMadeChamp_091` | Timewarped Calligrapher | 5 | 12/14 | DEMON | [x]<b>Battlecry, Deathrattle,<br>and Rally:</b> Get a random<br>Tavern spell. |
| `BG34_Giant_618` | Timewarped Caretaker | 5 | 5/5 | UNDEAD | [x]<b>Deathrattle:</b> Summon five 1/1<br>Skeletons. Any that don't fit<br>give your Undead +1 Attack this<br> game <i>(wherever they are)</i>. |
| `BG34_PreMadeChamp_200` | Timewarped Centurion | 5 | 8/8 | DRAGON | [x]After you cast a Tavern spell,<br>get an extra copy of it.<br><i>(3 times per turn.)</i> |
| `BG34_Giant_042` | Timewarped Chameleon | 5 | 6/15 | BEAST | <b>Start of Combat:</b> Transform into a copy of the minion to the left of this. |
| `BG34_PreMadeChamp_090` | Timewarped Clefthoof | 5 | 1/10 | BEAST | [x]At the end of your turn,<br>give your Beasts +2/+2<br>and deal 1 damage to<br>them, three times. |
| `BG34_Giant_680` | Timewarped Collector | 5 | 12/12 | PIRATE | [x]Also damages adjacent<br>minions. <b>Rally:</b> If you control<br>4 Golden minions, gain<br><b>Divine Shield</b>. |
| `BG34_Giant_654` | Timewarped Deadstomper | 5 | 6/12 | UNDEAD, BEAST | [x]After you summon a minion,<br>give your minions<br>+4 Attack permanently. |
| `BG34_PreMadeChamp_020` | Timewarped Duskmaw | 5 | 6/14 | DRAGON | <b>Avenge (1):</b> Give your Dragons +6/+{2}. |
| `BG34_Giant_034` | Timewarped Geist | 5 | 10/6 | UNDEAD | <b>Deathrattle:</b> Your Tavern spells give an extra +2/+2 this game. |
| `BG34_Giant_644` | Timewarped Gemsplitter | 5 | 5/10 | QUILBOAR | [x]<b>Divine Shield</b>. After a friendly<br>minion loses <b>Divine Shield</b>,<br>your <b>Blood Gems</b> give an extra<br>+1 Attack this game. |
| `BG34_Giant_609` | Timewarped Ghoul-acabra | 5 | 6/15 | UNDEAD, BEAST | [x]After you trigger a<br><b>Deathrattle</b>, give your<br>minions +3/+2<br>permanently. |
| `BG34_Giant_035` | Timewarped Glowscale | 5 | 6/12 | NAGA | <b>Taunt</b><br><b>Spellcraft:</b> Give a minion <b>Divine Shield</b>. |
| `BG34_Giant_342` | Timewarped Hag | 5 | 6/8 | UNDEAD | [x]<b>Start of Combat:</b> Give the<br>Undead to the right <b>Reborn</b> and<br>"This is <b>Reborn</b> with full Health<br>and enchantments". |
| `BG34_Giant_370` | Timewarped Hawkstrider | 5 | 8/4 | BEAST | [x]<b>Start of Combat:</b> Trigger<br>all friendly <b>Deathrattles</b>. |
| `BG34_Giant_015` | Timewarped Hooktail | 5 | 6/12 | DRAGON, PIRATE | [x]Whenever you cast a<br>Tavern spell, give your<br>minions +2/+2. |
| `BG34_Giant_040` | Timewarped Ichoron | 5 | 7/4 | ELEMENTAL | [x]<b>Divine Shield</b><br>Whenever you play a minion,<br>give it <b>Divine Shield</b>. |
| `BG34_Giant_674` | Timewarped Icky Imp | 5 | 12/12 | DEMON | <b>Deathrattle:</b> Summon 2 Imps with this minion's maximum stats. |
| `BG34_Giant_597` | Timewarped Immortal | 5 | 8/8 | MECH | <b>Start of Combat:</b> Gain the stats of adjacent minions. |
| `BG34_PreMadeChamp_013` | Timewarped Imp-filtrator | 5 | 13/7 | DEMON | [x]After you spend {4} Gold,<br>give minions in the Tavern<br>+{2}/+{3} this game.<br><i>(8 Gold left!)</i> |
| `BG34_Giant_120` | Timewarped Interpreter | 5 | 6/8 | MECH | [x]Whenever you play or<br><b>Magnetize</b> a Mech, give<br>your Mechs +3/+3. |
| `BG34_PreMadeChamp_004` | Timewarped Jungle King | 5 | 8/12 | BEAST | [x]<b>Stealth</b><br>After you summon a Beast,<br>give it +4/+3. Improves<br>after you cast a spell. |
| `BG34_Giant_313` | Timewarped Kil'jaeden | 5 | 7/7 | DEMON | [x]The Tavern offers two extra<br>Demons with +7/+{0}<br>whenever it is <b><b>Refreshed</b>.</b><br> <i>(Upgrades each turn!)</i> |
| `BG34_Giant_678` | Timewarped Lava Lurker | 5 | 8/9 | NAGA | [x]After you cast a <b>Spellcraft</b><br>spell from hand on a minion,<br>also cast a permanent copy<br> on this. <i>(Twice per turn.)</i> |
| `BG34_Giant_608` | Timewarped Lil' Quilboar | 5 | 5/5 | QUILBOAR | [x]<b>Reborn</b><br><b>Deathrattle:</b> This plays 3<br><b>Blood Gems</b> on all your<br>Quilboar. |
| `BG34_Giant_683` | Timewarped Lucky Egg | 5 | 2/2 | NONE | [x]In two turns, choose from <br>three Golden Tier 7 minions <br>to transform into.<br><i>(2 turns left!)</i> |
| `BG34_Giant_006` | Timewarped Molten Rock | 5 | 7/7 | ELEMENTAL | After you play an Elemental, gain +1/+1 and improve this. |
| `BG34_Giant_321` | Timewarped Mrrrglr | 5 | 8/8 | MURLOC | [x]<b>Start of Combat:</b> Give<br>adjacent Murlocs the stats<br>of all the minions in<br>your hand. |
| `BG34_Giant_318` | Timewarped Murk-Eye | 5 | 12/5 | MURLOC | At the end of your turn, trigger all friendly <b>Battlecries</b>. |
| `BG34_Giant_206` | Timewarped Murky | 5 | 5/5 | MURLOC | [x]At the end of your turn,<br>gain +2/+2. <i>(Improved<br>by each <b>Battlecry</b> you've<br>triggered this game!)</i> |
| `BG34_Giant_320` | Timewarped Mystic | 5 | 6/6 | MURLOC | [x]After you sell 3 Murlocs,<br>your Tavern spells give an<br>extra +{2}/+{3} this game.<br><i>(3 left!)</i> |
| `BG34_Giant_684` | Timewarped Mythrax | 5 | 8/9 | ALL | <b>Start of Combat:</b> Gain the stats of 3 friendly minions of different types <i>(except Timewarped Mythrax)</i>. |
| `BG34_Giant_205` | Timewarped Nalaa | 5 | 12/12 | NONE | [x]Whenever you cast a spell,<br>give a friendly minion<br>of each type +4/+3. |
| `BG34_Giant_687` | Timewarped Nest Swarmer | 5 | 7/7 | BEAST | [x]<b>Battlecry, Deathrattle, and<br>Rally:</b> Your Beetles have<br>+{2}/+{3} this game. Summon<br>a 2/2 Beetle. |
| `BG34_Giant_309` | Timewarped Nine Frogs | 5 | 12/12 | BEAST | [x]After you buy a minion, get a<br>random Tavern spell from<br>the same Tier. <i>(9 left!)</i> |
| `BG34_Giant_032` | Timewarped Nomi | 5 | 10/10 | NONE | After you play an Elemental, give minions in the Tavern +4/+3 this game. |
| `BG34_PreMadeChamp_011` | Timewarped Overfiend | 5 | 7/13 | DEMON | [x]After you buy a card,<br>give your Demons<br>+4/+4. |
| `BG34_Giant_319` | Timewarped Painter | 5 | 4/9 | MURLOC | [x]At the end of your turn, give<br>adjacent minions +3/+2.<br>After you play a card from Tier 3<br>or below, improve this. |
| `BG34_Giant_327` | Timewarped Peggy | 5 | 9/5 | PIRATE | Whenever a card is added to your hand, give your Pirates +1/+1. |
| `BG34_Giant_322` | Timewarped Pioneer | 5 | 4/13 | NAGA | After you <b>Refresh</b> 3 times, get a random <b>Spellcraft</b> spell. <i>(3 left!)</i> |
| `BG34_PreMadeChamp_067` | Timewarped Plunderer | 5 | 15/5 | PIRATE | <b>Deathrattle:</b> Increase your maximum Gold by 2. |
| `BG34_Giant_314` | Timewarped Poet | 5 | 6/7 | DRAGON | [x]<b>Divine Shield</b><br>All your Dragons keep<br><b><b>Bonus Keyword</b>s</b> and stats<br>gained in combat. |
| `BG34_PreMadeChamp_022` | Timewarped Prismscale | 5 | 8/12 | DRAGON | <b>Avenge (2):</b> Get an Azerite Empowerment. |
| `BG34_Giant_088` | Timewarped Promo-Drake | 5 | 6/6 | DRAGON | [x]<b>Start of Combat:</b> Give<br>your minions +{3}/+{3}.<br>At the end of your turn,<br>improve this. |
| `BG34_Giant_330` | Timewarped Radio Star | 5 | 1/1 | UNDEAD | [x]<b>Deathrattle:</b> Get a copy of<br>the enemy minion that killed<br>this with full Health<br>and enchantments. |
| `BG34_PreMadeChamp_065` | Timewarped Raider | 5 | 14/6 | PIRATE | After you play a card from Tier 4 or above, give your Pirates +3/+2. |
| `BG34_Giant_325` | Timewarped Riplash | 5 | 13/5 | NAGA | <b>Deathrattle:</b> Get a copy of the last Tavern spell you cast. |
| `BG34_Giant_333` | Timewarped Scout | 5 | 7/7 | NONE | [x]When you sell this, get 1<br>random minions from<br>Tier 7. <i>(Improves<br>each turn!)</i> |
| `BG34_Giant_110` | Timewarped Sea Glass | 5 | 10/8 | ELEMENTAL | [x]<b>Divine Shield</b><br><b>Rally:</b> Double this minion's<br>stats. <i>(2 times per combat.)</i> |
| `BG34_Giant_323` | Timewarped Secretary | 5 | 5/11 | NAGA | [x]After you cast 2 <b>Spellcraft</b><br>spells, get a random<br>Tavern spell. <i>(2 left!)</i> |
| `BG34_Giant_311` | Timewarped Shivarra | 5 | 4/2 | DEMON | [x]Whenever a minion is<br>consumed, this gains<br>its stats. |
| `BG34_PreMadeChamp_058` | Timewarped Siren | 5 | 6/14 | NAGA | [x]After you play a Naga, give<br>all your Naga +6/+10. |
| `BG34_PreMadeChamp_049` | Timewarped Squallfin | 5 | 6/14 | MURLOC | [x]After you play a Murloc,<br>give minions in your hand<br>and board +2/+2. |
| `BG34_Giant_675` | Timewarped Stone Drake | 5 | 6/6 | DRAGON, ELEMENTAL | [x]<b>Start of Combat:</b> Gain the<br>stats of all the minions<br>you sold this turn.<br><i>(0/0)</i> |
| `BG34_PreMadeChamp_031` | Timewarped Stormcloud | 5 | 11/9 | ELEMENTAL | [x]<b>Deathrattle and Avenge (3):</b><br>Get a Tavern Tempest. |
| `BG34_PreMadeChamp_032` | Timewarped Substrate | 5 | 8/8 | ELEMENTAL | [x]<b>Divine Shield</b><br>At the end of your turn,<br>get a Temperature Shift. |
| `BG34_Giant_324` | Timewarped Summoner | 5 | 6/9 | NAGA, ELEMENTAL | <b>Spellcraft:</b> Choose a minion. Transform all minions in the Tavern into ones of its type, keeping Tiers. |
| `BG34_Giant_686` | Timewarped Swirler | 5 | 9/9 | ELEMENTAL | Your Elementals give an extra +3/+3 this game. |
| `BG34_Giant_595` | Timewarped Tamuzo | 5 | 5/5 | BEAST | [x]After you summon a<br>minion in combat,<br>double its stats. |
| `BG34_Giant_328` | Timewarped Tide Razor | 5 | 12/8 | NONE | <b>Deathrattle:</b> Summon and get 4 random Pirates. |
| `BG34_Giant_676` | Timewarped Trumpeter | 5 | 7/8 | ELEMENTAL | [x]After you sell 5<br>Elementals, get a random<br>Elemental. <i>(5 left!)</i> |
| `BG34_Giant_677` | Timewarped Wargear | 5 | 5/5 | MECH | <b>Magnetic</b><br>After you <b>Magnetize</b> this, double the target's stats. |
| `BG34_Giant_331` | Timewarped Warghoul | 5 | 9/3 | UNDEAD | [x]<b>Taunt.</b> <b>Deathrattle:</b> Trigger<br>an adjacent minion's<br><b>Deathrattle</b> <i>(except<br> Timewarped Warghoul)</i>. |
| `BG34_Giant_599` | Timewarped Whirl-O-Tron | 5 | 7/5 | MECH | [x]<b>Start of Combat:</b> Copy your<br>two left-most <b>Deathrattles</b><br><i>(except other<br>Whirl-O-Trons)</i>. |
| `BG34_Giant_336` | Timewarped Amalgam | 0 | 7/9 | ALL | After you play a minion,<br>give minions of its type in<br>the Tavern +4/+4 this game. |
| `BG34_Giant_027` | Timewarped Arm | 0 | 8/8 | NONE | [x]Whenever a friendly<br>minion is attacked, give it<br>+8 Attack permanently. |
| `BG34_Giant_104` | Timewarped Bristler | 0 | 6/6 | QUILBOAR | [x]<b>Deathrattle:</b> Give this<br>minion's <b>Blood Gems</b> to<br>2 different friendly<br>Quilboar. |
| `BG34_Giant_376` | Timewarped Deios | 0 | 6/10 | NONE | Your <b>Battlecries</b>, <b>Deathrattles</b>, and <b>Rallies</b> trigger twice. |
| `BG34_Giant_610` | Timewarped Electron | 0 | 9/9 | MECH | [x]After you cast 2 Tavern<br>spells, <b>Magnetize</b> a {2}/{3}<br>Satellite to all your Mechs.<br><i>(2 left!)</i> |
| `BG34_Giant_310` | Timewarped Elegist | 0 | 3/5 | MURLOC | [x]At the end of your turn,<br>give minions in your hand<br>and board +2/+1. |
| `BG34_Giant_317` | Timewarped Expeditioner | 0 | 6/12 | MURLOC | [x]<b>Taunt</b>, <b>Divine Shield</b>.<br>After this gains stats, also give<br>the stats to the two left-most<br>minions in your hand. |
| `BG34_Giant_362` | Timewarped Goldrinn | 0 | 6/6 | BEAST | [x]<b>Deathrattle:</b> Your Beasts<br>have +4/+4 this game<br><i>(wherever they are)</i>. |
| `BG34_Giant_656` | Timewarped Grease Bot | 0 | 6/12 | MECH | [x]<b>Divine Shield</b>. After a friendly<br>minion loses <b>Divine Shield</b>,<br>give your minions +3/+3<br>permanently. |
| `BG34_Giant_068` | Timewarped Guard | 0 | 5/10 | MECH | [x]<b>Divine Shield</b><br><b>Rally:</b> Give a different<br>friendly minion <b>Divine<br>Shield</b> permanently. |
| `BG34_Giant_588` | Timewarped Hunter | 0 | 8/5 | MECH | [x]<b>Battlecry and Deathrattle:</b><br>Get a Pointy Arrow. |
| `BG34_Giant_024` | Timewarped Jelly Belly | 0 | 5/6 | UNDEAD | [x]After a friendly minion is<br><b>Reborn</b>, give your minions<br>+2/+2 permanently. |
| `BG34_PreMadeChamp_056` | Timewarped Karathress | 0 | 14/6 | NAGA | After you summon a minion in combat, get a copy of Deep Blues. |
| `BG34_PreMadeChamp_002` | Timewarped Lab Rat | 0 | 12/8 | BEAST | After you cast a spell, give your Beasts +2/+2. |
| `BG34_Giant_065` | Timewarped Low-Flier | 0 | 10/10 | DRAGON | [x]At the end of your turn, give<br>+2 Attack to your minions<br>with less Attack than this.<br>Repeat with Health. |
| `BG34_Giant_619` | Timewarped Magnanimoose | 0 | 8/2 | BEAST | [x]<b>Deathrattle:</b> Summon and<br>get a minion from a random<br>opponent's warband. |
| `BG34_PreMadeChamp_047` | Timewarped Paleofin | 0 | 2/18 | MURLOC | [x]At the end of your turn,<br>get a Cloning Conch. |
| `BG34_Giant_121` | Timewarped Probius | 0 | 12/7 | MECH | [x]<b>Magnetic</b><br>After you <b>Magnetize</b> this to<br> a Mech, make it Golden. |
| `BG34_Giant_002` | Timewarped Relaxer | 0 | 3/4 | QUILBOAR | [x]After you sell a Quilboar,<br>this plays 4 <b>Blood Gems</b> on<br>a random friendly minion. |
| `BG34_Giant_008` | Timewarped Seer | 0 | 8/8 | DEMON, NAGA | Two Tavern spells each turn cost (2) less. <i>(2 left!)</i> |
| `BG34_Giant_681` | Timewarped Shadequill | 0 | 7/11 | QUILBOAR | [x]At the end of your turn,<br>gain the stats of the 3<br>highest-Health minions<br>in the Tavern. |
| `BG34_Giant_592` | Timewarped Spirit of Air | 0 | 5/3 | ELEMENTAL | [x]<b>Deathrattle:</b> Give a random<br>friendly minion <b>Windfury</b>,<br> <b>Divine Shield</b>, and <b>Taunt</b>. |
| `BG34_PreMadeChamp_038` | Timewarped Steamer | 0 | 13/7 | MECH | At the end of your turn, get one of each <b>Magnetic</b> Volumizer. |
| `BG34_Giant_601` | Timewarped Stoneshell | 0 | 4/8 | NONE | [x]<b>Start of Combat:</b> Copy all<br>friendly <b>Rallies</b> <i>(except<br>other Stoneshells)</i>. |
| `BG34_Giant_021` | Timewarped Sylvar | 0 | 7/10 | PIRATE | [x]At the end of your turn, give<br>adjacent minions +8/+8.<br>Repeat for each friendly<br>Golden minion. |
| `BG34_Giant_603` | Timewarped Tender | 0 | 7/5 | NONE | [x]At the end of your turn,<br>get 2 random Tavern<br>spells that give stats. |
| `BG34_Giant_335` | Timewarped Theotar | 0 | 8/8 | ALL | [x]After you play a minion with<br>no type, give a friendly<br>minion of each type<br>+6/+6. |
| `BG34_Giant_326` | Timewarped Tony | 0 | 12/6 | PIRATE | <b>Deathrattle:</b> Get a copy of Eyes of the Earth Mother. |
| `BG34_Giant_010` | Timewarped Trickster | 0 | 8/8 | DEMON | [x]<b>Deathrattle:</b> Give this<br>minion's maximum stats to<br>another friendly minion. |
| `BG34_Giant_105` | Timewarped Twirler | 0 | 7/5 | QUILBOAR | After you play a <b>Blood Gem</b> on this, cast Blood Gem Barrage. |
| `BG34_Treasure_994` | Timewarped Ultralisk | 0 | 8/8 | NONE | [x]Also damages adjacent<br>minions. <b>Start of Combat:</b><br>Double this minion's<br>stats. |
| `BG34_Giant_361` | Timewarped Upstart | 0 | 4/7 | ELEMENTAL | [x]After the Tavern is<br><b>Refreshed</b>, double the<br>Health of its right-most<br>minion. |
| `BG34_Treasure_990` | Timewarped Viper | 0 | 8/8 | NONE | <b>Venomous<br>Immune</b> while attacking. |
