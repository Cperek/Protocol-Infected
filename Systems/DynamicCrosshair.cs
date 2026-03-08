using UnityEngine;

public class DynamicCrosshair : MonoBehaviour
{
    public RectTransform top;
    public RectTransform bottom;
    public RectTransform left;
    public RectTransform right;

    public float baseSize = 10f;
    public float maxSpread = 40f;

    public float spreadIncrease = 20f;
    public float startAimSpeadIncrease = 10f;
    public float spreadDecay = 25f;
    public float movementSpread = 10f;

    float currentSpread;

    void Update()
    {
        bool moving = Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0;

        if (moving)
            currentSpread += movementSpread * Time.deltaTime;

        currentSpread -= spreadDecay * Time.deltaTime;
        currentSpread = Mathf.Clamp(currentSpread, 0, maxSpread);
        // Debug.Log("Current Spread: " + currentSpread);
        UpdateCrosshair();
    }

    public void Shoot()
    {
        currentSpread += spreadIncrease;
    }

    public void StartAiming()
    {
        currentSpread += spreadIncrease;
    }

    void UpdateCrosshair()
    {
        float spread = baseSize + currentSpread;

        top.anchoredPosition = new Vector2(0, spread);
        bottom.anchoredPosition = new Vector2(0, -spread);
        left.anchoredPosition = new Vector2(-spread, 0);
        right.anchoredPosition = new Vector2(spread, 0);
    }
}