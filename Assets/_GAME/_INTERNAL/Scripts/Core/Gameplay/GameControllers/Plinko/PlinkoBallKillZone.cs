using System;
using UI.Plinko;
using UnityEngine;

namespace Core.Gameplay.GameControllers.Plinko
{
    public class PlinkoBallKillZone : MonoBehaviour
    {
        public event Action<float> OnBallDropToKillZone;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if(collision.gameObject.TryGetComponent<PlayerBallView>(out var playerBall))
            {
                OnBallDropToKillZone?.Invoke(0f);
                Destroy(playerBall.gameObject);
            }
        }
    }
}