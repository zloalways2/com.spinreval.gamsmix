using UI.Player;
using UnityEngine;

namespace Core.Services.Quests
{
    public class DailyQuestsEntryPoint : MonoBehaviour
    {
        [SerializeField] private DailyQuestsUIView _view;

        private void Awake()
        {
            _view.Init(GameServices.Quests);
        }

        private void Start()
        {
            _view.SetupViewsHolder();
        }
    }
}