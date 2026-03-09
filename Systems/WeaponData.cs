using UnityEngine;

public enum WeaponType { Gun, Melee };


[CreateAssetMenu(fileName = "WeaponData", menuName = "Weapons/New Weapon")]
public class WeaponData : ScriptableObject
{
    public string weaponName;
    public int price;
    public WeaponType type;
    public int damage;
    public int defaultMagazineSize;
    public int realMagazineSize;
    public AmmoData ammoType;
    public float fireRate;
    public float kickbackAmount;
    public float RayDistance;
    public AudioClip fireSound;
    public AudioClip emptySound;
    public Sprite SpriteUI;
    public GameObject modelPrefab;
    public string firePointName = "FirePoint";
    public string flashlightName = "Flashlight";
}
