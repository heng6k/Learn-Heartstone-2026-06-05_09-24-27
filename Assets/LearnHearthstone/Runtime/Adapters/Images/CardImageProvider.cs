using LearnHearthstone.Domain.Models;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LearnHearthstone.Adapters.Images
{
    public sealed class CardImageProvider
    {
        private const float FullTexturePixelsPerUnit = 100f;
        private const float FullCardAspectThreshold = 1.2f;
        private const string ArtDisplayContainTag = "art_display:contain";
        private const string ArtDisplayCropTag = "art_display:crop";
        private static readonly Dictionary<string, Sprite> fullTextureSpriteCache = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Sprite> resourceSpriteCache = new Dictionary<string, Sprite>();
        private readonly Sprite fallback;

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void RegisterEditorCacheInvalidation()
        {
            UnityEditor.EditorApplication.projectChanged -= ClearCaches;
            UnityEditor.EditorApplication.projectChanged += ClearCaches;
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearCaches()
        {
            fullTextureSpriteCache.Clear();
            resourceSpriteCache.Clear();
        }

        public CardImageProvider(Sprite fallback = null)
        {
            this.fallback = fallback;
        }

        public Sprite Load(MinionDefinition definition)
        {
            return LoadSprite(definition, fallback);
        }

        public Sprite Load(MinionInstance instance)
        {
            return LoadSprite(instance, fallback);
        }

        public static Sprite LoadSprite(MinionDefinition definition, Sprite fallback = null)
        {
            if (definition == null)
            {
                return fallback;
            }

            return LoadSprite(definition.ImagePath, definition.CardId, CardKind.Minion, fallback);
        }

        public static Sprite LoadSprite(MinionInstance instance, Sprite fallback = null)
        {
            if (instance == null)
            {
                return fallback;
            }

            return LoadSprite(instance.ImagePath, instance.CardId, instance.CardKind, fallback);
        }

        public static Sprite LoadSprite(string imagePath, string cardId, CardKind cardKind, Sprite fallback = null)
        {
            foreach (var candidate in CandidatePaths(imagePath, cardId, cardKind))
            {
                if (ShouldLoadFullTexture(cardKind))
                {
                    var fullTextureSprite = LoadFullTextureSprite(candidate);
                    if (fullTextureSprite != null)
                    {
                        return fullTextureSprite;
                    }
                }

                var sprite = LoadResourceSprite(candidate);
                if (sprite != null)
                {
                    return sprite;
                }
            }

            return fallback;
        }

        public static bool ShouldCropToPortrait(Sprite sprite, IEnumerable<string> tags = null)
        {
            if (tags != null && tags.Any(tag => string.Equals(tag, ArtDisplayContainTag, System.StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            if (tags != null && tags.Any(tag => string.Equals(tag, ArtDisplayCropTag, System.StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            return sprite != null &&
                   sprite.rect.width > 0.01f &&
                   sprite.rect.height / sprite.rect.width >= FullCardAspectThreshold;
        }

        private static Sprite LoadFullTextureSprite(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            if (fullTextureSpriteCache.TryGetValue(path, out var cached))
            {
                return cached;
            }

            var texture = Resources.Load<Texture2D>(path);
            if (texture == null)
            {
                texture = LoadResourceSprite(path)?.texture;
            }

            if (texture == null)
            {
                fullTextureSpriteCache[path] = null;
                return null;
            }

            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                FullTexturePixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
            sprite.name = texture.name + "_FullCard";
            fullTextureSpriteCache[path] = sprite;
            return sprite;
        }

        private static Sprite LoadResourceSprite(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            if (resourceSpriteCache.TryGetValue(path, out var cached))
            {
                return cached;
            }

            var sprite = Resources.Load<Sprite>(path)
                ?? Resources.LoadAll<Sprite>(path).FirstOrDefault();
            resourceSpriteCache[path] = sprite;
            return sprite;
        }

        private static IEnumerable<string> CandidatePaths(string imagePath, string cardId, CardKind cardKind)
        {
            var seen = new HashSet<string>();
            var paths = new List<string>();
            AddCandidate(paths, seen, NormalizeResourcePath(imagePath));

            if (!string.IsNullOrWhiteSpace(cardId))
            {
                if (cardKind == CardKind.TavernSpell)
                {
                    AddCandidate(paths, seen, "CardImages/TavernSpells/" + cardId);
                }
                else if (cardKind == CardKind.HeroPower)
                {
                    AddCandidate(paths, seen, "HeroBuddyImages/heroPowers/" + cardId);
                }
                else if (cardKind == CardKind.HeroBuddy)
                {
                    AddCandidate(paths, seen, "HeroBuddyImages/buddies/" + cardId);
                }
                else if (cardKind == CardKind.Hero)
                {
                    AddCandidate(paths, seen, "HeroBuddyImages/heroes/" + cardId);
                }

                AddCandidate(paths, seen, "CardImages/" + cardId);
            }

            return paths;
        }

        private static bool ShouldLoadFullTexture(CardKind cardKind)
        {
            return cardKind == CardKind.Minion
                || cardKind == CardKind.TavernSpell
                || cardKind == CardKind.Hero
                || cardKind == CardKind.HeroPower
                || cardKind == CardKind.HeroBuddy
                || cardKind == CardKind.Spell
                || cardKind == CardKind.Trinket
                || cardKind == CardKind.Quest
                || cardKind == CardKind.QuestReward;
        }

        private static void AddCandidate(List<string> paths, HashSet<string> seen, string value)
        {
            if (!string.IsNullOrWhiteSpace(value) && seen.Add(value))
            {
                paths.Add(value);
            }
        }

        private static string NormalizeResourcePath(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var normalized = value.Replace('\\', '/').Trim();
            const string resourcesMarker = "/Resources/";
            var resourcesIndex = normalized.IndexOf(resourcesMarker, System.StringComparison.OrdinalIgnoreCase);
            if (resourcesIndex >= 0)
            {
                normalized = normalized.Substring(resourcesIndex + resourcesMarker.Length);
            }

            if (normalized.StartsWith("Resources/", System.StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring("Resources/".Length);
            }

            if (normalized.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (normalized.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase)
                || normalized.EndsWith(".jpg", System.StringComparison.OrdinalIgnoreCase)
                || normalized.EndsWith(".jpeg", System.StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(0, normalized.LastIndexOf('.'));
            }

            return normalized;
        }
    }
}
