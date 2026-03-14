using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class TransformExtensions
{
    public static void DestroyAllChildren(this Transform parent)
    {
        foreach (Transform child in parent)
            GameObject.Destroy(child.gameObject);
    }
}

public class Shop : MonoBehaviour
{
    public List<WeaponData> availableWeapons;
    public Inventory playerInventory;
    public GameObject ShopUI;
    public GameObject ShopItemsList;
    public GameObject ItemPrefab;
    public TMP_Text moneyText;
    public float sellRate = 2f;

    GameManager gameManager;
    private bool shopUIPrewarmed;
    private InputSystem inputSystem;

    private void Start()
    {
        inputSystem = InputSystem.Instance;
        if (inputSystem == null)
            Debug.LogError("InputSystem instance not found in scene.");
        gameManager = GetComponent<GameManager>();
        BuyWindow();
        StartCoroutine(PrewarmShopUI());
    }

    private IEnumerator PrewarmShopUI()
    {
        if (shopUIPrewarmed || ShopUI == null)
            yield break;

        bool wasActive = ShopUI.activeSelf;
        ShopUI.SetActive(true);
        Canvas.ForceUpdateCanvases();

        yield return null;

        if (!wasActive)
            ShopUI.SetActive(false);

        shopUIPrewarmed = true;
    }

    private void Update()
    {
        if (inputSystem == null)
            return;

        if (inputSystem.IsBuyMenuPressed())
        {
            playerInventory.InventoryUI.SetActive(false);
            if (ShopUI.activeSelf == true)
            {
                ShopUI.SetActive(false);
                gameManager.Player.UnlockCrusor(false);
            }
            else
            {
                ShopUI.SetActive(true);
                gameManager.Player.UnlockCrusor(true);
            }
            
        }

        if(ShopUI.activeSelf == true)
        {
            moneyText.text = playerInventory.money.ToString() + "$";
        }
    }

    private void ClearShopWindow()
    {
        ShopItemsList.transform.DestroyAllChildren();

    }

    public void BuyWindow()
    {

        ClearShopWindow();
        
        for (int i = 0; i < availableWeapons.Count; i++)
        {
            int index = i;

            GameObject newItem = Instantiate<GameObject>(ItemPrefab, ShopItemsList.transform);
            TMP_Text itemName = newItem.transform.Find("name").GetComponent<TMP_Text>();
            TMP_Text itemPrice = newItem.transform.Find("price").GetComponent<TMP_Text>();
            Image itemImage = newItem.transform.Find("Image").GetComponent<Image>();
            itemName.text = availableWeapons[index].weaponName;
            itemPrice.text = availableWeapons[index].price.ToString() + "$";
            itemImage.sprite = availableWeapons[index].SpriteUI;
            newItem.GetComponent<Button>().onClick.AddListener(() => {
                BuyWeapon(availableWeapons[index], newItem);
            });
        }
    }

    public void SellWindow()
    {
        ClearShopWindow();

        for (int i = 0; i < playerInventory.ownedWeapons.Count; i++)
        {
            WeaponData weapon = playerInventory.ownedWeapons[i];

            GameObject newItem = Instantiate(ItemPrefab, ShopItemsList.transform);
            TMP_Text itemName = newItem.transform.Find("name").GetComponent<TMP_Text>();
            TMP_Text itemPrice = newItem.transform.Find("price").GetComponent<TMP_Text>();
            Image itemImage = newItem.transform.Find("Image").GetComponent<Image>();

            itemName.text = weapon.weaponName;
            itemPrice.text = ((int)(weapon.price / sellRate)).ToString() + "$";
            itemImage.sprite = weapon.SpriteUI;

            newItem.GetComponent<Button>().onClick.AddListener(() => {
                SellWeapon(weapon, newItem);
            });
        }
    }

    public void UpgradeWindow()
    {
        ClearShopWindow();

        for (int i = 0; i < playerInventory.ownedWeapons.Count; i++)
        {
            WeaponData weapon = playerInventory.ownedWeapons[i];

            GameObject newItem = Instantiate(ItemPrefab, ShopItemsList.transform);
            TMP_Text itemName = newItem.transform.Find("name").GetComponent<TMP_Text>();
            TMP_Text itemPrice = newItem.transform.Find("price").GetComponent<TMP_Text>();
            Image itemImage = newItem.transform.Find("Image").GetComponent<Image>();

            itemName.text = weapon.weaponName;
            itemPrice.text = "+";
            itemImage.sprite = weapon.SpriteUI;

            newItem.GetComponent<Button>().onClick.AddListener(() => {
                UpgradeWeapon(weapon, newItem);
            });
        }
    }

    public void BuyWeapon(WeaponData weapon, GameObject itemBox)
    {
        if (playerInventory.money >= weapon.price && playerInventory.AddWeapon(weapon))
        {
            playerInventory.money -= weapon.price;
            Destroy(itemBox);
        }

    }

    public void UpgradeWeapon(WeaponData weapon, GameObject itemBox)
    {
        //TODO:: implementacja wy�wietlania listy dost�pnych ulepsze� a nast�pnie ich obs�uga
    }

    public void SellWeapon(WeaponData weapon, GameObject itemBox)
    {
        if (playerInventory.ownedWeapons.Contains(weapon))
        {
            playerInventory.RemoveWeapon(weapon);
            playerInventory.money +=  (int) (weapon.price / sellRate);
            Destroy(itemBox);
        }
    }
}
