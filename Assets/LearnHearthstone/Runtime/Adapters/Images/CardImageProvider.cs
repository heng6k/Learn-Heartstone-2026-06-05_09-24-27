using LearnHearthstone.Domain.Models;
using UnityEngine;

namespace LearnHearthstone.Adapters.Images
{
    public sealed class CardImageProvider
    {
        private readonly Sprite fallback;

        public CardImageProvider(Sprite fallback = null)
        {
            this.fallback = fallback;
        }

        public Sprite Load(MinionDefinition definition)
        {
            if (definition == null || string.IsNullOrEmpty(definition.ImagePath))
            {
                return fallback;
            }

            return Resources.Load<Sprite>(definition.ImagePath) ?? fallback;
        }
    }
}
