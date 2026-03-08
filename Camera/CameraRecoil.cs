using UnityEngine;
using System.Collections;

public class CameraRecoil : MonoBehaviour
{
    public float recoilStrength = 0.1f;
    public float recoilDuration = 0.1f;
    public float recoilSmoothnessCorrector = 0f;
    public float kickbackSpeed = 6f;          // How fast camera recoils upward
    public float recoverySpeed = 5f;          // How fast camera returns to normal

    private float currentKick = 0f;           // Current angle offset
    private float targetKick = 0f;            // Where we want the camera to be
    private Vector3 originalPos;
    private Coroutine recoilRoutine;

    void Start()
    {
        originalPos = transform.localPosition;
    }

    void Update()
    {
        // Smooth kick toward target
        currentKick = Mathf.Lerp(currentKick, targetKick, Time.deltaTime * kickbackSpeed);

        // Apply to camera pitch (x-axis)
        Vector3 euler = transform.localEulerAngles;
        euler.x = Mathf.Clamp(-currentKick, -80f, 80f); // clamp pitch
        transform.localEulerAngles = euler;

        // Smoothly recover back to normal
        targetKick = Mathf.Lerp(targetKick, 0f, Time.deltaTime * recoverySpeed);
    }

    public void PlayRecoil()
    {
        if (recoilRoutine != null)
            StopCoroutine(recoilRoutine);

        recoilRoutine = StartCoroutine(DoRecoil());
    }

    public void ApplyKickback(float amount)
    {
        targetKick += amount;
        targetKick = Mathf.Clamp(targetKick, 0f, 30f); // limit how far up it can go
    }

    IEnumerator DoRecoil()
    {
        float elapsed = 0f;
        float strength = recoilStrength;

        while (elapsed < recoilDuration)
        {
            Vector3 randomOffset = Random.insideUnitSphere * strength;
            transform.localPosition = originalPos + randomOffset;

            elapsed += Time.deltaTime;
            if(strength > 0){
                strength -= recoilSmoothnessCorrector;
            }
            yield return null;
        }

        transform.localPosition = originalPos;
    }
}