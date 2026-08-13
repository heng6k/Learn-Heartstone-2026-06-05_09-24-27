const COMMON_ALLOWED_ACTIONS = [
  'BuyMinion',
  'SellMinion',
  'PlayMinion',
  'MoveMinion',
  'MoveBoardMinion',
  'UseHeroPower',
  'UseRecruitAction',
  'ChooseDiscover',
  'ChooseMechanicOption',
  'UseGuideShapingSpell',
  'BeginNextTurnTransition',
  'ContinueNextTurnTransition'
]

function asset(stableId, name, imagePath, golden) {
  return {
    stableId,
    kind: 'Minion',
    name,
    imagePath,
    golden: Boolean(golden),
    badge: golden ? '金色' : ''
  }
}

function compatibility() {
  return {
    scenarioSchemaVersion: 3,
    mechanicStateSchemaVersion: 2,
    gameVersionId: '36.2-preview',
    rulesetId: 'ruleset-36.2-preview-v1',
    rulesetRevision: 1,
    contentSnapshotId: 'embedded:0.1.0-alpha',
    contentFingerprint: '713af57590b0006478f58e712c867531b19095a681833b09e11a4b9ee8d17238',
    status: 'Compatible',
    diagnostics: []
  }
}

function handoff(shareCode) {
  return {
    webPlayUrl: 'https://learn-hearthstone.example/play?scene=' + shareCode,
    shareUrl: 'https://learn-hearthstone.example/scenes/' + shareCode,
    windowsDownloadUrl: 'https://learn-hearthstone.example/download'
  }
}

function objectives() {
  return {
    requireFinalComposition: true,
    requireCombatWin: true,
    postWinChoices: ['FreeExplore', 'Restart', 'Return']
  }
}

function undo() {
  return {
    usesPerRun: 1,
    restoreRng: true,
    lockAfterTurnEnd: true,
    lockAfterCombat: true,
    lockDuringFreeExplore: true
  }
}

function contract(options) {
  return {
    schemaVersion: 1,
    sceneId: options.guideId + ':showcase',
    revisionId: options.revisionId,
    shareCode: options.shareCode,
    status: 'Published',
    contentHash: options.contentHash,
    summary: {
      title: options.title,
      summary: options.summary,
      archetype: options.archetype,
      difficulty: 'Showcase',
      difficultyTitle: '简单模式',
      gameVersionId: '36.2-preview',
      gameVersionName: '36.2',
      hero: options.hero,
      activeTribes: options.activeTribes,
      finalComposition: options.finalComposition
    },
    compatibility: compatibility(),
    content: {
      allowedActions: COMMON_ALLOWED_ACTIONS.slice(),
      objectives: objectives(),
      steps: options.steps,
      hints: ['固定种子仅用于复现本训练场景。'],
      discoveryRules: [],
      undo: undo()
    },
    handoff: handoff(options.shareCode)
  }
}

