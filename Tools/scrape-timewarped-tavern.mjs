import { mkdir, writeFile } from 'node:fs/promises';
import { createWriteStream } from 'node:fs';
import { dirname, join } from 'node:path';
import { pipeline } from 'node:stream/promises';

const root = process.cwd();
const outDir = join(root, 'Docs', 'research', 'timewarped-tavern');
const currentImageDir = join(outDir, 'images-current');
const allImageDir = join(outDir, 'images-all');
const historicalExtraImageDir = join(outDir, 'images-historical-extra');

const FIRESTONE_CARDS_URL = 'https://static.firestoneapp.com/data/cards/cards_enUS.gz.json';
const FIRESTONE_ZH_CN_CARDS_URL = 'https://static.firestoneapp.com/data/cards/cards_zhCN.gz.json';
const HEARTHSTONEJSON_URL = 'https://api.hearthstonejson.com/v1/latest/enUS/cards.json';
const CARD_ART_BASE_URL = 'https://static.zerotoheroes.com/hearthstone/cardart/256x/';

async function fetchJson(url) {
  const response = await fetch(url);
  if (!response.ok) {
    throw new Error(`${url} returned ${response.status} ${response.statusText}`);
  }

  return response.json();
}

async function downloadFile(url, destination) {
  const response = await fetch(url);
  if (!response.ok || response.body == null) {
    throw new Error(`${url} returned ${response.status} ${response.statusText}`);
  }

  await mkdir(dirname(destination), { recursive: true });
  await pipeline(response.body, createWriteStream(destination));
}

function hasMechanic(card, mechanic) {
  return Array.isArray(card.mechanics) && card.mechanics.includes(mechanic);
}

function isNormalTimewarpedMinion(card) {
  return card.type === 'Minion' &&
    !card.premium &&
    hasMechanic(card, 'BACON_TIMEWARPED');
}

function normalizeRaces(card) {
  const races = Array.isArray(card.races) ? card.races : [];
  if (races.length > 0) {
    return races;
  }

  return card.race ? [card.race] : [];
}

function mapKeywords(card) {
  const mechanics = Array.isArray(card.mechanics) ? card.mechanics : [];
  return mechanics
    .filter((value) => value !== 'BACON_TIMEWARPED')
    .filter((value) => !value.startsWith('TAG_'));
}

function stripCardText(value) {
  return String(value ?? '')
    .replace(/<br\s*\/?>/gi, '\n')
    .replace(/<\/?[^>]+>/g, '')
    .replace(/\[x\]/gi, '')
    .replace(/\s+/g, ' ')
    .trim();
}

function hasText(card, pattern) {
  return pattern.test(stripCardText(card.text));
}

function addUnique(values, value) {
  if (!values.includes(value)) {
    values.push(value);
  }
}

function inferTriggerTimings(card) {
  const timings = [];
  const keywords = card.keywords ?? [];
  const text = card.text ?? '';

  if (keywords.includes('BATTLECRY') || hasText(card, /\bBattlecry\b/i)) {
    addUnique(timings, 'battlecry');
  }

  if (keywords.includes('DEATHRATTLE') || hasText(card, /\bDeathrattle\b/i)) {
    addUnique(timings, 'deathrattle');
  }

  if (keywords.includes('BACON_RALLY') || hasText(card, /\bRally\b/i)) {
    addUnique(timings, 'rally');
  }

  if (keywords.includes('AVENGE') || hasText(card, /\bAvenge\b/i)) {
    addUnique(timings, 'avenge');
  }

  if (keywords.includes('START_OF_COMBAT') || hasText(card, /Start of Combat/i)) {
    addUnique(timings, 'start_of_combat');
  }

  if (keywords.includes('END_OF_TURN') || keywords.includes('END_OF_TURN_TRIGGER') || hasText(card, /At the end of your turn/i)) {
    addUnique(timings, 'end_of_turn');
  }

  if (hasText(card, /At the start of your turn|At the start of each turn/i)) {
    addUnique(timings, 'start_of_turn');
  }

  if (hasText(card, /Whenever this takes damage|After your hero takes damage|Whenever a friendly minion is attacked|After a friendly minion is Reborn/i)) {
    addUnique(timings, 'damage_reactive');
  }

  if (hasText(card, /After you play|Whenever you play|After you summon|After you cast|Whenever you cast|After you buy|After you sell|When you sell|After you Refresh|After the Tavern is Refreshed/i)) {
    addUnique(timings, 'recruit_phase_reactive');
  }

  if (hasText(card, /while this is in your hand|in your hand/i)) {
    addUnique(timings, 'hand_state');
  }

  if (hasText(card, /attacks and kills|kills a minion|After you kill/i)) {
    addUnique(timings, 'combat_kill');
  }

  if (keywords.includes('BACON_SPELLCRAFT_ID') || hasText(card, /\bSpellcraft\b/i)) {
    addUnique(timings, 'spellcraft');
  }

  if (timings.length === 0) {
    addUnique(timings, 'static_or_aura');
  }

  return timings;
}

