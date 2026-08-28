using System;
using UnityEngine;

namespace UI.Other
{
    public abstract class Window : MonoBehaviour
    {
        public bool IsActive => gameObject.activeSelf;

        public virtual void Open(Action onComplete = null) => gameObject.SetActive(true);
        public virtual void Close(Action onComplete = null) => gameObject.SetActive(false);
    }
}