const items = [
  {
    popularity: 98,
    updatedAt: '2026-08-09T08:00:00Z',
    contract: contract({
      guideId: 'GUIDE-S14-BEAST-LOBSTER-RALLY',
      revisionId: 'GUIDE-S14-BEAST-LOBSTER-RALLY@9c5f2d77a8b1e640',
      shareCode: '23456789ABCDEFGHJKMN',
      contentHash: '9770eb6b58a95c3f06084fd2b223b201275082cc8b75c4860b0a25ceb15d520e',
      title: '龙虾进击亡语',
      summary: '用复生保护龙虾，以进击召唤和左侧亡语串起完整野兽阵容。',
      archetype: 'DeathrattleSummonChain',
      hero: {
        stableId: 'TB_BaconShop_HERO_95',
        kind: 'Hero',
        name: '格雷布',
        imagePath: 'HeroBuddyImages/heroes/TB_BaconShop_HERO_95',
        golden: false,
        badge: null
      },
      activeTribes: ['野兽', '机械', '恶魔', '纳迦', '野猪人'],
      finalComposition: [
        asset('BG36_202', '美味龙虾', 'CardImages/Minions/Season14/BG36_202', false),
        asset('BG36_210', '囤食土狼', 'CardImages/Minions/Season14/BG36_210', true),
        asset('BG36_208', '逐亡陆行鸟', 'CardImages/Minions/Season14/BG36_208', true),
        asset('BG36_211', '啮笼鼠', 'CardImages/Minions/Season14/BG36_211', false),
        asset('BG36_207', '狼宝宝', 'CardImages/Minions/Season14/BG36_207', true),
        asset('BG36_208', '逐亡陆行鸟', 'CardImages/Minions/Season14/BG36_208', true),
        asset('BG36_209', '暴虐巨蝎', 'CardImages/Minions/Season14/BG36_209', true)
      ],
      steps: [
        { order: 1, actionId: 'buy-scarab', kind: 'Buy', count: 1, instruction: '购买机变甲虫', sourcePlacementId: 'beast-scarab', sourcePlacementIds: [], targetPlacementId: null, choiceId: null },
        { order: 2, actionId: 'play-scarab', kind: 'Play', count: 1, instruction: '打出机变甲虫，为最左美味龙虾选择复生', sourcePlacementId: 'beast-scarab', sourcePlacementIds: [], targetPlacementId: 'beast-lobster', choiceId: 'Reborn' },
        { order: 3, actionId: 'sell-scarab', kind: 'Sell', count: 1, instruction: '卖出完成教学的机变甲虫', sourcePlacementId: 'beast-scarab', sourcePlacementIds: [], targetPlacementId: null, choiceId: null },
        { order: 4, actionId: 'play-finishers', kind: 'PlayFinalCards', count: 3, instruction: '打出两张逐亡陆行鸟和暴虐巨蝎', sourcePlacementId: null, sourcePlacementIds: ['beast-deathstrider-a', 'beast-deathstrider-b', 'beast-scorpion'], targetPlacementId: null, choiceId: null },
        { order: 5, actionId: 'keep-lobster-left', kind: 'BoardOrder', count: 1, instruction: '保持美味龙虾位于战队最左侧', sourcePlacementId: null, sourcePlacementIds: [], targetPlacementId: 'beast-lobster', choiceId: 'LeftMost' }
      ]
    })
  },
  {
    popularity: 87,
    updatedAt: '2026-08-08T10:30:00Z',
    contract: contract({
      guideId: 'GUIDE-S14-MECH-SPELL-SATELLITE',
      revisionId: 'GUIDE-S14-MECH-SPELL-SATELLITE@2ce081766cca9c9f',
      shareCode: '3456789ABCDEFGHJKLMN',
      contentHash: '58f4f12e058923ed6280d0f350764120e479fa85c9c91a680a937ee08c3ad5c0',
      title: '法术磁力卫星',
      summary: '用酒馆法术推动机械成长，再以发动和磁力卫星完成阵容。',
      archetype: 'SpellEconomyGrowth',
      hero: {
        stableId: 'BG31_HERO_006',
        kind: 'Hero',
        name: 'Exarch Othaar',
        imagePath: 'HeroBuddyImages/heroes/BG31_HERO_006',
        golden: false,
        badge: null
      },
      activeTribes: ['机械', '鱼人', '恶魔', '元素', '龙'],
      finalComposition: [
        asset('BG36_506', '复映无人机', 'CardImages/Minions/Season14/BG36_506', true),
        asset('BG36_851', '火花破坏机', 'CardImages/Minions/Season14/BG36_851', true),
        asset('BG36_853', '炫彩机器人', 'CardImages/Minions/Season14/BG36_853', true),
        asset('BG36_853', '炫彩机器人', 'CardImages/Minions/Season14/BG36_853', true),
        asset('BG36_764', '机鳍鱼人', 'CardImages/Minions/Season14/BG36_764', true),
        asset('BG36_854', '救援机器人', 'CardImages/Minions/Season14/BG36_854', false),
        asset('BG36_854', '救援机器人', 'CardImages/Minions/Season14/BG36_854', false)
      ],
      steps: [
        { order: 1, actionId: 'play-drone', kind: 'Play', count: 1, instruction: '打出复映无人机', sourcePlacementId: 'mech-drone', sourcePlacementIds: [], targetPlacementId: null, choiceId: null },
        { order: 2, actionId: 'play-glambot', kind: 'Play', count: 1, instruction: '打出一张炫彩机器人', sourcePlacementId: 'mech-glambot-a', sourcePlacementIds: [], targetPlacementId: null, choiceId: null },
        { order: 3, actionId: 'activate-drone', kind: 'Activate', count: 1, instruction: '发动复映无人机', sourcePlacementId: 'mech-drone', sourcePlacementIds: [], targetPlacementId: null, choiceId: null },
        { order: 4, actionId: 'cast-repair', kind: 'Cast', count: 1, instruction: '对复映无人机施放维修作业', sourcePlacementId: 'mech-repair-job', sourcePlacementIds: [], targetPlacementId: 'mech-drone', choiceId: null },
        { order: 5, actionId: 'cast-second-spell', kind: 'Cast', count: 1, instruction: '对炫彩机器人施放防御者的仪式', sourcePlacementId: 'mech-defenders-rites', sourcePlacementIds: [], targetPlacementId: 'mech-glambot-a', choiceId: null }
      ]
    })
  },
  {
    popularity: 82,
    updatedAt: '2026-08-07T09:00:00Z',
    contract: contract({
      guideId: 'GUIDE-S14-DEMON-TAVERN-CONSUME',
      revisionId: 'GUIDE-S14-DEMON-TAVERN-CONSUME@8d47a95e1302da4b',
      shareCode: '456789ABCDEFGHJKLMNP',
      contentHash: 'e35ce6b214afaff8e8385ce7afabd4194012e59085e8fc86033beb8fb4979b5a',
      title: '酒馆成长吞噬',
      summary: '强化酒馆中的恶魔，利用吞噬和发动把酒馆属性转移到战队。',
      archetype: 'StatGrowth',
      hero: {
        stableId: 'BG20_HERO_301',
        kind: 'Hero',
        name: '吞噬者穆坦努斯',
        imagePath: 'HeroBuddyImages/heroes/BG20_HERO_301',
        golden: false,
        badge: null
      },
      activeTribes: ['恶魔', '野猪人', '野兽', '亡灵', '海盗'],
      finalComposition: [
        asset('BG36_503', '缚魂狱卒', 'CardImages/Minions/Season14/BG36_503', true),
        asset('BG36_762', '恶魔干扰者', 'CardImages/Minions/Season14/BG36_762', true),
        asset('BG36_762', '恶魔干扰者', 'CardImages/Minions/Season14/BG36_762', true),
        asset('BG36_621', '灵巧的逃亡者', 'CardImages/Minions/Season14/BG36_621', true),
        asset('BG36_731', '鬼影幻术师', 'CardImages/Minions/Season14/BG36_731', true),
        asset('BG36_733', '艾瑞达逃脱大师', 'CardImages/Minions/Season14/BG36_733', true),
        asset('BG36_730', '受困的钟舌恶魔', 'CardImages/Minions/Season14/BG36_730', false)
      ],
      steps: [
        { order: 1, actionId: 'cast-on-distractor', kind: 'Cast', count: 1, instruction: '对恶魔干扰者施放理性癫狂', sourcePlacementId: 'demon-methodical-madness', sourcePlacementIds: [], targetPlacementId: 'demon-distractor-a', choiceId: null },
        { order: 2, actionId: 'cast-on-tavern', kind: 'Cast', count: 1, instruction: '对酒馆恶魔施放尖利箭矢并触发吞噬', sourcePlacementId: 'demon-arrow', sourcePlacementIds: [], targetPlacementId: 'demon-shop-food-a', choiceId: null },
        { order: 3, actionId: 'activate-jailer', kind: 'Activate', count: 1, instruction: '发动缚魂狱卒', sourcePlacementId: 'demon-jailer', sourcePlacementIds: [], targetPlacementId: null, choiceId: null },
        { order: 4, actionId: 'play-demon-finishers', kind: 'PlayFinalCards', count: 4, instruction: '打出手牌中的四张成型恶魔', sourcePlacementId: null, sourcePlacementIds: ['demon-distractor-b', 'demon-deserter', 'demon-illusionist', 'demon-eredar'], targetPlacementId: null, choiceId: null }
      ]
    })
  }
]

const byCode = {}
for (const item of items) {
  byCode[item.contract.shareCode] = item
}

module.exports = {
  byCode,
  items
}
