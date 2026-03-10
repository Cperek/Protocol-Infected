using UnityEngine;

public class Bilboard : MonoBehaviour
{
    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            return;
        }

        Vector3 horizontalForward = cam.transform.forward;
        horizontalForward.y = 0f;

        if (horizontalForward.sqrMagnitude > 0.0001f)
        {
            transform.forward = horizontalForward.normalized;
        }
    }
}