using UnityEngine;

public class AmmoPickup : MonoBehaviour, IInteractable
{
    public AmmoData ammoType;
    public int amount = 6;
    public GameObject floatingUI;
    public bool ShowPrompt = false;
    private ThirdPersonController nearbyPlayer;

    private GameObject uiInstance = null;

    public string GetPrompt()
    {
        if (!ShowPrompt)
            return null;

        return "Press E to pick up " + ammoType.ammoPickupName;
    }

    public void Interact(ThirdPersonController player)
    {
        player.inventory.AddAmmo(ammoType, amount);

        player.HUD.SetHoldAmmoAmount(
            player.inventory.GetAmmo(player.EquipedWeapon.ammoType)
        );

        player.UnregisterNearby(this);

        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        ThirdPersonController interaction = other.GetComponent<ThirdPersonController>();

        if (interaction != null)
        {
            if (uiInstance == null && floatingUI != null)
            {
                uiInstance = GameObject.Instantiate(
                    floatingUI,
                    transform.position,
                    Quaternion.identity,
                    transform
                );
                uiInstance.SetActive(true);
            }

            nearbyPlayer = interaction;
            interaction.RegisterNearby(this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (uiInstance != null)
        {
            Destroy(uiInstance);
            uiInstance = null;
        }

        ThirdPersonController interaction = other.GetComponent<ThirdPersonController>();

        if (interaction != null)
        {
            if (nearbyPlayer == interaction)
                nearbyPlayer = null;

            interaction.UnregisterNearby(this);
        }
    }

    private void OnDestroy()
    {
        if (uiInstance != null)
        {
            Destroy(uiInstance);
            uiInstance = null;
        }

        if (nearbyPlayer != null)
            nearbyPlayer.UnregisterNearby(this);
    }
}