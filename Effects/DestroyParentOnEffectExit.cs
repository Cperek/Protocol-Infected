using UnityEngine;
using UnityEngine.VFX;


public class DestroyParentOnEffectExit : MonoBehaviour
{

    private VisualEffect vfx;

    void Start()
    {
        vfx = GetComponent<VisualEffect>();

        if (vfx == null)
        {
            Debug.LogWarning("No VisualEffect component found on this GameObject.");
            return;
        }

    }

    void Update()
    {
        // Check if VFX has finished playing automatically
        if (vfx != null && !vfx.HasAnySystemAwake())
        {
            DestroyParent();
        }
    }

    private void OnVFXStopped(VisualEffect v)
    {
        DestroyParent();
    }

    private void DestroyParent()
    {
        if (transform.parent != null)
        {
            Destroy(transform.parent.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
