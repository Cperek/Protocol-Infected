using UnityEngine;

public class AmmoPickup : MonoBehaviour, IInteractable
{
    public AmmoData ammoType;
    public int amount = 6;
    public GameObject floatingUI;
    private ThirdPersonController nearbyPlayer;

    public string GetPrompt()
    {
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
        floatingUI.SetActive(true);
        ThirdPersonController interaction = other.GetComponent<ThirdPersonController>();

        if (interaction != null)
        {
            nearbyPlayer = interaction;
            interaction.RegisterNearby(this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        floatingUI.SetActive(false);
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
        if (nearbyPlayer != null)
            nearbyPlayer.UnregisterNearby(this);
    }
}