using UnityEngine;

public class Enemy : MonoBehaviour
{
    public int HP;
    public GameObject bloodEffectPrefab;

    [Header("Ragdoll")]
    public GameObject ragdollPrefab;


    private AudioSource hitSound;
    private Vector3 lastHitPoint;
    private Vector3 lastHitDirection;

    private void Start()
    {
        hitSound = GetComponent<AudioSource>();
    }
    // Update is called once per frame
    void Update()
    {
        if(HP < 0)
        {
            ReplaceWithRagdoll();
        }
    }

    public void Hit(int damage, RaycastHit hit, Vector3 direction)
    {
        hitSound.Play();

        lastHitPoint = hit.point;
        lastHitDirection = direction;

        if (bloodEffectPrefab != null)
        {
            GameObject blood = Instantiate(bloodEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            blood.transform.SetParent(hit.transform);

        }
        HP -= damage;
    }

    void ReplaceWithRagdoll()
    {
        if (ragdollPrefab != null)
        {
            GameObject ragdoll = Instantiate(ragdollPrefab, transform.position, transform.rotation);
            ragdoll.transform.localScale = transform.localScale;

            // Copy pose before physics takes over
            CopyTransformData(transform, ragdoll.transform);

            Rigidbody[] ragdollBodies = ragdoll.GetComponentsInChildren<Rigidbody>();
            Rigidbody closestRb = null;
            float closestDist = Mathf.Infinity;

            foreach (var rb in ragdollBodies)
            {
                // Inherit movement if desired
                rb.linearVelocity = GetComponent<Rigidbody>()?.linearVelocity ?? Vector3.zero;

                // Find closest part to hit point
                float dist = Vector3.Distance(rb.worldCenterOfMass, lastHitPoint);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestRb = rb;
                }
            }

            // Apply force at impact point
            if (closestRb != null)
            {
                closestRb.AddForce(lastHitDirection.normalized * 300f); // tweak force as needed
            }
        }

        Destroy(gameObject);
    }

    void CopyTransformData(Transform source, Transform destination)
    {
        destination.position = source.position;
        destination.rotation = source.rotation;

        for (int i = 0; i < source.childCount; i++)
        {
            Transform sourceChild = source.GetChild(i);
            Transform destChild = destination.Find(sourceChild.name);

            if (destChild != null)
            {
                CopyTransformData(sourceChild, destChild);
            }
        }
    }

}
