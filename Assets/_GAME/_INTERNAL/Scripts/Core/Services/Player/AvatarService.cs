using Core.Data;
using System;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;
using static NativeGallery;

namespace Core.Services.Player
{
    public class AvatarService
    {
        private readonly PlayerData _playerData;
        private Texture2D _avatarTexture;
        private readonly int _maxAvatarSize = 512;

        public event Action<Texture2D> OnAvatarSetted;

        public AvatarService(PlayerData playerData)
        {
            _playerData = playerData;
            LoadSavedAvatar();
        }

        public void RequestPermission()
        {
            RequestPermissionAsync(permission =>
            {
                HandlePermissionGranded(permission);
            }, PermissionType.Read, MediaType.Image);
        }

        private void SaveAvatar(Texture2D texture)
        {
            string avatarPath = _playerData.AvatarPath;

            try
            {
                byte[] pngBytes = texture.EncodeToPNG();
                File.WriteAllBytes(avatarPath, pngBytes);
                Debug.Log($"Avatar saved: {avatarPath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Avatar saving error: {e.Message}");
            }
        }

        public void LoadSavedAvatar()
        {
            if (_playerData.CurrentAvatar != null)
            {
                _avatarTexture = _playerData.CurrentAvatar;
                return;
            }

            string avatarPath = GameServices.PlayerService.GetData().AvatarPath;

            if (!File.Exists(avatarPath))
            {
                Debug.Log("Saved avatar not found");
                return;
            }

            try
            {
                byte[] bytes = File.ReadAllBytes(avatarPath);
                Texture2D texture = new(2, 2, TextureFormat.RGB24, false);

                if (texture.LoadImage(bytes))
                {
                    SetAvatarTexture(texture);
                    Debug.Log("Avatar loaded from save");
                }
                else
                {
                    Object.Destroy(texture);
                    Debug.LogError("Saved avatar could not be decoded");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Avatar loading error: {e.Message}");
            }
        }

        private void ShowPermissionDeniedMessage() => OpenSettings();

        private void SetAvatarTexture(Texture2D texture)
        {
            Texture2D currentAvatar = _playerData.CurrentAvatar;

            if (currentAvatar != null && currentAvatar != texture)
                Object.Destroy(currentAvatar);

            _playerData.CurrentAvatar = texture;
            _avatarTexture = texture;
            Debug.Log($"[Avatar Service] Current Player avatar: {_avatarTexture.name}");
            OnAvatarSetted?.Invoke(texture);
        }

        private void HandlePermissionGranded(Permission permission)
        {
            if (permission != Permission.Granted)
            {
                ShowPermissionDeniedMessage();
                return;
            }

            GetImageFromGallery(HandleSelectedImage, "Choose an avatar", "image/*");
        }

        private void HandleSelectedImage(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.Log($"Choose is cancelled");
                return;
            }

            Debug.Log($"Image choosen: {path}");

            Texture2D texture = LoadImageAtPath(path, _maxAvatarSize, false, false, false);

            if (texture == null)
            {
                Debug.LogError($"Couldn't upload image from: {path}");
                return;
            }

            SaveAvatar(texture);
            SetAvatarTexture(texture);
        }
    }
}