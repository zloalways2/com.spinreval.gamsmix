using UnityEngine;
using UnityEngine.UI;

namespace UI.Player
{
    public class FavoriteArcadeSlot : MonoBehaviour
    {
        [SerializeField] private Image _sprite;
        [SerializeField] private int _slotPosition;

        private int _totalPlayed;

        public void SetData(Sprite sprite, int totalPlayed)
        {
            _sprite.sprite = sprite;
            _totalPlayed = totalPlayed;
        }
    }
}