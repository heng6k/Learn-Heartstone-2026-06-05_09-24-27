#!/usr/bin/env python3
"""Project the authoritative strategy-guide JSON into the native Mini Program.

This is a release adapter only: it joins existing guide/catalog data and makes
small local thumbnails. It does not reproduce Unity gameplay or validation.
"""

from __future__ import annotations

import argparse
import json
import re
from pathlib import Path

from PIL import Image, ImageOps


TRIBE_NAMES = {
    "Beast": "野兽",
    "Murloc": "鱼人",
    "Mech": "机械",
    "Demon": "恶魔",
    "Dragon": "龙",
    "Pirate": "海盗",
    "Elemental": "元素",
    "Naga": "纳迦",
    "Quilboar": "野猪人",
    "Undead": "亡灵",
}

TUTORIAL_GLYPHS = {
    "GUIDE_SHAPING_BATTLECRY": "吼",
    "GUIDE_SHAPING_DEATHRATTLE": "骷",
    "GUIDE_SHAPING_END_OF_TURN": "末",
}


def load_json(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


def by_key(items: list[dict], *keys: str) -> dict[str, dict]:
    result: dict[str, dict] = {}
    for item in items:
        for key in keys:
            value = item.get(key)
            if value is not None and str(value).strip():
                result[str(value).lower()] = item
    return result


def safe_file_name(value: str) -> str:
    return re.sub(r"[^A-Za-z0-9._-]+", "-", value).strip("-") or "card"


def find_image(resources: Path, image_path: str, identifier: str) -> Path | None:
    candidates = []
    if image_path:
        candidates.append(resources / image_path)
    # Older catalog rows predate imagePath, but their canonical assets live here.
    candidates.append(resources / "CardImages" / identifier)
    for stem in candidates:
        for extension in (".png", ".jpg", ".jpeg", ".webp"):
            candidate = Path(f"{stem}{extension}")
            if candidate.is_file():
                return candidate
    return None


def make_thumbnail(source: Path, target: Path) -> None:
    with Image.open(source) as raw:
        rgba = raw.convert("RGBA")
        fitted = ImageOps.contain(rgba, (176, 232), Image.Resampling.LANCZOS)
        canvas = Image.new("RGB", fitted.size, (16, 24, 22))
        canvas.paste(fitted, mask=fitted.getchannel("A"))
        target.parent.mkdir(parents=True, exist_ok=True)
        canvas.save(target, "JPEG", quality=72, optimize=True, progressive=True)


class Projector:
    def __init__(self, repository: Path, asset_outputs: list[Path]) -> None:
        self.repository = repository
        self.resources = repository / "Assets" / "LearnHearthstone" / "Resources"
        data = self.resources / "Data"

        minion_payload = load_json(data / "battlegroundsMinions.json")
        spell_payload = load_json(data / "battlegroundsSpells.json")
        hero_payload = load_json(data / "battlegroundsHeroes.json")
        trinket_payload = load_json(data / "battlegroundsTrinkets.json")
        hero_localization = load_json(data / "battlegroundsHeroLocalizationZhCN.json")
        trinket_localization = load_json(data / "battlegroundsTrinketLocalizationZhCN.json")

        self.minions = by_key(minion_payload["minions"], "cardId")
        self.spells = by_key(spell_payload["spells"], "cardNumber", "cardId", "id")
        self.heroes = by_key(hero_payload["heroes"], "heroCardId")
        self.trinkets = by_key(trinket_payload["trinkets"], "cardId", "id")
        self.hero_names = {
            item["cardId"].lower(): item["name"]
            for item in hero_localization["cards"]
            if item.get("cardId") and item.get("name")
        }
        self.trinket_names = {
            item["cardId"].lower(): item["name"]
            for item in trinket_localization["cards"]
            if item.get("cardId") and item.get("name")
        }
        self.asset_outputs = asset_outputs
        self.generated_images: set[str] = set()
        self.missing_images: list[str] = []

    def project_asset(self, kind: str, identifier: str, golden: bool = False) -> dict:
        normalized_kind = (kind or "Minion").lower()
        lookup_key = (identifier or "").lower()

        if normalized_kind == "hero":
            definition = self.heroes.get(lookup_key)
            source_kind = "英雄"
            name = self.hero_names.get(lookup_key) or (definition or {}).get("name")
            image_path = (definition or {}).get("imagePath", "")
            attack = health = tavern_tier = cost = 0
        elif normalized_kind in ("trinket", "lessertrinket", "greatertrinket"):
            definition = self.trinkets.get(lookup_key)
            source_kind = "饰品"
            name = self.trinket_names.get(lookup_key) or (definition or {}).get("name")
            image_path = (definition or {}).get("imagePath", "")
            attack = health = tavern_tier = 0
            cost = int((definition or {}).get("cost") or 0)
        elif normalized_kind in ("spell", "tavernspell"):
            definition = self.spells.get(lookup_key)
            source_kind = "教学法术" if identifier in TUTORIAL_GLYPHS else "酒馆法术"
            name = (definition or {}).get("name")
            image_path = (definition or {}).get("imagePath", "")
            attack = health = 0
            tavern_tier = int((definition or {}).get("tavernTier") or 0)
            cost = int((definition or {}).get("cost") or 0)
        else:
            definition = self.minions.get(lookup_key)
            source_kind = "随从"
            name = (definition or {}).get("name")
            image_path = (definition or {}).get("imagePath", "")
            stats = (definition or {}).get("golden") if golden else definition
            stats = stats or definition or {}
            attack = int(stats.get("attack") or 0)
            health = int(stats.get("health") or 0)
            tavern_tier = int((definition or {}).get("tavernTier") or 0)
            cost = 0

        if definition is None:
            raise ValueError(f"Unknown {kind} card in strategy guide: {identifier}")

        source = find_image(self.resources, image_path, identifier)
        tutorial = identifier in TUTORIAL_GLYPHS
        image_url = ""
        art_type = "tutorial" if tutorial else "image"
        if source is not None:
            file_name = f"{safe_file_name(identifier)}.jpg"
            if file_name not in self.generated_images:
                for asset_output in self.asset_outputs:
                    make_thumbnail(source, asset_output / file_name)
                self.generated_images.add(file_name)
            image_url = f"/assets/cards/{file_name}"
        elif not tutorial:
            art_type = "missing"
            self.missing_images.append(f"{identifier} -> {image_path}")

        badge = "金色" if golden else ""
        if source_kind == "饰品":
            slot_kind = (definition or {}).get("slotKind", "")
            badge = "大型" if slot_kind == "Greater" else "小型"

        return {
            "stableId": identifier,
            "kind": source_kind,
            "name": name or identifier,
            "image": image_url,
            "artType": art_type,
            "tutorialGlyph": TUTORIAL_GLYPHS.get(identifier, ""),
            "golden": bool(golden),
            "badge": badge,
            "attack": attack,
            "health": health,
            "tavernTier": tavern_tier,
            "cost": cost,
        }

    def project_guide(self, guide: dict) -> dict:
        final_composition = [
            self.project_asset(item.get("CardKind", "Minion"), item["CardId"], item.get("Golden", False))
            for item in guide.get("FinalComposition", [])
        ]
        core_cards = [self.project_asset("Minion", value) for value in guide.get("CoreMinionCardIds", [])]
        core_cards.extend(self.project_asset("TavernSpell", value) for value in guide.get("CoreSpellCardNumbers", []))

        lesser_ids = guide.get("RecommendedLesserTrinketCardIds") or [guide.get("LesserTrinketCardId")]
        greater_ids = guide.get("RecommendedGreaterTrinketCardIds") or [guide.get("GreaterTrinketCardId")]
        lesser_ids = [value for value in lesser_ids if value]
        greater_ids = [value for value in greater_ids if value]

        profiles = [self.project_profile(profile) for profile in guide.get("EntryProfiles", [])]
        first_core = self.minions.get((guide.get("CoreMinionCardIds") or [""])[0].lower(), {})
        first_tribe = (first_core.get("tribes") or [""])[0]
        primary_tribe = TRIBE_NAMES.get(first_tribe, first_tribe)

        return {
            "guideId": guide["GuideId"],
            "revisionId": guide["RevisionId"],
            "gameVersionId": guide["GameVersionId"],
            "title": guide["Title"],
            "summary": guide.get("Summary", ""),
            "archetype": guide.get("Archetype", ""),
            "primaryTribe": primary_tribe,
            "activeTribes": [TRIBE_NAMES.get(value, value) for value in guide.get("ActiveTribes", [])],
            "hero": self.project_asset("Hero", guide["HeroCardId"]),
            "recommendedLesserTrinkets": [self.project_asset("Trinket", value) for value in lesser_ids],
            "recommendedGreaterTrinkets": [self.project_asset("Trinket", value) for value in greater_ids],
            "finalComposition": final_composition,
            "coreCards": core_cards,
            "coverCards": final_composition[:4],
            "defaultProfileId": next(
                (item["profileId"] for item in profiles if item["difficulty"] == "GuidedDiscover"),
                profiles[0]["profileId"],
            ),
            "profiles": profiles,
        }

    def project_profile(self, profile: dict) -> dict:
        zones = {"Shop": [], "Board": [], "Hand": []}
        for placement in profile.get("Placements", []):
            asset = self.project_asset(
                placement.get("CardKind", "Minion"),
                placement.get("CardId") or placement.get("CardNumber"),
                placement.get("Golden", False),
            )
            asset["placementId"] = placement.get("PlacementId", "")
            zones.setdefault(placement.get("Zone", "Hand"), []).append(asset)

        steps = []
        for index, action in enumerate(profile.get("RequiredActions", []), start=1):
            steps.append({
                "order": index,
                "actionId": action["ActionId"],
                "kind": action["Kind"],
                "count": int(action.get("Count") or 1),
                "instruction": action.get("Instruction", ""),
                "sourcePlacementId": action.get("SourcePlacementId", ""),
                "sourcePlacementIds": action.get("SourcePlacementIds", []),
                "targetPlacementId": action.get("TargetPlacementId", ""),
                "choiceId": action.get("ChoiceId", ""),
            })

        victory = profile.get("Victory") or {}
        completion = []
        if victory.get("RequireFinalComposition"):
            completion.append("完成目标阵容")
        if victory.get("RequireCombatWin"):
            completion.append("赢下下一场战斗")
        if profile.get("GrowthQuality"):
            completion.append("达到全部成长目标")

        return {
            "profileId": profile["ProfileId"],
            "difficulty": profile["Difficulty"],
            "title": profile["Title"],
            "learningGoal": profile.get("LearningGoal", ""),
            "startRound": int(profile.get("StartRound") or 0),
            "tavernTier": int(profile.get("TavernTier") or 0),
            "gold": int(profile.get("Gold") or 0),
            "maxGold": int(profile.get("MaxGold") or 0),
            "allowsUndo": int((profile.get("Undo") or {}).get("UsesPerRun") or 0) > 0,
            "keyDecisions": [value for value in profile.get("KeyDecisions", []) if value][:3],
            "shapingSpells": [self.project_asset("TavernSpell", value) for value in profile.get("ShapingSpellCardIds", [])],
            "growthTargets": profile.get("GrowthQuality", []),
            "startingShop": zones.get("Shop", []),
            "startingBoard": zones.get("Board", []),
            "startingHand": zones.get("Hand", []),
            "steps": steps,
            "completionCondition": "、".join(completion) or "完成本档训练目标",
        }


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--repository", type=Path, default=Path(__file__).resolve().parents[2])
    args = parser.parse_args()

    repository = args.repository.resolve()
    mini_program = repository / "MiniProgram"
    asset_outputs = [
        mini_program / "assets" / "cards",
        repository / "WebApp" / "public" / "assets" / "cards",
    ]
    fixture_output = mini_program / "fixtures" / "guides.js"
    web_fixture_output = repository / "WebApp" / "public" / "data" / "guides.json"
    if any(repository not in output.parents for output in [*asset_outputs, fixture_output, web_fixture_output]):
        raise ValueError("Strategy guide output escaped the project directory")

    for asset_output in asset_outputs:
        asset_output.mkdir(parents=True, exist_ok=True)
        for stale in asset_output.glob("*.jpg"):
            stale.unlink()

    data_root = repository / "Assets" / "LearnHearthstone" / "Resources" / "Data"
    main_guides = load_json(data_root / "battlegroundsStrategyGuides.json")
    expanded_guides = load_json(data_root / "battlegroundsStrategyGuidesExpandedTribes.json")
    projector = Projector(repository, asset_outputs)
    guides = [
        projector.project_guide(guide)
        for guide in [*main_guides.get("Guides", []), *expanded_guides.get("Guides", [])]
    ]
    if len(guides) != 8 or any(len(guide["profiles"]) != 3 for guide in guides):
        raise ValueError("Mini Program release requires exactly 8 guides with 3 profiles each")
    if projector.missing_images:
        raise FileNotFoundError("Missing real card images:\n" + "\n".join(sorted(set(projector.missing_images))))

    payload = {
        "schemaVersion": 1,
        "catalogRevisionId": f"{main_guides.get('CatalogRevisionId', '')}+{expanded_guides.get('CatalogRevisionId', '')}",
        "gameVersionId": guides[0]["gameVersionId"],
        "guides": guides,
    }
    fixture_output.write_text(
        "// Generated by Tools/Release/sync-mini-program-content.py; do not edit by hand.\n"
        + "module.exports = "
        + json.dumps(payload, ensure_ascii=False, indent=2)
        + "\n",
        encoding="utf-8",
        newline="\n",
    )
    web_fixture_output.parent.mkdir(parents=True, exist_ok=True)
    web_fixture_output.write_text(
        json.dumps(payload, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
        newline="\n",
    )
    package_bytes = sum(path.stat().st_size for path in asset_outputs[0].glob("*.jpg"))
    print(f"Mini Program guides: {len(guides)}")
    print(f"Profiles: {sum(len(guide['profiles']) for guide in guides)}")
    print(f"Thumbnails: {len(projector.generated_images)} ({package_bytes} bytes)")
    print(f"Fixture: {fixture_output}")
    print(f"Web fixture: {web_fixture_output}")


if __name__ == "__main__":
    main()
