using Core.Gameplay.GameControllers.Plinko;
using Core.SO;
using System.Collections.Generic;
using UnityEngine;

namespace UI.Plinko
{
    public class PlayerBallView : MonoBehaviour
    {
        private MathBallMover _mathMover;

        private void Awake()
        {
            Debug.Log($"[Player Ball View] Ball position: {transform.position}");
        }

        /// <summary>
        /// Инициализирует мяч для движения по математическому пути.
        /// </summary>
        public void InitForMathMovement(
            PlinkoPath path,
            PlinkoConfig config,
            List<PegView> allPegs,
            List<BucketView> allBuckets,
            Vector3 initialPosition,
            AudioSource audioSource)
        {
            // Добавляем компонент математического движения если его нет
            if (_mathMover == null)
                _mathMover = gameObject.GetComponent<MathBallMover>();

            if (_mathMover == null)
                _mathMover = gameObject.AddComponent<MathBallMover>();

            _mathMover.SetInitialPosition(initialPosition);

            // Настраиваем и запускаем движение
            _mathMover.StartMove(path, config, allPegs, allBuckets, audioSource);
        }
    }
}