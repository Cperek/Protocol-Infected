using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Inventory : MonoBehaviour
{
    private enum InventoryItemType
    {
        Weapon,
        Ammo
    }

    private sealed class InventoryItem
    {
        public InventoryItemType type;
        public WeaponData weapon;
        public AmmoData ammo;
        public int ammoAmount;
    }

    public int maxWeapons = 6;
    public int money = 100;

    public List<WeaponData> ownedWeapons = new List<WeaponData>();

    private Dictionary<AmmoData, List<int>> ammoStorage = new Dictionary<AmmoData, List<int>>();

    public WeaponData[] quickSlots = new WeaponData[4];
    public AmmoData[] holdingAmmo = new AmmoData[4];

    [Header("UI")]
    public GameObject InventoryUI;
    public TMP_Text moneyText;
    public GameObject inventoryContentUI;
    public GameObject rowPrefabUI;
    public GameObject slotPrefabUI;

    public int rows = 2;
    public int columns = 6;

    [Header("Inventory Feel")]
    public Color slotNormalColor = Color.white;
    public Color slotSelectedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
    public KeyCode sortInventoryKey = KeyCode.Tab;

    private Shop shop;
    private GameManager gameManager;
    private readonly List<InventoryItem> inventoryItems = new List<InventoryItem>();
    private int selectedIndex;

    private void Start()
    {
        gameManager = GetComponent<GameManager>();
        if (gameManager != null)
            shop = gameManager.GetComponent<Shop>();

        foreach (AmmoData ammoT in holdingAmmo)
        {
            AddAmmo(ammoT, 60);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventory();
        }

        if (InventoryUI.activeSelf)
        {
            moneyText.text = money + "$";
            HandleInventoryInput();
        }
    }

    private void ToggleInventory()
    {
        if (shop != null && shop.ShopUI != null)
            shop.ShopUI.SetActive(false);

        bool shouldOpen = !InventoryUI.activeSelf;
        InventoryUI.SetActive(shouldOpen);

        if (gameManager != null && gameManager.Player != null)
            gameManager.Player.UnlockCrusor(shouldOpen);

        if (shouldOpen)
        {
            BuildInventoryItems();
            EnsureSelectedIndexInBounds();
            InstantiateInventory();
        }
    }

    private void HandleInventoryInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            MoveSelection(-1, 0);

        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            MoveSelection(1, 0);

        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            MoveSelection(0, -1);

        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            MoveSelection(0, 1);

        if (Input.GetKeyDown(KeyCode.Alpha1)) AssignSelectedToQuickSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) AssignSelectedToQuickSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) AssignSelectedToQuickSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) AssignSelectedToQuickSlot(3);

        if (Input.GetKeyDown(sortInventoryKey))
            SortAndRefreshInventory();
    }

    private void MoveSelection(int deltaX, int deltaY)
    {
        int slotCount = GetSlotCount();
        if (slotCount <= 0)
            return;

        int currentRow = selectedIndex / columns;
        int currentColumn = selectedIndex % columns;

        int targetColumn = Mathf.Clamp(currentColumn + deltaX, 0, columns - 1);
        int targetRow = Mathf.Clamp(currentRow + deltaY, 0, rows - 1);

        int targetIndex = (targetRow * columns) + targetColumn;
        selectedIndex = Mathf.Clamp(targetIndex, 0, slotCount - 1);
        InstantiateInventory();
    }

    private void AssignSelectedToQuickSlot(int quickSlotIndex)
    {
        if (quickSlotIndex < 0 || quickSlotIndex >= quickSlots.Length)
            return;

        InventoryItem selectedItem = GetSelectedItem();
        if (selectedItem == null || selectedItem.type != InventoryItemType.Weapon || selectedItem.weapon == null)
            return;

        quickSlots[quickSlotIndex] = selectedItem.weapon;
        InstantiateInventory();
    }

    // =========================
    // AMMO SYSTEM
    // =========================

    public void AddAmmo(AmmoData type, int amount)
    {
        if (type == null || amount <= 0)
            return;

        bool hasTypeAlready = ammoStorage.ContainsKey(type);
        if (!hasTypeAlready && !HasFreeInventorySlot())
            return;

        if (!hasTypeAlready)
            ammoStorage[type] = new List<int>();

        int remaining = amount;
        for (int i = 0; i < ammoStorage[type].Count; i++)
        {
            int space = type.maxCarry - ammoStorage[type][i];
            int toAdd = Mathf.Min(space, remaining);
            ammoStorage[type][i] += toAdd;
            remaining -= toAdd;

            if (remaining <= 0) break;
        }

        while (remaining > 0 && HasFreeInventorySlot())
        {
            int packageAmount = Mathf.Min(type.maxCarry, remaining);
            ammoStorage[type].Add(packageAmount);
            remaining -= packageAmount;
        }

        RefreshInventoryIfOpen();
    }

    public void UseAmmo(AmmoData type, int amount)
    {
        if (type == null || !ammoStorage.ContainsKey(type) || amount <= 0)
            return;

        int remaining = amount;
        for (int i = 0; i < ammoStorage[type].Count; i++)
        {
            if (ammoStorage[type][i] >= remaining)
            {
                ammoStorage[type][i] -= remaining;
                remaining = 0;
                break;
            }
            else
            {
                remaining -= ammoStorage[type][i];
                ammoStorage[type][i] = 0;
            }
        }

        ammoStorage[type].RemoveAll(x => x <= 0);
        if (ammoStorage[type].Count == 0)
            ammoStorage.Remove(type);

        RefreshInventoryIfOpen();
    }

    public int GetAmmo(AmmoData type)
    {
        if (type == null || !ammoStorage.ContainsKey(type))
            return 0;

        int total = 0;
        foreach (int pack in ammoStorage[type])
            total += pack;

        return total;
    }

    // =========================
    // INVENTORY UI
    // =========================

    private void InstantiateInventory(bool rebuildItems = true)
    {
        if (rebuildItems)
            BuildInventoryItems();

        EnsureSelectedIndexInBounds();
        ClearInventoryWindow();

        for (int i = 0; i < rows; i++)
        {
            GameObject newRow = Instantiate(rowPrefabUI, inventoryContentUI.transform);

            for (int x = 0; x < columns; x++)
            {
                int index = i * columns + x;
                GameObject newSlot = Instantiate(slotPrefabUI, newRow.transform);

                Image slotBackground = newSlot.GetComponent<Image>();
                if (slotBackground != null)
                    slotBackground.color = (index == selectedIndex) ? slotSelectedColor : slotNormalColor;

                Image itemImage = newSlot.transform.Find("image").GetComponent<Image>();
                TMP_Text itemName = newSlot.transform.Find("name").GetComponent<TMP_Text>();
                TMP_Text itemDecor = newSlot.transform.Find("decoration").GetComponent<TMP_Text>();
                Image itemDecorBg = newSlot.transform.Find("decorationBg").GetComponent<Image>();

                if (index >= inventoryItems.Count)
                {
                    itemImage.sprite = null;
                    itemName.text = "";
                    itemDecor.text = "";
                    itemImage.gameObject.SetActive(false);
                    itemName.gameObject.SetActive(false);
                    itemDecor.gameObject.SetActive(false);
                    itemDecorBg.gameObject.SetActive(false);
                    continue;
                }

                InventoryItem item = inventoryItems[index];

                if (item.type == InventoryItemType.Weapon && item.weapon != null)
                {
                    WeaponData weapon = item.weapon;
                    itemName.text = weapon.weaponName;
                    itemImage.sprite = weapon.SpriteUI;
                    itemDecor.text = "";
                    itemDecor.gameObject.SetActive(false);
                    itemDecorBg.gameObject.SetActive(false);
                }
                else if (item.type == InventoryItemType.Ammo && item.ammo != null)
                {
                    AmmoData ammo = item.ammo;
                    itemName.text = ammo.ammoPickupName;
                    itemImage.sprite = ammo.inventoryIcon;

                    itemDecor.text = item.ammoAmount.ToString();
                    itemDecor.gameObject.SetActive(true);
                    itemDecorBg.gameObject.SetActive(true);
                }
                else
                {
                    itemImage.sprite = null;
                    itemName.text = "";
                    itemDecor.text = "";
                }

                itemName.gameObject.SetActive(true);
                itemImage.gameObject.SetActive(true);
                
            }
        }
    }

    private void ClearInventoryWindow()
    {
        inventoryContentUI.transform.DestroyAllChildren();
    }

    private void BuildInventoryItems()
    {
        inventoryItems.Clear();

        for (int i = 0; i < ownedWeapons.Count; i++)
        {
            WeaponData weapon = ownedWeapons[i];
            if (weapon == null)
                continue;

            inventoryItems.Add(new InventoryItem
            {
                type = InventoryItemType.Weapon,
                weapon = weapon
            });
        }

        foreach (KeyValuePair<AmmoData, List<int>> entry in ammoStorage)
        {
            if (entry.Key == null)
                continue;

            for (int i = 0; i < entry.Value.Count; i++)
            {
                int packageAmount = entry.Value[i];
                if (packageAmount <= 0)
                    continue;

                inventoryItems.Add(new InventoryItem
                {
                    type = InventoryItemType.Ammo,
                    ammo = entry.Key,
                    ammoAmount = packageAmount
                });
            }
        }
    }

    private void SortAndRefreshInventory()
    {
        NormalizeAmmoPackagesForAllTypes();
        BuildInventoryItems();
        SortInventoryForDisplay();
        EnsureSelectedIndexInBounds();
        InstantiateInventory(false);
    }

    private void NormalizeAmmoPackagesForAllTypes()
    {
        List<AmmoData> typesToRemove = new List<AmmoData>();

        foreach (KeyValuePair<AmmoData, List<int>> entry in ammoStorage)
        {
            AmmoData ammoType = entry.Key;
            if (ammoType == null)
                continue;

            List<int> packages = entry.Value;
            if (packages == null)
            {
                typesToRemove.Add(ammoType);
                continue;
            }

            int totalAmmo = 0;
            for (int i = 0; i < packages.Count; i++)
            {
                if (packages[i] > 0)
                    totalAmmo += packages[i];
            }

            if (totalAmmo <= 0)
            {
                typesToRemove.Add(ammoType);
                continue;
            }

            packages.Clear();

            int fullPackages = totalAmmo / ammoType.maxCarry;
            int leftover = totalAmmo % ammoType.maxCarry;

            for (int i = 0; i < fullPackages; i++)
                packages.Add(ammoType.maxCarry);

            if (leftover > 0)
                packages.Add(leftover);
        }

        for (int i = 0; i < typesToRemove.Count; i++)
            ammoStorage.Remove(typesToRemove[i]);
    }

    public void SortInventoryForDisplay()
    {
        inventoryItems.Sort((left, right) =>
        {
            if (left == null && right == null) return 0;
            if (left == null) return 1;
            if (right == null) return -1;

            if (left.type != right.type)
                return left.type == InventoryItemType.Weapon ? -1 : 1;

            if (left.type == InventoryItemType.Weapon)
            {
                string leftName = left.weapon != null ? left.weapon.weaponName : string.Empty;
                string rightName = right.weapon != null ? right.weapon.weaponName : string.Empty;
                return string.Compare(leftName, rightName, System.StringComparison.OrdinalIgnoreCase);
            }

            int amountCompare = right.ammoAmount.CompareTo(left.ammoAmount);
            if (amountCompare != 0)
                return amountCompare;

            string leftAmmoName = left.ammo != null ? left.ammo.ammoPickupName : string.Empty;
            string rightAmmoName = right.ammo != null ? right.ammo.ammoPickupName : string.Empty;
            return string.Compare(leftAmmoName, rightAmmoName, System.StringComparison.OrdinalIgnoreCase);
        });
    }

    private string GetQuickSlotLabel(WeaponData weapon)
    {
        List<string> labels = new List<string>();

        for (int i = 0; i < quickSlots.Length; i++)
        {
            if (quickSlots[i] == weapon)
                labels.Add((i + 1).ToString());
        }

        if (labels.Count == 0)
            return "";

        return "SLOT " + string.Join("/", labels);
    }

    private InventoryItem GetSelectedItem()
    {
        if (selectedIndex < 0 || selectedIndex >= inventoryItems.Count)
            return null;

        return inventoryItems[selectedIndex];
    }

    private bool HasFreeInventorySlot()
    {
        return GetUsedSlotCount() < GetSlotCount();
    }

    private int GetUsedSlotCount()
    {
        int weaponCount = 0;
        for (int i = 0; i < ownedWeapons.Count; i++)
        {
            if (ownedWeapons[i] != null)
                weaponCount++;
        }

        int ammoTypes = 0;
        foreach (KeyValuePair<AmmoData, List<int>> entry in ammoStorage)
        {
            if (entry.Key == null)
                continue;

            for (int i = 0; i < entry.Value.Count; i++)
            {
                if (entry.Value[i] > 0)
                    ammoTypes++;
            }
        }

        return weaponCount + ammoTypes;
    }

    private int GetSlotCount()
    {
        return Mathf.Max(1, rows * columns);
    }

    private void EnsureSelectedIndexInBounds()
    {
        int slotCount = GetSlotCount();
        selectedIndex = Mathf.Clamp(selectedIndex, 0, slotCount - 1);
    }

    private void RefreshInventoryIfOpen()
    {
        if (!InventoryUI.activeSelf)
            return;

        InstantiateInventory();
    }

    // =========================
    // WEAPONS
    // =========================

    public bool AddWeapon(WeaponData weapon)
    {
        if (weapon == null)
            return false;

        if (ownedWeapons.Contains(weapon))
            return false;

        if (ownedWeapons.Count >= maxWeapons)
            return false;

        if (!HasFreeInventorySlot())
            return false;

        ownedWeapons.Add(weapon);
        RefreshInventoryIfOpen();
        return true;
    }

    public void RemoveWeapon(WeaponData weapon)
    {
        ownedWeapons.Remove(weapon);

        for (int i = 0; i < quickSlots.Length; i++)
        {
            if (quickSlots[i] == weapon)
                quickSlots[i] = null;
        }

        RefreshInventoryIfOpen();
    }

    public void AssignToQuickSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < quickSlots.Length && ownedWeapons.Count > 0 && ownedWeapons[0] != null)
        {
            quickSlots[slotIndex] = ownedWeapons[0];
            RefreshInventoryIfOpen();
        }
    }
}