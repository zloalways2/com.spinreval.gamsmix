using UnityEngine;

namespace Core.Services.Audio
{
    [System.Serializable]
    public class SoundEntry
    {
        public SoundType Type;
        public AudioClip Clip;
    }
}