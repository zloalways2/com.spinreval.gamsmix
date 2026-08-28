using System;
using UnityEngine;

namespace UI.Plinko
{
    public class BucketView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;

        [SerializeField] private float _multiplier;

        public Action<float> OnBallEntered;

        public void Init(float multiplier, Sprite sprite)
        {
            if (_spriteRenderer != null)
                _spriteRenderer.sprite = sprite;
            _multiplier = multiplier;
        }

        public void InvokeBallEntered() => OnBallEntered?.Invoke(_multiplier);
    }
}