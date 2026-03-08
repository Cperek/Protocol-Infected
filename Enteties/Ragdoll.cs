using UnityEngine;

public class Ragdoll : MonoBehaviour
{
    public GameObject bloodEffectPrefab;
    public float forceAmount = 100f;

    private AudioSource hitSound;
    private Vector3 lastHitPoint;
    private Vector3 lastHitDirection;

    private void Start()
    {
        hitSound = GetComponent<AudioSource>();
    }

    public void Hit(int damage, RaycastHit hit, Vector3 direction)
    {
        hitSound.Play();

        lastHitPoint = hit.point;
        lastHitDirection = direction;

        // Spawn blood effect
        if (bloodEffectPrefab != null)
        {
            GameObject blood = Instantiate(bloodEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            blood.transform.SetParent(hit.transform);
        }

        // Add force to the hit Rigidbody if available
        Rigidbody hitRb = hit.rigidbody;
        if (hitRb != null)
        {
            var newFoce = forceAmount * (damage / 10);
            hitRb.AddForceAtPosition(direction.normalized * newFoce, hit.point, ForceMode.Impulse);
        }
    }
}
