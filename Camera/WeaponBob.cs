using UnityEngine;

public class WeaponBob : MonoBehaviour
{
    public Transform weaponTransform;

    [Header("Movement Bob")]
    public float bobSpeed = 8f;
    public float bobAmount = 0.05f;

    [Header("Recoil Kick")]
    public float recoilAmount = 0.1f;
    public float recoilRecoverySpeed = 8f;

    private Vector3 initialPosition;
    private Vector3 recoilOffset;
    private float inputHorizontal;
    private float inputVertical;

    void Start()
    {
        if (weaponTransform == null)
            weaponTransform = transform;

        initialPosition = weaponTransform.localPosition;
    }

    void Update()
    {
        inputHorizontal = Input.GetAxis("Horizontal");
        inputVertical = Input.GetAxis("Vertical");
        // Weapon movement bob (based on time)
        float bobX =  (inputVertical > 0 || inputHorizontal > 0) ? Mathf.Sin(Time.time * bobSpeed) * bobAmount : 0f;
        float bobY =  (inputVertical > 0 || inputHorizontal > 0) ? Mathf.Cos(Time.time * bobSpeed * 2f) * bobAmount * 0.5f : 0f;

        Vector3 bobOffset = new Vector3(bobX, bobY, 0f);

        // Lerp recoil recovery
        recoilOffset = Vector3.Lerp(recoilOffset, Vector3.zero, Time.deltaTime * recoilRecoverySpeed);

        // Combine bob + recoil
        weaponTransform.localPosition = initialPosition + bobOffset + recoilOffset;
    }

    public void ApplyRecoil()
    {
        recoilOffset += new Vector3(0, 0, -recoilAmount); // Push backward on Z
    }
}