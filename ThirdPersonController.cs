using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ThirdPersonController : MonoBehaviour
{
    [Header("Player data")]
    public float velocity = 5f;
    public float sprintAdittion = 3.5f;
    public float gravity = 9.8f;
    public float mouseSensitivity = 3.0f;

    [Header("UI data")]
    public HUD HUD;
    public float interactDistance = 3f;
    public LayerMask interactLayer;
    private IInteractable lookedAtInteractable;
    private List<IInteractable> nearbyInteractables = new List<IInteractable>();

    [Header("Gun Effects data")]
    public float fireFlashDuration = 0.4f;
    public GameObject impactConcretePrefab;
    public GameObject impactBodyPrefab;
    public int maxImpactEffects = 20;
    public AudioSource emptySound;
    private GameObject firePoint;
    public Transform weaponModelParent;

    [Header("Inventory data")]
    public Inventory inventory;
    public WeaponData EquipedWeapon;

    [Header("Flashlight data")]
    public Transform flashlight;
    public float flashlightFollowSpeed = 15f;
    public LayerMask flashlightBlockMask = ~0;

    // Player States
    bool isSprinting = false;
    bool isCrouching = false;
    bool isFlashlight = false;
    bool isAiming = false;

    private Queue<GameObject> impactEffectQueue = new Queue<GameObject>();

    // Components
    private CameraRecoil camRecoil;
    private WeaponBob weaponBob;
    private Animator animator;
    private CharacterController cc;
    private AudioSource fireSound;
    private Light fireLight;
    private ParticleSystem shotEmmision;
    private Dictionary<WeaponData, GameObject> weaponModelInstances = new Dictionary<WeaponData, GameObject>();
    private GameObject activeWeaponModel;

    private InputSystem inputSystem;

    // Inputs
    float inputHorizontal;
    float inputVertical;
    bool inputCrouch;
    bool inputSprint;
    bool inputEnterAim;
    bool inputExitAim;
    bool inputFire;
    bool inputReload;
    bool InputFlashlight;

    bool canShot = true;
    bool uiOpen = false;

    void Start()
    {
        inputSystem = InputSystem.Instance;
        if (inputSystem == null)
            Debug.LogError("InputSystem instance not found in scene.");

        cc = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        CacheFirePointComponents();

        camRecoil = Camera.main.GetComponent<CameraRecoil>();
        weaponBob = GetComponentInChildren<WeaponBob>();

        if (fireLight != null)
            fireLight.enabled = false;

        EquipWeapon(EquipedWeapon);
    }

    void Update()
    {
        if (inputSystem == null)
            return;

        if (inputSystem.IsPausePressed())
            UnlockCrusor(!uiOpen);

        // Quick weapon slots
        for (int i = 0; i < inventory.quickSlots.Length; i++)
        {
            if (inputSystem.IsQuickSlotPressed(i))
                EquipWeapon(inventory.quickSlots[i]);
        }

        AssignInputs();

        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        if (inputCrouch)
            isCrouching = !isCrouching;

        if (InputFlashlight)
            isFlashlight = !isFlashlight;

        if (inputReload)
            Reload();

        if (inputEnterAim)
        {
            isAiming = true;
            HUD.CallCrosshair();
            HUD.CallAmmoDisplay();
        }

        if (isAiming && inputExitAim)
        {
            isAiming = false;
            HUD.ForgetAmmoDisplay();
        }

        if (inputFire && canShot && EquipedWeapon != null && !uiOpen)
            HandleInputFire();

        if (flashlight != null)
        {
            Light flashlightLight = flashlight.GetComponent<Light>();
            if (flashlightLight != null)
                flashlightLight.enabled = isFlashlight;
        }

        if (cc.isGrounded && animator != null)
        {
            animator.SetBool("crouch", isCrouching);
            animator.SetFloat("speed", cc.velocity.magnitude);
            animator.SetBool("aim", isAiming);

            if (HUD.crosshairUI != null)
            {
                HUD.crosshairAimingUI.SetActive(isAiming);
                HUD.crosshairUI.SetActive(!isAiming);
            }
        }

        CheckLookPrompt();

        if (inputSystem.IsInteractPressed())
        {
            TryInteractNearby();
        }
    }

    private void FixedUpdate()
    {
        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        float velocityAddition = 0;

        if (isSprinting)
            velocityAddition = sprintAdittion;

        if (isCrouching)
            velocityAddition = -(velocity * 0.5f);

        // Combine input into a single direction vector
        Vector3 inputDirection = new Vector3(inputHorizontal, 0f, inputVertical);
        if (inputDirection.magnitude > 1f)
            inputDirection.Normalize(); // prevent faster diagonal movement

        // Transform input relative to camera
        Vector3 forward = Camera.main.transform.forward;
        Vector3 right = Camera.main.transform.right;

        forward.y = 0;
        right.y = 0;

        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection = (forward * inputDirection.z + right * inputDirection.x) * (velocity + velocityAddition) * Time.deltaTime;
        moveDirection.y = -gravity * Time.deltaTime;

        // Handle rotation
        if (isAiming)
        {
            Vector3 lookDirection = Camera.main.transform.forward;
            lookDirection.y = 0f;

            if (lookDirection.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
            }

            UpdateFlashlightDirection();
        }
        else if (inputDirection.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(forward.x * inputDirection.z + right.x * inputDirection.x,
                                      forward.z * inputDirection.z + right.z * inputDirection.x) * Mathf.Rad2Deg;
            Quaternion rotation = Quaternion.Euler(0, angle, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, 0.15f);
        }

        cc.Move(moveDirection);
    }


    void CheckLookPrompt()
    {
        lookedAtInteractable = null;

        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f));

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactLayer, QueryTriggerInteraction.Ignore))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                lookedAtInteractable = interactable;
                HUD.ShowPrompt(interactable.GetPrompt());
                return;
            }
        }

        HUD.HidePrompt();
    }

    void TryInteractNearby()
    {
        nearbyInteractables.RemoveAll(interactable => interactable == null);

        if (nearbyInteractables.Count == 0)
            return;

        // interact with closest
        IInteractable closest = nearbyInteractables[0];
        float closestDist = Mathf.Infinity;

        foreach (var interactable in nearbyInteractables)
        {
            MonoBehaviour mb = interactable as MonoBehaviour;

            if (mb == null)
                continue;

            float dist = Vector3.Distance(transform.position, mb.transform.position);

            if (dist < closestDist)
            {
                closestDist = dist;
                closest = interactable;
            }
        }

        closest.Interact(GetComponent<ThirdPersonController>());
    }

    public void RegisterNearby(IInteractable interactable)
    {
        if (!nearbyInteractables.Contains(interactable))
            nearbyInteractables.Add(interactable);
    }

    public void UnregisterNearby(IInteractable interactable)
    {
        if (nearbyInteractables.Contains(interactable))
            nearbyInteractables.Remove(interactable);
    }


    void UpdateFlashlightDirection()
    {
        if (flashlight == null) return;

        Ray ray = GetAimRay();
        Vector3 targetPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, 200f, flashlightBlockMask, QueryTriggerInteraction.Ignore))
            targetPoint = hit.point;
        else
            targetPoint = ray.origin + ray.direction * 200f;

        Vector3 dir = targetPoint - flashlight.position;
        Quaternion targetRot = Quaternion.LookRotation(dir);

        flashlight.rotation = Quaternion.Slerp(
            flashlight.rotation,
            targetRot,
            flashlightFollowSpeed * Time.deltaTime
        );
    }

    Ray GetAimRay()
    {
        Ray cameraRay = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        Vector3 rayOrigin = firePoint != null ? firePoint.transform.position : transform.position + Vector3.up * 1.5f;
        return new Ray(rayOrigin, cameraRay.direction);
    }

    void CacheFirePointComponents()
    {
        fireSound = null;
        fireLight = null;
        shotEmmision = null;

        if (firePoint == null)
            return;

        fireSound = firePoint.GetComponent<AudioSource>();
        fireLight = firePoint.GetComponent<Light>();
        shotEmmision = firePoint.GetComponent<ParticleSystem>();
    }

    GameObject GetOrCreateWeaponModel(WeaponData weapon)
    {
        if (weapon == null || weapon.modelPrefab == null)
            return null;

        if (weaponModelInstances.TryGetValue(weapon, out GameObject cachedModel) && cachedModel != null)
            return cachedModel;

        Transform parent = weaponModelParent != null ? weaponModelParent : transform;
        GameObject modelInstance = Instantiate(weapon.modelPrefab, parent);
        modelInstance.SetActive(false);
        weaponModelInstances[weapon] = modelInstance;

        return modelInstance;
    }

    void SetActiveWeaponModel(WeaponData weapon)
    {
        foreach (var model in weaponModelInstances.Values)
        {
            if (model != null)
                model.SetActive(false);
        }

        activeWeaponModel = GetOrCreateWeaponModel(weapon);

        if (activeWeaponModel != null)
            activeWeaponModel.SetActive(true);
    }

    Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent == null || string.IsNullOrEmpty(childName))
            return null;

        Queue<Transform> queue = new Queue<Transform>();
        queue.Enqueue(parent);

        while (queue.Count > 0)
        {
            Transform current = queue.Dequeue();

            if (current.name == childName)
                return current;

            for (int i = 0; i < current.childCount; i++)
                queue.Enqueue(current.GetChild(i));
        }

        return null;
    }

    void ResolveWeaponFirePoint(WeaponData weapon)
    {
        firePoint = null;

        if (activeWeaponModel == null)
        {
            CacheFirePointComponents();
            return;
        }

        string firePointName = string.IsNullOrEmpty(weapon.firePointName) ? "FirePoint" : weapon.firePointName;
        Transform muzzle = FindChildRecursive(activeWeaponModel.transform, firePointName);

        if (muzzle != null)
            firePoint = muzzle.gameObject;
        else
            Debug.LogWarning($"FirePoint '{firePointName}' not found on weapon model: {weapon.weaponName}");

        CacheFirePointComponents();

        if (fireLight != null)
            fireLight.enabled = false;
    }

    void ResolveFlashlight(WeaponData weapon)
    {
        flashlight = null;

        if (activeWeaponModel == null)
            return;

        string flashlightName = string.IsNullOrEmpty(weapon.flashlightName) ? "Flashlight" : weapon.flashlightName;
        Transform flashlightGM = FindChildRecursive(activeWeaponModel.transform, flashlightName);

        if (flashlightGM != null)
            flashlight = flashlightGM;
        else
            Debug.LogWarning($"Flashlight '{flashlightName}' not found on weapon model: {weapon.weaponName}");

        if (flashlight != null)
        {
            Light flashlightLight = flashlight.GetComponent<Light>();
            if (flashlightLight != null)
                flashlightLight.enabled = false;
        }
    }

    public void UnlockCrusor(bool newuiOpen)
    {
        uiOpen = newuiOpen;
        Cursor.lockState = uiOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = uiOpen;
    }

    void AssignInputs()
    {
        inputHorizontal = inputSystem.GetHorizontalInput();
        inputVertical = inputSystem.GetVerticalInput();
        inputSprint = inputSystem.IsSprintHeld();
        inputEnterAim = inputSystem.IsAimPressed();
        inputExitAim = inputSystem.IsAimReleased();
        inputFire = inputSystem.IsFirePressed();
        inputReload = inputSystem.IsReloadPressed();
        inputCrouch = inputSystem.IsCrouchPressed();
        InputFlashlight = inputSystem.IsFlashlightPressed();
    }

    void CameraRecoilAndBob()
    {
        if (camRecoil != null)
        {
            camRecoil.PlayRecoil();
            camRecoil.ApplyKickback(EquipedWeapon.kickbackAmount);
        }

        if (weaponBob != null && isAiming)
            weaponBob.ApplyRecoil();
    }

    void Reload()
    {
        if (EquipedWeapon == null) return;

        int neededAmmo = EquipedWeapon.defaultMagazineSize - EquipedWeapon.realMagazineSize;

        if (neededAmmo <= 0) return;

        int availableAmmo = inventory.GetAmmo(EquipedWeapon.ammoType);
        int ammoToLoad = Mathf.Min(neededAmmo, availableAmmo);

        EquipedWeapon.realMagazineSize += ammoToLoad;

        inventory.UseAmmo(EquipedWeapon.ammoType, ammoToLoad);

        HUD.SetLoadedAmmoAmount(EquipedWeapon.realMagazineSize);
        HUD.SetHoldAmmoAmount(inventory.GetAmmo(EquipedWeapon.ammoType));
    }

    void EquipWeapon(WeaponData weapon)
    {
        if (weapon == null) return;

        EquipedWeapon = weapon;

        SetActiveWeaponModel(weapon);
        ResolveWeaponFirePoint(weapon);
        ResolveFlashlight(weapon);

        if (fireSound != null)
            fireSound.clip = weapon.fireSound;

        if (emptySound != null)
            emptySound.clip = weapon.emptySound;

        if (weapon.defaultMagazineSize > 0)
        {
            HUD.lockAmmoDisplay = false;
            HUD.SetLoadedAmmoAmount(weapon.realMagazineSize);
            HUD.SetHoldAmmoAmount(inventory.GetAmmo(weapon.ammoType));
        }
        else
        {
            HUD.lockAmmoDisplay = true;
        }
    }

    IEnumerator FlashGun()
    {
        if (fireLight == null)
            yield break;

        fireLight.enabled = true;
        yield return new WaitForSecondsRealtime(fireFlashDuration);
        fireLight.enabled = false;
    }

    IEnumerator ResetShotDuration()
    {
        yield return new WaitForSecondsRealtime(EquipedWeapon.fireRate);
        canShot = true;
    }

    void HandleInputFire()
    {
        if (EquipedWeapon.realMagazineSize <= 0)
        {
            if (emptySound != null)
                emptySound.Play();

            return;
        }

        EquipedWeapon.realMagazineSize--;

        canShot = false;

        if (fireSound != null)
            fireSound.Play();

        if (shotEmmision != null)
            shotEmmision.Play();

        StartCoroutine(FlashGun());
        StartCoroutine(ResetShotDuration());

        CameraRecoilAndBob();

        HUD.crosshair.Shoot();
        HUD.SetLoadedAmmoAmount(EquipedWeapon.realMagazineSize);

        Ray ray = GetAimRay();

        if (Physics.Raycast(ray, out RaycastHit hit, EquipedWeapon.RayDistance))
        {
            if (firePoint != null)
                firePoint.transform.LookAt(hit.point);

            switch (hit.collider.tag)
            {
                case "Enemy":
                    HandleEnemyHit(hit, ray);
                    break;

                case "Wall":
                case "Floor":
                    HandleImpact(hit, impactConcretePrefab);
                    break;
            }

            Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.green, 1f);
        }
        else
        {
            Debug.DrawRay(ray.origin, ray.direction * EquipedWeapon.RayDistance, Color.red, 1f);
        }
    }

    void HandleEnemyHit(RaycastHit hit, Ray ray)
    {
        Enemy enemy = hit.collider.GetComponentInParent<Enemy>();

        if (enemy != null)
        {
            enemy.Hit(EquipedWeapon.damage, hit, ray.direction);
            HandleImpact(hit, impactBodyPrefab);
            return;
        }

        Ragdoll ragdoll = hit.collider.GetComponentInParent<Ragdoll>();

        if (ragdoll != null)
        {
            ragdoll.Hit(EquipedWeapon.damage, hit, ray.direction);
            HandleImpact(hit, impactBodyPrefab);
        }
    }

    void HandleImpact(RaycastHit hit, GameObject prefab)
    {
        GameObject impact = Instantiate(prefab, hit.point, Quaternion.LookRotation(hit.normal));
        impactEffectQueue.Enqueue(impact);

        if (impactEffectQueue.Count > maxImpactEffects)
        {
            Destroy(impactEffectQueue.Dequeue());
        }
    }
}