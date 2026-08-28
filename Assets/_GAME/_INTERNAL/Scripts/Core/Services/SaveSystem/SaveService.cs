using Core.Data;
using Newtonsoft.Json;
using System;
using System.IO;
using UnityEngine;

namespace Core.Services.SaveSystem
{
    public class SaveService
    {
        private const string KEY_SETTINGS = GameConstants.KEY_SETTINGS;

        private PlayerData _playerData;
        private string SaveFilePath => Path.Combine(Application.persistentDataPath, "player_data.sav");

        public PlayerData PlayerData => _playerData;

        public void Init(bool isDebug)
        {
            if (isDebug)
                DeleteAllSaves();

            LoadPlayerData();
        }

        private void LoadPlayerData()
        {
            if (File.Exists(SaveFilePath))
            {
                try
                {
                    string json = File.ReadAllText(SaveFilePath);
                    _playerData = JsonConvert.DeserializeObject<PlayerData>(json);
                    Debug.Log("[SaveService] File loaded successfully.");
                }
                catch
                {
                    CreateNewPlayerData();
                }
            }
            else
                CreateNewPlayerData();
        }

        private void CreateNewPlayerData()
        {
            _playerData = new PlayerData();
            SavePlayerData();
            Debug.Log("[SaveService] New player data created.");
        }

        public void SavePlayerData()
        {
            if (_playerData == null) 
                return;

            var avatarBackup = _playerData.CurrentAvatar;

            try
            {
                _playerData.CurrentAvatar = null;
                string json = JsonConvert.SerializeObject(_playerData);

                string tempPath = SaveFilePath + ".tmp";
                File.WriteAllText(tempPath, json);

                if (File.Exists(SaveFilePath))
                    File.Delete(SaveFilePath);

                File.Move(tempPath, SaveFilePath);

                Debug.Log("[SaveService] File saved successfully.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveService] File save failed: {e.Message}");
            }
            finally
            {
                _playerData.CurrentAvatar = avatarBackup;
            }
        }

        public void DeleteAllSaves()
        {
            if (File.Exists(SaveFilePath))
                File.Delete(SaveFilePath);

            Debug.Log("[SaveService] File deleted successfully.");
        }

        public bool HasProfile()
        {
            return PlayerPrefs.HasKey(GameConstants.KEY_HAS_PROFILE);
        }

        public void SetProfileCreated(bool value)
        {
            PlayerPrefs.SetInt(GameConstants.KEY_HAS_PROFILE, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}