using UnityEngine;

namespace UI.Other
{
    public abstract class Screen : MonoBehaviour
    {
        public bool IsActive => gameObject.activeSelf;

        public void Open() => gameObject.SetActive(true);
        public void Close() => gameObject.SetActive(false);
    }
}