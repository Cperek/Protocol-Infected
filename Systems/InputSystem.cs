using UnityEngine;

public class InputSystem : MonoBehaviour
{
    [Header("General")]
    public KeyCode PauseKey = KeyCode.Escape;
    public KeyCode InteractKey = KeyCode.E;
    public KeyCode InventoryKey = KeyCode.I;
    public KeyCode ReloadKey = KeyCode.R;
    public KeyCode CrouchKey = KeyCode.LeftControl;
    public KeyCode FlashlightKey = KeyCode.F;
    public KeyCode SortInventoryKey = KeyCode.Tab;
    public KeyCode BuyMenuKey = KeyCode.B;

    [Header("Inventory Navigation")]
    public KeyCode InventoryLeftKey = KeyCode.LeftArrow;
    public KeyCode InventoryLeftAltKey = KeyCode.A;
    public KeyCode InventoryRightKey = KeyCode.RightArrow;
    public KeyCode InventoryRightAltKey = KeyCode.D;
    public KeyCode InventoryUpKey = KeyCode.UpArrow;
    public KeyCode InventoryUpAltKey = KeyCode.W;
    public KeyCode InventoryDownKey = KeyCode.DownArrow;
    public KeyCode InventoryDownAltKey = KeyCode.S;

    [Header("Quick Slots")]
    public KeyCode QuickSlot1Key = KeyCode.Alpha1;
    public KeyCode QuickSlot2Key = KeyCode.Alpha2;
    public KeyCode QuickSlot3Key = KeyCode.Alpha3;
    public KeyCode QuickSlot4Key = KeyCode.Alpha4;

    [Header("Player Movement")]
    public string HorizontalAxis = "Horizontal";
    public string VerticalAxis = "Vertical";
    public KeyCode SprintKey = KeyCode.LeftShift;

    [Header("Combat")]
    public int AimMouseButton = 1;
    public int FireMouseButton = 0;

    public static InputSystem Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public bool IsPausePressed() => Input.GetKeyDown(PauseKey);
    public bool IsInteractPressed() => Input.GetKeyDown(InteractKey);
    public bool IsInventoryPressed() => Input.GetKeyDown(InventoryKey);
    public bool IsReloadPressed() => Input.GetKeyDown(ReloadKey);
    public bool IsCrouchPressed() => Input.GetKeyDown(CrouchKey);
    public bool IsFlashlightPressed() => Input.GetKeyDown(FlashlightKey);
    public bool IsSortInventoryPressed() => Input.GetKeyDown(SortInventoryKey);
    public bool IsBuyMenuPressed() => Input.GetKeyDown(BuyMenuKey);
    public bool IsInventoryLeftPressed() => Input.GetKeyDown(InventoryLeftKey) || Input.GetKeyDown(InventoryLeftAltKey);
    public bool IsInventoryRightPressed() => Input.GetKeyDown(InventoryRightKey) || Input.GetKeyDown(InventoryRightAltKey);
    public bool IsInventoryUpPressed() => Input.GetKeyDown(InventoryUpKey) || Input.GetKeyDown(InventoryUpAltKey);
    public bool IsInventoryDownPressed() => Input.GetKeyDown(InventoryDownKey) || Input.GetKeyDown(InventoryDownAltKey);

    public bool IsQuickSlotPressed(int quickSlotIndex)
    {
        switch (quickSlotIndex)
        {
            case 0: return Input.GetKeyDown(QuickSlot1Key);
            case 1: return Input.GetKeyDown(QuickSlot2Key);
            case 2: return Input.GetKeyDown(QuickSlot3Key);
            case 3: return Input.GetKeyDown(QuickSlot4Key);
            default: return false;
        }
    }

    public float GetHorizontalInput() => Input.GetAxis(HorizontalAxis);
    public float GetVerticalInput() => Input.GetAxis(VerticalAxis);
    public bool IsSprintHeld() => Input.GetKey(SprintKey);
    public bool IsAimPressed() => Input.GetMouseButtonDown(AimMouseButton);
    public bool IsAimReleased() => Input.GetMouseButtonUp(AimMouseButton);
    public bool IsFirePressed() => Input.GetMouseButtonDown(FireMouseButton);
}