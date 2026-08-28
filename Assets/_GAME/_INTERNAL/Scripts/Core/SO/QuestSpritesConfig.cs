using Core.Services.Quests;
using System.Collections.Generic;
using UnityEngine;

namespace Core.SO
{
    [CreateAssetMenu(menuName = "Meta game/Daily Quests/Quest Sprites Config")]
    public class QuestSpritesConfig : ScriptableObject
    {
        [field: SerializeField] public List<SpriteEntry> Entries { get; private set; } = new();

        private Dictionary<string, Sprite> _spriteMap;

        public void Initialize()
        {
            _spriteMap = new Dictionary<string, Sprite>();
            foreach (var entry in Entries)
            {
                if (!_spriteMap.ContainsKey(entry.Tag))
                    _spriteMap.Add(entry.Tag, entry.Sprite);
                else
                    Debug.LogWarning($"[QuestSpritesConfig] Duplicate tag: {entry.Tag}");
            }
        }

        public Sprite GetSprite(string tag)
        {
            if (_spriteMap == null) 
                Initialize();

            if (_spriteMap.TryGetValue(tag, out var sprite))
                return sprite;

            Debug.LogWarning($"[QuestSpritesConfig] Sprite not found for tag: {tag}");
            return null;
        }
    }
}