using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

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
        public int itemId;
    }

    private sealed class AmmoPackage
    {
        public int id;
        public int amount;
    }

    public int maxWeapons = 6;
    public int money = 100;

    public List<WeaponData> ownedWeapons = new List<WeaponData>();

    private Dictionary<AmmoData, List<AmmoPackage>> ammoStorage = new Dictionary<AmmoData, List<AmmoPackage>>();

    public WeaponData[] quickSlots = new WeaponData[4];
    public AmmoData[] holdingAmmo = new AmmoData[4];

    [Header("UI")]
    public GameObject InventoryUI;
    public TMP_Text moneyText;
    public GameObject inventoryContentUI;
    public GameObject rowPrefabUI;
    public GameObject slotPrefabUI;
    public GameObject[] quickSlotDropZones = new GameObject[4];

    public int rows = 2;
    public int columns = 6;

    [Header("Inventory Feel")]
    public Color slotNormalColor = Color.white;
    public Color slotSelectedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
    public bool dragDropDebugLogs;

    private Shop shop;
    private GameManager gameManager;
    private readonly List<InventoryItem> inventoryItems = new List<InventoryItem>();
    private readonly List<int> manualInventoryOrderIds = new List<int>();
    private int selectedIndex;
    private bool keepInventorySorted;
    private int draggedInventoryIndex = -1;
    private int nextAmmoPackageId = 1;
    private bool dropHandledThisDrag;
    private bool inventoryUIPrewarmed;
    private InputSystem inputSystem;

    private void Start()
    {
        inputSystem = InputSystem.Instance;
        if (inputSystem == null)
            Debug.LogError("InputSystem instance not found in scene.");

        gameManager = GetComponent<GameManager>();
        if (gameManager != null)
            shop = gameManager.GetComponent<Shop>();

        foreach (AmmoData ammoT in holdingAmmo)
        {
            AddAmmo(ammoT, 60);
        }

        ValidateDragDropSetup();
        ConfigureQuickSlotDropZones();
        RefreshQuickSlotVisuals();
        StartCoroutine(PrewarmInventoryUI());
    }

    private IEnumerator PrewarmInventoryUI()
    {
        if (inventoryUIPrewarmed || InventoryUI == null)
            yield break;

        bool wasActive = InventoryUI.activeSelf;
        InventoryUI.SetActive(true);

        BuildInventoryItems();
        EnsureSelectedIndexInBounds();
        InstantiateInventory();
        Canvas.ForceUpdateCanvases();

        yield return null;

        if (!wasActive)
            InventoryUI.SetActive(false);

        inventoryUIPrewarmed = true;
    }

    private void Update()
    {
        if (inputSystem == null)
            return;

        if (inputSystem.IsInventoryPressed())
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
            ValidateDragDropSetup();
            ConfigureQuickSlotDropZones();
            BuildInventoryItems();
            EnsureSelectedIndexInBounds();
            InstantiateInventory();
        }
    }

    private void HandleInventoryInput()
    {
        if (inputSystem.IsInventoryLeftPressed())
            MoveSelection(-1, 0);

        if (inputSystem.IsInventoryRightPressed())
            MoveSelection(1, 0);

        if (inputSystem.IsInventoryUpPressed())
            MoveSelection(0, -1);

        if (inputSystem.IsInventoryDownPressed())
            MoveSelection(0, 1);

        if (inputSystem.IsQuickSlotPressed(0)) AssignSelectedToQuickSlot(0);
        if (inputSystem.IsQuickSlotPressed(1)) AssignSelectedToQuickSlot(1);
        if (inputSystem.IsQuickSlotPressed(2)) AssignSelectedToQuickSlot(2);
        if (inputSystem.IsQuickSlotPressed(3)) AssignSelectedToQuickSlot(3);

        if (inputSystem.IsSortInventoryPressed())
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
        RefreshQuickSlotVisuals();
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
            ammoStorage[type] = new List<AmmoPackage>();

        int remaining = amount;
        for (int i = 0; i < ammoStorage[type].Count; i++)
        {
            int space = type.maxCarry - ammoStorage[type][i].amount;
            int toAdd = Mathf.Min(space, remaining);
            ammoStorage[type][i].amount += toAdd;
            remaining -= toAdd;

            if (remaining <= 0) break;
        }

        while (remaining > 0 && HasFreeInventorySlot())
        {
            int packageAmount = Mathf.Min(type.maxCarry, remaining);
            ammoStorage[type].Add(new AmmoPackage
            {
                id = nextAmmoPackageId++,
                amount = packageAmount
            });
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
            if (ammoStorage[type][i].amount >= remaining)
            {
                ammoStorage[type][i].amount -= remaining;
                remaining = 0;
                break;
            }
            else
            {
                remaining -= ammoStorage[type][i].amount;
                ammoStorage[type][i].amount = 0;
            }
        }

        ammoStorage[type].RemoveAll(x => x == null || x.amount <= 0);
        if (ammoStorage[type].Count == 0)
            ammoStorage.Remove(type);

        RefreshInventoryIfOpen();
    }

    public int GetAmmo(AmmoData type)
    {
        if (type == null || !ammoStorage.ContainsKey(type))
            return 0;

        int total = 0;
        foreach (AmmoPackage pack in ammoStorage[type])
        {
            if (pack != null && pack.amount > 0)
                total += pack.amount;
        }

        return total;
    }

    // =========================
    // INVENTORY UI
    // =========================

    private void InstantiateInventory(bool rebuildItems = true)
    {
        if (rebuildItems)
        {
            if (keepInventorySorted)
                NormalizeAmmoPackagesForAllTypes();

            BuildInventoryItems();

            if (keepInventorySorted)
                SortInventoryForDisplay();

            ApplyManualInventoryOrderIfNeeded();
        }

        EnsureInventoryListHasSlotPlaceholders();

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
                if (item == null)
                {
                    itemImage.sprite = null;
                    itemName.text = "";
                    itemDecor.text = "";
                    itemImage.gameObject.SetActive(false);
                    itemName.gameObject.SetActive(false);
                    itemDecor.gameObject.SetActive(false);
                    itemDecorBg.gameObject.SetActive(false);

                    InventorySlotDropHandler emptySlotDropHandler = newSlot.GetComponent<InventorySlotDropHandler>();
                    if (emptySlotDropHandler == null)
                        emptySlotDropHandler = newSlot.AddComponent<InventorySlotDropHandler>();

                    emptySlotDropHandler.Configure(this, index);
                    continue;
                }

                if (item.type == InventoryItemType.Weapon && item.weapon != null)
                {
                    WeaponData weapon = item.weapon;
                    itemName.text = weapon.weaponName;
                    itemImage.sprite = weapon.SpriteUI;
                    string quickSlotLabel = GetQuickSlotLabel(weapon);
                    itemDecor.text = quickSlotLabel;
                    bool hasLabel = !string.IsNullOrEmpty(quickSlotLabel);
                    itemDecor.gameObject.SetActive(hasLabel);
                    itemDecorBg.gameObject.SetActive(true);
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

                InventorySlotDropHandler slotDropHandler = newSlot.GetComponent<InventorySlotDropHandler>();
                if (slotDropHandler == null)
                    slotDropHandler = newSlot.AddComponent<InventorySlotDropHandler>();

                slotDropHandler.Configure(this, index);
                
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

        foreach (KeyValuePair<AmmoData, List<AmmoPackage>> entry in ammoStorage)
        {
            if (entry.Key == null)
                continue;

            for (int i = 0; i < entry.Value.Count; i++)
            {
                AmmoPackage package = entry.Value[i];
                if (package == null || package.amount <= 0)
                    continue;

                inventoryItems.Add(new InventoryItem
                {
                    type = InventoryItemType.Ammo,
                    ammo = entry.Key,
                    ammoAmount = package.amount,
                    itemId = package.id
                });
            }
        }

        for (int i = 0; i < inventoryItems.Count; i++)
        {
            if (inventoryItems[i].type == InventoryItemType.Weapon && inventoryItems[i].weapon != null)
                inventoryItems[i].itemId = GetWeaponItemId(inventoryItems[i].weapon);
        }
    }

    private void SortAndRefreshInventory()
    {
        keepInventorySorted = true;
        InstantiateInventory();
    }

    private void NormalizeAmmoPackagesForAllTypes()
    {
        List<AmmoData> typesToRemove = new List<AmmoData>();

        foreach (KeyValuePair<AmmoData, List<AmmoPackage>> entry in ammoStorage)
        {
            AmmoData ammoType = entry.Key;
            if (ammoType == null)
                continue;

            List<AmmoPackage> packages = entry.Value;
            if (packages == null)
            {
                typesToRemove.Add(ammoType);
                continue;
            }

            int totalAmmo = 0;
            for (int i = 0; i < packages.Count; i++)
            {
                if (packages[i] != null && packages[i].amount > 0)
                    totalAmmo += packages[i].amount;
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
                packages.Add(new AmmoPackage { id = nextAmmoPackageId++, amount = ammoType.maxCarry });

            if (leftover > 0)
                packages.Add(new AmmoPackage { id = nextAmmoPackageId++, amount = leftover });
        }

        for (int i = 0; i < typesToRemove.Count; i++)
            ammoStorage.Remove(typesToRemove[i]);

        SaveManualOrderFromCurrentItems();
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

        return "#" + string.Join("/", labels);
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

    private int GetWeaponItemId(WeaponData weapon)
    {
        return weapon != null ? weapon.GetInstanceID() : 0;
    }

    private void ApplyManualInventoryOrderIfNeeded()
    {
        if (keepInventorySorted)
        {
            SaveManualOrderFromCurrentItems();
            return;
        }

        int slotCount = GetSlotCount();
        if (manualInventoryOrderIds.Count == 0)
        {
            EnsureInventoryListHasSlotPlaceholders();
            SaveManualOrderFromCurrentItems();
            return;
        }

        Dictionary<int, InventoryItem> itemsById = new Dictionary<int, InventoryItem>();
        List<InventoryItem> unordered = new List<InventoryItem>();

        for (int i = 0; i < inventoryItems.Count; i++)
        {
            InventoryItem item = inventoryItems[i];
            if (item == null || item.itemId == 0 || itemsById.ContainsKey(item.itemId))
            {
                unordered.Add(item);
                continue;
            }

            itemsById[item.itemId] = item;
        }

        List<InventoryItem> ordered = new List<InventoryItem>(slotCount);

        for (int i = 0; i < slotCount; i++)
        {
            int id = i < manualInventoryOrderIds.Count ? manualInventoryOrderIds[i] : 0;
            if (itemsById.TryGetValue(id, out InventoryItem matched))
            {
                ordered.Add(matched);
                itemsById.Remove(id);
            }
            else
            {
                ordered.Add(null);
            }
        }

        foreach (KeyValuePair<int, InventoryItem> pair in itemsById)
            unordered.Add(pair.Value);

        for (int i = 0; i < unordered.Count; i++)
        {
            int freeIndex = ordered.FindIndex(x => x == null);
            if (freeIndex >= 0)
                ordered[freeIndex] = unordered[i];
        }

        while (ordered.Count < slotCount)
            ordered.Add(null);

        if (ordered.Count > slotCount)
            ordered.RemoveRange(slotCount, ordered.Count - slotCount);

        inventoryItems.Clear();
        inventoryItems.AddRange(ordered);
        SaveManualOrderFromCurrentItems();
    }

    private void SaveManualOrderFromCurrentItems()
    {
        manualInventoryOrderIds.Clear();

        if (keepInventorySorted)
        {
            for (int i = 0; i < inventoryItems.Count; i++)
            {
                if (inventoryItems[i] != null && inventoryItems[i].itemId != 0)
                    manualInventoryOrderIds.Add(inventoryItems[i].itemId);
            }

            return;
        }

        int slotCount = GetSlotCount();
        EnsureInventoryListHasSlotPlaceholders();
        for (int i = 0; i < slotCount; i++)
        {
            InventoryItem item = i < inventoryItems.Count ? inventoryItems[i] : null;
            manualInventoryOrderIds.Add(item != null ? item.itemId : 0);
        }
    }

    private void EnsureInventoryListHasSlotPlaceholders()
    {
        int slotCount = GetSlotCount();
        while (inventoryItems.Count < slotCount)
            inventoryItems.Add(null);

        if (inventoryItems.Count > slotCount)
            inventoryItems.RemoveRange(slotCount, inventoryItems.Count - slotCount);
    }

    private void ConfigureQuickSlotDropZones()
    {
        int count = Mathf.Min(quickSlots.Length, quickSlotDropZones.Length);
        for (int i = 0; i < count; i++)
        {
            GameObject zone = quickSlotDropZones[i];
            if (zone == null)
                continue;

            InventoryQuickSlotDropHandler dropHandler = zone.GetComponent<InventoryQuickSlotDropHandler>();
            if (dropHandler == null)
                dropHandler = zone.AddComponent<InventoryQuickSlotDropHandler>();

            dropHandler.Configure(this, i);
        }
    }

    private void RefreshQuickSlotVisuals()
    {
        int count = quickSlots.Length;
        for (int i = 0; i < count; i++)
        {
            GameObject slotUI = i < quickSlotDropZones.Length ? quickSlotDropZones[i] : null;
            if (slotUI == null && i < quickSlotDropZones.Length)
                slotUI = quickSlotDropZones[i];

            if (slotUI == null)
                continue;

            Transform imageTransform = slotUI.transform.Find("image");
            Transform nameTransform = slotUI.transform.Find("name");
            Transform decorationTransform = slotUI.transform.Find("decoration");
            Transform decorationBgTransform = slotUI.transform.Find("decorationBg");

            Image itemImage = imageTransform != null ? imageTransform.GetComponent<Image>() : null;
            TMP_Text itemName = nameTransform != null ? nameTransform.GetComponent<TMP_Text>() : null;
            TMP_Text itemDecor = decorationTransform != null ? decorationTransform.GetComponent<TMP_Text>() : null;
            Image itemDecorBg = decorationBgTransform != null ? decorationBgTransform.GetComponent<Image>() : null;

            WeaponData weapon = quickSlots[i];
            bool hasWeapon = weapon != null;

            if (itemImage != null)
            {
                itemImage.sprite = hasWeapon ? weapon.SpriteUI : null;
                itemImage.gameObject.SetActive(hasWeapon);
            }

            if (itemName != null)
            {
                itemName.text = hasWeapon ? weapon.weaponName : "";
                itemName.gameObject.SetActive(hasWeapon);
            }

            if (itemDecor != null)
            {
                itemDecor.text = hasWeapon ? (i + 1).ToString() : "";
                itemDecor.gameObject.SetActive(hasWeapon);
            }

            if (itemDecorBg != null)
                itemDecorBg.gameObject.SetActive(hasWeapon);
        }
    }

    public void BeginDragFromInventorySlot(int slotIndex)
    {
        if (!InventoryUI.activeSelf)
            return;

        if (slotIndex < 0 || slotIndex >= inventoryItems.Count)
            return;

        if (inventoryItems[slotIndex] == null)
            return;

        draggedInventoryIndex = slotIndex;
        dropHandledThisDrag = false;

        if (dragDropDebugLogs)
            Debug.Log($"[Inventory] Begin drag from slot {slotIndex}");
    }

    public void EndDragFromInventorySlot()
    {
        if (dragDropDebugLogs && draggedInventoryIndex >= 0)
            Debug.Log($"[Inventory] End drag from slot {draggedInventoryIndex}");

        draggedInventoryIndex = -1;
    }

    public void ResolveDropFromPointer(PointerEventData eventData)
    {
        if (eventData == null)
            return;

        if (dropHandledThisDrag)
            return;

        if (draggedInventoryIndex < 0 || draggedInventoryIndex >= inventoryItems.Count)
            return;

        GameObject targetObject = eventData.pointerCurrentRaycast.gameObject;
        if (targetObject != null)
        {
            InventorySlotDropHandler directSlotTarget = targetObject.GetComponentInParent<InventorySlotDropHandler>();
            if (directSlotTarget != null)
            {
                DropDraggedItemOnInventorySlot(directSlotTarget.SlotIndex);
                return;
            }

            InventoryQuickSlotDropHandler directQuickSlotTarget = targetObject.GetComponentInParent<InventoryQuickSlotDropHandler>();
            if (directQuickSlotTarget != null)
            {
                DropDraggedItemOnQuickSlot(directQuickSlotTarget.QuickSlotIndex);
                return;
            }
        }

        EventSystem currentEventSystem = EventSystem.current;
        if (currentEventSystem != null)
        {
            PointerEventData pointer = new PointerEventData(currentEventSystem)
            {
                position = eventData.position
            };

            List<RaycastResult> results = new List<RaycastResult>();
            currentEventSystem.RaycastAll(pointer, results);

            for (int i = 0; i < results.Count; i++)
            {
                GameObject hit = results[i].gameObject;
                if (hit == null)
                    continue;

                InventorySlotDropHandler slotTarget = hit.GetComponentInParent<InventorySlotDropHandler>();
                if (slotTarget != null)
                {
                    DropDraggedItemOnInventorySlot(slotTarget.SlotIndex);
                    return;
                }

                InventoryQuickSlotDropHandler quickSlotTarget = hit.GetComponentInParent<InventoryQuickSlotDropHandler>();
                if (quickSlotTarget != null)
                {
                    DropDraggedItemOnQuickSlot(quickSlotTarget.QuickSlotIndex);
                    return;
                }
            }
        }

        if (TryGetInventorySlotIndexFromPointer(eventData.position, out int fallbackInventorySlotIndex))
        {
            DropDraggedItemOnInventorySlot(fallbackInventorySlotIndex);
            return;
        }

        if (TryGetQuickSlotIndexFromPointer(eventData.position, out int fallbackQuickSlotIndex))
        {
            DropDraggedItemOnQuickSlot(fallbackQuickSlotIndex);
            return;
        }

        if (targetObject == null)
        {
            if (dragDropDebugLogs)
                Debug.Log("[Inventory] Drag ended with no UI target under pointer.");
            return;
        }

        if (dragDropDebugLogs)
            Debug.Log($"[Inventory] Drag ended over unsupported UI target: {targetObject.name}");
    }

    private bool TryGetInventorySlotIndexFromPointer(Vector2 screenPosition, out int slotIndex)
    {
        slotIndex = -1;
        if (inventoryContentUI == null)
            return false;

        Canvas canvas = inventoryContentUI.GetComponentInParent<Canvas>();
        Camera eventCamera = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

        int currentIndex = 0;
        for (int row = 0; row < inventoryContentUI.transform.childCount; row++)
        {
            Transform rowTransform = inventoryContentUI.transform.GetChild(row);
            for (int col = 0; col < rowTransform.childCount; col++)
            {
                Transform slotTransform = rowTransform.GetChild(col);
                RectTransform slotRect = slotTransform as RectTransform;
                if (slotRect != null && RectTransformUtility.RectangleContainsScreenPoint(slotRect, screenPosition, eventCamera))
                {
                    slotIndex = currentIndex;
                    return true;
                }

                currentIndex++;
            }
        }

        return false;
    }

    private bool TryGetQuickSlotIndexFromPointer(Vector2 screenPosition, out int quickSlotIndex)
    {
        quickSlotIndex = -1;
        int count = Mathf.Min(quickSlots.Length, quickSlotDropZones.Length);

        Canvas canvas = GetComponentInParent<Canvas>();
        Camera eventCamera = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

        for (int i = 0; i < count; i++)
        {
            GameObject zone = quickSlotDropZones[i];
            if (zone == null)
                continue;

            RectTransform zoneRect = zone.transform as RectTransform;
            if (zoneRect != null && RectTransformUtility.RectangleContainsScreenPoint(zoneRect, screenPosition, eventCamera))
            {
                quickSlotIndex = i;
                return true;
            }
        }

        return false;
    }

    public void DropDraggedItemOnInventorySlot(int targetSlotIndex)
    {
        int slotCount = GetSlotCount();

        if (draggedInventoryIndex < 0 || draggedInventoryIndex >= inventoryItems.Count)
        {
            if (dragDropDebugLogs)
                Debug.Log("[Inventory] Drop ignored: no active dragged slot index");
            return;
        }

        if (targetSlotIndex < 0 || targetSlotIndex >= slotCount)
            return;

        EnsureInventoryListHasSlotPlaceholders();

        if (draggedInventoryIndex == targetSlotIndex)
            return;

        InventoryItem draggedItem = inventoryItems[draggedInventoryIndex];
        if (draggedItem == null)
            return;

        keepInventorySorted = false;

        InventoryItem targetItem = inventoryItems[targetSlotIndex];
        inventoryItems[targetSlotIndex] = draggedItem;
        inventoryItems[draggedInventoryIndex] = targetItem;

        selectedIndex = targetSlotIndex;
        SaveManualOrderFromCurrentItems();
        InstantiateInventory(false);
        dropHandledThisDrag = true;

        if (dragDropDebugLogs)
            Debug.Log($"[Inventory] Swapped slots {draggedInventoryIndex} and {targetSlotIndex}");
    }

    public void DropDraggedItemOnQuickSlot(int quickSlotIndex)
    {
        if (quickSlotIndex < 0 || quickSlotIndex >= quickSlots.Length)
            return;

        if (draggedInventoryIndex < 0 || draggedInventoryIndex >= inventoryItems.Count)
        {
            if (dragDropDebugLogs)
                Debug.Log("[Inventory] Quickslot drop ignored: no active dragged slot index");
            return;
        }

        InventoryItem item = inventoryItems[draggedInventoryIndex];
        if (item == null || item.type != InventoryItemType.Weapon || item.weapon == null)
        {
            if (dragDropDebugLogs)
                Debug.Log("[Inventory] Quickslot drop ignored: dragged item is not a weapon");
            return;
        }

        quickSlots[quickSlotIndex] = item.weapon;
        RefreshQuickSlotVisuals();
        InstantiateInventory(false);
        dropHandledThisDrag = true;

        if (dragDropDebugLogs)
            Debug.Log($"[Inventory] Weapon {item.weapon.weaponName} assigned to quickslot {quickSlotIndex + 1}");
    }

    private void ValidateDragDropSetup()
    {
        if (!dragDropDebugLogs)
            return;

        if (EventSystem.current == null)
            Debug.LogWarning("[Inventory] Drag&Drop: Missing EventSystem in scene.");

        if (slotPrefabUI == null)
            Debug.LogWarning("[Inventory] Drag&Drop: slotPrefabUI is not assigned.");

        int assignedQuickZones = 0;
        for (int i = 0; i < quickSlotDropZones.Length; i++)
        {
            if (quickSlotDropZones[i] != null)
                assignedQuickZones++;
        }

        if (assignedQuickZones == 0)
            Debug.LogWarning("[Inventory] Drag&Drop: quickSlotDropZones are not assigned.");
        else
            Debug.Log($"[Inventory] Drag&Drop setup: {assignedQuickZones}/{quickSlotDropZones.Length} quickslot zones assigned.");
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
        foreach (KeyValuePair<AmmoData, List<AmmoPackage>> entry in ammoStorage)
        {
            if (entry.Key == null)
                continue;

            for (int i = 0; i < entry.Value.Count; i++)
            {
                if (entry.Value[i] != null && entry.Value[i].amount > 0)
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

        RefreshQuickSlotVisuals();

        RefreshInventoryIfOpen();
    }

    public void AssignToQuickSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < quickSlots.Length && ownedWeapons.Count > 0 && ownedWeapons[0] != null)
        {
            quickSlots[slotIndex] = ownedWeapons[0];
            RefreshQuickSlotVisuals();
            RefreshInventoryIfOpen();
        }
    }
}

public sealed class InventorySlotDropHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    private Inventory inventory;
    private int slotIndex;

    public int SlotIndex => slotIndex;

    public void Configure(Inventory ownerInventory, int index)
    {
        inventory = ownerInventory;
        slotIndex = index;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (inventory == null)
            return;

        inventory.BeginDragFromInventorySlot(slotIndex);
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (inventory == null)
            return;

        inventory.DropDraggedItemOnInventorySlot(slotIndex);
    }

    public void OnDrag(PointerEventData eventData)
    {
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (inventory == null)
            return;

        inventory.ResolveDropFromPointer(eventData);
        inventory.EndDragFromInventorySlot();
    }
}

public sealed class InventoryQuickSlotDropHandler : MonoBehaviour, IDropHandler
{
    private Inventory inventory;
    private int quickSlotIndex;

    public int QuickSlotIndex => quickSlotIndex;

    public void Configure(Inventory ownerInventory, int index)
    {
        inventory = ownerInventory;
        quickSlotIndex = index;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (inventory == null)
            return;

        inventory.DropDraggedItemOnQuickSlot(quickSlotIndex);
    }
}