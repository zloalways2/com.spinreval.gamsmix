using Core.Services;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

namespace Core.Common
{
    public abstract class GameEntryPoint : MonoBehaviour
    {
        [SerializeField] protected GameController _controller;

        private CancellationTokenSource _cancellationTokenSource;

        private void Start()
        {
            _cancellationTokenSource = new();

            _controller.Enter();
            _controller.Initialize();

            AsyncUpdatePlaytime(_cancellationTokenSource.Token).Forget();
        }

        private void OnDestroy()
        {
            _controller.Exit();

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        private void UpdatePlayTime()
        {
            if (GameServices.SaveService.PlayerData != null)
                GameServices.SaveService.PlayerData.PlayTimeSeconds++;
        }

        private async UniTask AsyncUpdatePlaytime(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: cancellationToken);
                UpdatePlayTime();
            }
        }
    }
}