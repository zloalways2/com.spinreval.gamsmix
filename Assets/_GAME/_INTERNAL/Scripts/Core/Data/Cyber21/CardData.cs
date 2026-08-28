using System.Collections.Generic;
using UnityEngine;

namespace Core.Data.Cyber21
{
    [System.Serializable]
    public struct CardData
    {
        public List<Sprite> CardSprites;
        public int CardValue;
        public bool IsAce;
    }
}