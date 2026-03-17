using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{

    public bool playHoverSound = true;
    public bool playClickSound = true;
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (playHoverSound)
        {
            UISoundManager.Instance?.PlayHover();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (playClickSound)
        {
            UISoundManager.Instance?.PlayClick();
        }
    }
}