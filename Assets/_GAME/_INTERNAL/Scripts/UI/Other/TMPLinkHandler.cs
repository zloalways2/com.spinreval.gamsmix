using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.Other
{
    public class TMPLinkHandler : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private TMP_Text _text;

        public void OnPointerClick(PointerEventData eventData)
        {
            int linkIndex = TMP_TextUtilities.FindIntersectingLink(_text, eventData.position, eventData.pressEventCamera);

            if (linkIndex == -1)
                return;

            TMP_LinkInfo linkInfo = _text.textInfo.linkInfo[linkIndex];

            switch (linkInfo.GetLinkID())
            {
                case "terms":
                    Application.OpenURL("https://telegra.ph/Terms-Of-Use-08-17-8");
                    Debug.Log("Terms Of Use URL opened");
                    break;
                case "privacy":
                    Application.OpenURL("https://telegra.ph/Privacy-Policy-08-17-129");
                    Debug.Log("Privacy Poliscy URL opened");
                    break;
            }
        }
    }
}