using UnityEngine;

public class WeaponModelKickback : MonoBehaviour
{
    private float kickbackZ = 0.06f;
    private float kickRotationX = 5f;
    private float kickRotationY = 1.5f;
    private float snappiness = 18f;
    private float returnSpeed = 10f;

    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;

    private Vector3 targetPositionOffset;
    private Vector3 currentPositionOffset;

    private Vector3 targetRotationOffset;
    private Vector3 currentRotationOffset;

    private bool initialized;

    void OnEnable()
    {
        EnsureBasePose();
        ResetPoseImmediate();
    }

    public void Configure(WeaponData weapon)
    {
        if (weapon == null)
            return;

        kickbackZ = Mathf.Max(0f, weapon.modelKickbackZ);
        kickRotationX = Mathf.Clamp(weapon.modelKickRotationX, -85f, 85f);
        kickRotationY = Mathf.Max(0f, weapon.modelKickRotationY);
        snappiness = Mathf.Max(1f, weapon.modelKickSnappiness);
        returnSpeed = Mathf.Max(1f, weapon.modelKickReturnSpeed);

        EnsureBasePose();
    }

    public void PlayKick()
    {
        EnsureBasePose();

        targetPositionOffset += Vector3.back * kickbackZ;

        float randomYaw = Random.Range(-kickRotationY, kickRotationY);
        float randomRoll = Random.Range(-kickRotationY * 0.35f, kickRotationY * 0.35f);
        targetRotationOffset += new Vector3(-kickRotationX, randomYaw, randomRoll);
    }

    public void ResetPoseImmediate()
    {
        EnsureBasePose();

        targetPositionOffset = Vector3.zero;
        currentPositionOffset = Vector3.zero;
        targetRotationOffset = Vector3.zero;
        currentRotationOffset = Vector3.zero;

        transform.localPosition = baseLocalPosition;
        transform.localRotation = baseLocalRotation;
    }

    void LateUpdate()
    {
        if (!initialized)
            return;

        targetPositionOffset = Vector3.Lerp(targetPositionOffset, Vector3.zero, Time.deltaTime * returnSpeed);
        currentPositionOffset = Vector3.Lerp(currentPositionOffset, targetPositionOffset, Time.deltaTime * snappiness);

        targetRotationOffset = Vector3.Lerp(targetRotationOffset, Vector3.zero, Time.deltaTime * returnSpeed);
        currentRotationOffset = Vector3.Lerp(currentRotationOffset, targetRotationOffset, Time.deltaTime * snappiness);

        transform.localPosition = baseLocalPosition + currentPositionOffset;
        transform.localRotation = baseLocalRotation * Quaternion.Euler(currentRotationOffset);
    }

    private void EnsureBasePose()
    {
        if (initialized)
            return;

        baseLocalPosition = transform.localPosition;
        baseLocalRotation = transform.localRotation;
        initialized = true;
    }
}
