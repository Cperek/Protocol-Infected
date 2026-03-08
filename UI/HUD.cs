using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class HUD : MonoBehaviour
{

    [Header("Crosshair")]
    public GameObject crosshairUI;
    public GameObject crosshairAimingUI;
    public DynamicCrosshair crosshair;

    [Header("Other")]
    public CanvasGroup ammoPanel;
    public TMP_Text loadedAmmo;
    public TMP_Text holdAmmo;
    public bool lockAmmoDisplay = false;

    [Header("Interact")]
    public GameObject interactPromptUI;
    public TMP_Text interactText;

    public void SetHoldAmmoAmount(int amount)
    {
        holdAmmo.text = amount.ToString();
    }
    public void SetLoadedAmmoAmount(int amount)
    {
        loadedAmmo.text = amount.ToString();
    }

    public void CallAmmoDisplay()
    {
        if (lockAmmoDisplay)
            return;
        StartCoroutine(Fade(ammoPanel, 0, 1, 0.5f));
    }

    public void ForgetAmmoDisplay()
    {
        StartCoroutine(Fade(ammoPanel, 1, 0, 0.5f));
    }

    IEnumerator Fade(CanvasGroup group, float start, float end, float duration)
    {
        float time = 0;
        group.alpha = start;

        while (time < duration)
        {
            time += Time.deltaTime;
            group.alpha = Mathf.Lerp(start, end, time / duration);
            yield return null;
        }

        group.alpha = end;
    }

    public void CallCrosshair()
    {
        crosshair.StartAiming();
    }


    public void ShowPrompt(string text)
    {
        interactPromptUI.SetActive(true);
        interactText.text = text;
    }

    public void HidePrompt()
    {
        interactPromptUI.SetActive(false);
    }


    private void Render(GameObject Object)
    {
        Object.SetActive(true);
    }

    private void Forget(GameObject Object)
    {
        Object.SetActive(false);
    }

}
