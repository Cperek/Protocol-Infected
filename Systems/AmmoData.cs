using UnityEngine;

[CreateAssetMenu(fileName = "AmmoData", menuName = "Weapons/Ammo Type")]
public class AmmoData : ScriptableObject
{
    [Header("Info")]
    public string ammoName;
    public string ammoPickupName;
    public Sprite inventoryIcon;

    [Header("Limits")]
    public int maxCarry = 60;

}