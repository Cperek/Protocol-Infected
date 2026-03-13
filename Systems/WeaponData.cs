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
    [Header("Model Kickback")]
    public float modelKickbackZ = 0.06f;
    public float modelKickRotationX = 5f;
    public float modelKickRotationY = 1.5f;
    public float modelKickSnappiness = 18f;
    public float modelKickReturnSpeed = 10f;

    [Header("Character Animation")]
    public string fireTriggerName = "";
    public string reloadTriggerName = "reload";
    public AnimationClip reloadAnimationClip;
    public float reloadAnimationDuration = 0f;

    public float RayDistance;
    public AudioClip fireSound;
    public AudioClip emptySound;
    public Sprite SpriteUI;
    public GameObject modelPrefab;
    public string firePointName = "FirePoint";
    public string flashlightName = "Flashlight";
}
