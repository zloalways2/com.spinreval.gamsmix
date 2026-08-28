using UnityEngine;

namespace Core.SO.Common
{
    [CreateAssetMenu(menuName = "Special Configs/Debug/Debug Config", fileName = "DebugConfig")]
    public class DebugConfig : ScriptableObject
    {
        [field: SerializeField] public bool IsDebug { get; private set; }
    }
}