function inferEffectFamilies(card) {
  const families = [];
  const text = card.text ?? '';

  if (/Gold|Tavern Coin|maximum Gold|Chronum/i.test(text)) {
    addUnique(families, 'economy');
  }

  if (/Tavern|Refresh|Refreshed|Frozen/i.test(text)) {
    addUnique(families, 'shop_or_refresh');
  }

  if (/\+\d|\+\{|\bstats\b|Attack|Health/i.test(text)) {
    addUnique(families, 'stats');
  }

  if (/Divine Shield|Reborn|Taunt|Venomous|Stealth|Windfury|Magnetic/i.test(text)) {
    addUnique(families, 'keyword_grant_or_keyword_body');
  }

  if (/Summon|summon/i.test(text)) {
    addUnique(families, 'summon');
  }

  if (/Get |get |Discover|Fill your hand|copy|copies|card from|spell/i.test(text)) {
    addUnique(families, 'card_generation');
  }

  if (/Tavern spell|Tavern spells/i.test(text)) {
    addUnique(families, 'tavern_spell_synergy');
  }

  if (/Blood Gem|Blood Gems/i.test(text)) {
    addUnique(families, 'blood_gem');
  }

  if (/Spellcraft/i.test(text)) {
    addUnique(families, 'spellcraft');
  }

  if (/Transform|transform/i.test(text)) {
    addUnique(families, 'transform');
  }

  if (/exact copy|plain copy|copy of|copies of/i.test(text)) {
    addUnique(families, 'copy');
  }

  if (/Hero Power|Buddy/i.test(text)) {
    addUnique(families, 'hero_or_buddy');
  }

  if (/damage|damages|deal/i.test(text)) {
    addUnique(families, 'damage');
  }

  if (/Beast|Murloc|Mech|Demon|Dragon|Pirate|Elemental|Quilboar|Undead|Naga/i.test(text)) {
    addUnique(families, 'tribe_synergy');
  }

  if (/for this combat only|combat only/i.test(text)) {
    addUnique(families, 'combat_only');
  }

  if (families.length === 0) {
    addUnique(families, 'special');
  }

  return families;
}

function inferImplementationNotes(card) {
  const notes = [];
  const text = card.text ?? '';

  if (card.tavernTier === 0) {
    notes.push('非当前池条目，先作为历史/上线版本候选，不默认进入当前 Timewarped Tavern。');
  }

  if (/last game/i.test(text)) {
    notes.push('涉及跨局历史数据，第一版建议代理或暂缓。');
  }

  if (/Chronum/i.test(text)) {
    notes.push('涉及 Timewarped Tavern 独立货币，需要接入 Chronum 状态。');
  }

  if (/Tavern|Refresh|Refreshed|Frozen/i.test(text)) {
    notes.push('需要挂到特殊酒馆或普通刷新事件，注意不要误触发普通酒馆效果。');
  }

  if (/exact copy|plain copy|copy of|copies of|Transform|transform/i.test(text)) {
    notes.push('需要生成新 InstanceId，避免复用源实例。');
  }

  if (/for this combat only|combat only/i.test(text)) {
    notes.push('需要战斗临时召唤/临时增益清理。');
  }

  if (/Blood Gem|Tavern spell|Spellcraft/i.test(text)) {
    notes.push('需要复用现有鲜血宝石、酒馆法术或塑造法术管线。');
  }

  if (/Start of Combat|Deathrattle|Avenge|Rally|Battlecry/i.test(text)) {
    notes.push('可接入现有关键字触发分发。');
  }

  return notes.length === 0 ? ['按牌面文本实现，无额外跨系统依赖。'] : notes;
}

function attachMechanism(card) {
  return {
    ...card,
    plainText: stripCardText(card.text),
    mechanism: {
      poolStatus: card.inCurrentFirestonePool ? 'current' : 'historical_extra',
      triggerTimings: inferTriggerTimings(card),
      effectFamilies: inferEffectFamilies(card),
      implementationNotes: inferImplementationNotes(card),
    },
  };
}

function toResearchCard(card, zhCardByDbfId, goldenByNormalDbfId) {
  const zh = zhCardByDbfId.get(card.dbfId);
  const golden = goldenByNormalDbfId.get(card.dbfId);
  return {
    cardId: card.id,
    dbfId: card.dbfId,
    name: card.name,
    zhName: zh?.name ?? null,
    tavernTier: card.techLevel ?? 0,
    timewarpKind: card.techLevel === 3 ? 'minor' : card.techLevel === 5 ? 'major' : 'unknown',
    attack: card.attack ?? 0,
    health: card.health ?? 0,
    tribes: normalizeRaces(card),
    keywords: mapKeywords(card),
    text: card.text ?? '',
    zhText: zh?.text ?? null,
    inCurrentFirestonePool: card.isBaconPool === true,
    costInTimewarpedTavern: card.cost ?? null,
    goldenCardId: golden?.id ?? null,
    goldenDbfId: golden?.dbfId ?? card.battlegroundsPremiumDbfId ?? null,
    imageUrl: `${CARD_ART_BASE_URL}${card.id}.jpg`,
  };
}

function summarize(cards) {
  const byTier = {};
  const byKind = {};
  const byTribe = {};
  for (const card of cards) {
    byTier[card.tavernTier] = (byTier[card.tavernTier] ?? 0) + 1;
    byKind[card.timewarpKind] = (byKind[card.timewarpKind] ?? 0) + 1;
    for (const tribe of card.tribes.length > 0 ? card.tribes : ['NONE']) {
      byTribe[tribe] = (byTribe[tribe] ?? 0) + 1;
    }
  }

  return { total: cards.length, byTier, byKind, byTribe };
}

function markdownTable(cards) {
  const rows = [
    '| Card ID | Name | Tier | Stats | Tribe | Text |',
    '| --- | --- | ---: | --- | --- | --- |',
  ];
  for (const card of cards) {
    const text = (card.text || '')
      .replaceAll('\n', '<br>')
      .replaceAll('|', '\\|');
    rows.push(`| \`${card.cardId}\` | ${card.name} | ${card.tavernTier} | ${card.attack}/${card.health} | ${card.tribes.join(', ') || 'NONE'} | ${text} |`);
  }

  return rows.join('\n');
}

function mechanismMarkdown(cards, title) {
  const rows = [`# ${title}`, ''];
  for (const card of cards) {
    rows.push(`## ${card.name} (${card.cardId})`);
    rows.push('');
    rows.push(`- 状态: ${card.mechanism.poolStatus === 'current' ? '当前 Firestone 池' : '历史/上线版本额外池'}`);
    rows.push(`- 分档: ${card.timewarpKind} / techLevel ${card.tavernTier}`);
    rows.push(`- 成本: ${card.costInTimewarpedTavern ?? '未知'}`);
    rows.push(`- 身材: ${card.attack}/${card.health}`);
    rows.push(`- 种族: ${card.tribes.join(', ') || 'NONE'}`);
    rows.push(`- 触发时机: ${card.mechanism.triggerTimings.join(', ')}`);
    rows.push(`- 效果类别: ${card.mechanism.effectFamilies.join(', ')}`);
    rows.push(`- 机制文本: ${card.plainText || '(无文本)'}`);
    if (card.zhText) {
      rows.push(`- 中文文本: ${stripCardText(card.zhText)}`);
    }
    rows.push(`- 实现备注: ${card.mechanism.implementationNotes.join('；')}`);
    rows.push('');
  }

  return rows.join('\n');
}

async function main() {
  await mkdir(outDir, { recursive: true });
  await mkdir(currentImageDir, { recursive: true });
  await mkdir(allImageDir, { recursive: true });
  await mkdir(historicalExtraImageDir, { recursive: true });

  const [firestoneCards, hearthstoneJsonCards] = await Promise.all([
    fetchJson(FIRESTONE_CARDS_URL),
    fetchJson(HEARTHSTONEJSON_URL),
  ]);

  let zhCards = [];
  try {
    zhCards = await fetchJson(FIRESTONE_ZH_CN_CARDS_URL);
  } catch (error) {
    console.warn(`Could not fetch zhCN Firestone cards: ${error.message}`);
  }

  const zhCardByDbfId = new Map(zhCards.map((card) => [card.dbfId, card]));
  const goldenByNormalDbfId = new Map(
    firestoneCards
      .filter((card) => card.premium && card.battlegroundsNormalDbfId)
      .map((card) => [card.battlegroundsNormalDbfId, card])
  );

  const allTimewarpedMinions = firestoneCards
    .filter(isNormalTimewarpedMinion)
    .map((card) => toResearchCard(card, zhCardByDbfId, goldenByNormalDbfId))
    .map(attachMechanism)
    .sort((a, b) => {
      if (a.inCurrentFirestonePool !== b.inCurrentFirestonePool) {
        return a.inCurrentFirestonePool ? -1 : 1;
      }

      return a.tavernTier - b.tavernTier || a.name.localeCompare(b.name);
    });

  const currentFirestoneMinions = allTimewarpedMinions
    .filter((card) => card.inCurrentFirestonePool);
  const historicalExtraMinions = allTimewarpedMinions
    .filter((card) => !card.inCurrentFirestonePool);

  const hearthstoneJsonTimewarped = hearthstoneJsonCards
    .filter((card) => card.type === 'MINION')
    .filter((card) => !String(card.id).endsWith('_G'))
    .filter((card) => String(card.name ?? '').includes('Timewarped') || String(card.text ?? '').includes('Timewarped'))
    .map((card) => ({
      cardId: card.id,
      dbfId: card.dbfId,
      name: card.name,
      tavernTier: card.techLevel ?? 0,
      attack: card.attack ?? 0,
      health: card.health ?? 0,
      race: card.race ?? null,
      races: card.races ?? null,
      text: card.text ?? null,
      imageUrl: `${CARD_ART_BASE_URL}${card.id}.jpg`,
    }))
    .sort((a, b) => a.tavernTier - b.tavernTier || a.name.localeCompare(b.name));

  const payload = {
    generatedAt: new Date().toISOString(),
    sources: {
      firestoneCards: FIRESTONE_CARDS_URL,
      firestoneZhCnCards: FIRESTONE_ZH_CN_CARDS_URL,
      hearthstoneJson: HEARTHSTONEJSON_URL,
      imageBase: CARD_ART_BASE_URL,
    },
    filter: 'Firestone cards where type == Minion, premium != true, mechanics includes BACON_TIMEWARPED. Current pool additionally requires isBaconPool == true.',
    currentFirestoneSummary: summarize(currentFirestoneMinions),
    allFirestoneTimewarpedSummary: summarize(allTimewarpedMinions),
    hearthstoneJsonNameSummary: {
      total: hearthstoneJsonTimewarped.length,
    },
    currentFirestoneMinions,
    allFirestoneTimewarpedMinions: allTimewarpedMinions,
    historicalExtraFirestoneMinions: historicalExtraMinions,
    hearthstoneJsonTimewarpedMinions: hearthstoneJsonTimewarped,
  };

  await writeFile(
    join(outDir, 'timewarped-tavern-research.json'),
    `${JSON.stringify(payload, null, 2)}\n`,
    'utf8'
  );

  const currentRows = markdownTable(currentFirestoneMinions);
  const allRows = markdownTable(allTimewarpedMinions);
  const report = `# Timewarped Tavern Data Research

Generated: ${payload.generatedAt}

## Sources

- Firestone static card data: ${FIRESTONE_CARDS_URL}
- Firestone zhCN static card data: ${FIRESTONE_ZH_CN_CARDS_URL}
- HearthstoneJSON fallback/search data: ${HEARTHSTONEJSON_URL}
- Card art URL pattern: ${CARD_ART_BASE_URL}{cardId}.jpg

## Filter

Current Firestone list:
\`type == "Minion" && premium != true && mechanics includes "BACON_TIMEWARPED" && isBaconPool == true\`.

All Firestone Timewarped minions:
\`type == "Minion" && premium != true && mechanics includes "BACON_TIMEWARPED"\`.

## Counts

- Current Firestone pool: ${currentFirestoneMinions.length}
- All Firestone Timewarped minions in static card data: ${allTimewarpedMinions.length}
- HearthstoneJSON name/text fallback hits: ${hearthstoneJsonTimewarped.length}

## Current Firestone Timewarped Minions

${currentRows}

## All Firestone Timewarped Minions

${allRows}
`;

  await writeFile(join(outDir, 'timewarped-tavern-research.md'), report, 'utf8');
  await writeFile(
    join(outDir, 'timewarped-minion-mechanisms.json'),
    `${JSON.stringify({
      generatedAt: payload.generatedAt,
      filter: payload.filter,
      count: allTimewarpedMinions.length,
      currentCount: currentFirestoneMinions.length,
      historicalExtraCount: historicalExtraMinions.length,
      minions: allTimewarpedMinions,
    }, null, 2)}\n`,
    'utf8'
  );
  await writeFile(
    join(outDir, 'timewarped-minion-mechanisms.md'),
    mechanismMarkdown(allTimewarpedMinions, 'Timewarped Minion Mechanisms'),
    'utf8'
  );

  const imageFailures = {
    current: [],
    all: [],
    historicalExtra: [],
  };
  for (const card of currentFirestoneMinions) {
    try {
      await downloadFile(card.imageUrl, join(currentImageDir, `${card.cardId}.jpg`));
    } catch (error) {
      imageFailures.current.push({ cardId: card.cardId, imageUrl: card.imageUrl, error: error.message });
    }
  }

  for (const card of allTimewarpedMinions) {
    try {
      await downloadFile(card.imageUrl, join(allImageDir, `${card.cardId}.jpg`));
    } catch (error) {
      imageFailures.all.push({ cardId: card.cardId, imageUrl: card.imageUrl, error: error.message });
    }
  }

  for (const card of historicalExtraMinions) {
    try {
      await downloadFile(card.imageUrl, join(historicalExtraImageDir, `${card.cardId}.jpg`));
    } catch (error) {
      imageFailures.historicalExtra.push({ cardId: card.cardId, imageUrl: card.imageUrl, error: error.message });
    }
  }

  await writeFile(
    join(outDir, 'image-download-failures.json'),
    `${JSON.stringify(imageFailures, null, 2)}\n`,
    'utf8'
  );

  console.log(`Current Firestone Timewarped minions: ${currentFirestoneMinions.length}`);
  console.log(`All Firestone Timewarped minions: ${allTimewarpedMinions.length}`);
  console.log(`Historical extra Firestone Timewarped minions: ${historicalExtraMinions.length}`);
  console.log(`HearthstoneJSON fallback hits: ${hearthstoneJsonTimewarped.length}`);
  console.log(`Current images downloaded: ${currentFirestoneMinions.length - imageFailures.current.length}`);
  console.log(`All images downloaded: ${allTimewarpedMinions.length - imageFailures.all.length}`);
  console.log(`Historical extra images downloaded: ${historicalExtraMinions.length - imageFailures.historicalExtra.length}`);
  console.log(`Image failures: ${imageFailures.current.length + imageFailures.all.length + imageFailures.historicalExtra.length}`);
  console.log(`Wrote ${join(outDir, 'timewarped-tavern-research.md')}`);
  console.log(`Wrote ${join(outDir, 'timewarped-minion-mechanisms.md')}`);
}

main().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
