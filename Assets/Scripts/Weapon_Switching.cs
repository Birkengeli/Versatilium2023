using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Weapon_Switching : MonoBehaviour
{
    #region Enums & Classes
    public enum WeaponBuildingModes
    {
        CoreBased, Freeform
    }

    [System.Serializable]
    public class Module
    {
        public enum ModuleSlots
        {
            SubCore, Core
        }
        public enum Availability
        {
            isDisabled, isLocked, isAvailable,
        }

        public string name = "Example Core";

        public ModuleSlots moduleSlot = ModuleSlots.SubCore;

        public Availability state = Availability.isAvailable;

        [Header("Cores")]
        public Color coreColor = Color.cyan;
        [HideInInspector] public int[] storedIndexes;
        public Sprite Icon;

    }
    #endregion

    [Header("Must Be Assigned")]
    public GameObject Canvas_WeaponSwitch;
    Controller_Character playerScript;
    Weapon_Versatilium weaponScript;


    [Header("Settings")]
    public WeaponBuildingModes WeaponBuildingMode = WeaponBuildingModes.CoreBased;
    public KeyCode InventoryKey = KeyCode.Tab;
    public Sound[] sounds;

    [Header("Settings - Visuals")]
    public int SlowTimeBy = 10;
    public Color tintColor = Color.blue;
    public float tintFadeTime = 0.333f;
    public Sprite missingIcon;

    [Header("Modules")]
    public bool allowDuplicates;
    public Module[] Modules;

    #region Start & Update
    void Start()
    {
        playerScript = GetComponent<Controller_Character>();
        weaponScript = GetComponent<Weapon_Versatilium>();

        ToggleUI(false);

        ResetCores();
    }

    void ResetCores(bool resetSubCoresOnly = false)
    {
        int mainCoreIndex = customizeableSlots.Length - 1;

        if (!resetSubCoresOnly)
        {
            ApplyModule(mainCoreIndex, 0);
        }

        for (int i = 0; i < mainCoreIndex; i++)
        {
            customizeableSlots[i] = 0;
            Button_OnClick(i * 2 + 1);
        }


    }

    // Update is called once per frame
    void Update()
    {
        bool onInventoryDown = Input.GetKeyDown(InventoryKey);
        bool WhileInventoryDown = Input.GetKey(InventoryKey);
        bool onInventoryRelease = Input.GetKeyUp(InventoryKey);

        if (onInventoryDown || WhileInventoryDown || onInventoryRelease)
            OpenInventory((onInventoryDown ? 1 : 0) + (onInventoryRelease ? 2 : 0));

        if(true) // Control Maincores with mousewheel
        {
            int mainCoreIndex = customizeableSlots.Length - 1;

            int mouseWheel = (int)Input.mouseScrollDelta.y;

            if (mouseWheel != 0)
                Button_OnClick(mainCoreIndex * 2 + (mouseWheel == 1 ? 1 : 0));
        }
    }

	#endregion

	void OpenInventory(int keyState)
    {
        bool onOpenInventory = keyState == 1;
        bool whileHoldingInventory = keyState == 0;
        bool onClosingInventory = keyState == 2;

        if (onOpenInventory)
        {
            Controller_Spectator.LockCursor(false);
            Time.timeScale = (1f / SlowTimeBy);

            playerScript.ApplyStatusEffect(Controller_Character.StatusEffect.FreezeCamera);
            playerScript.ApplyStatusEffect(Controller_Character.StatusEffect.DisableShooting);

            ToggleUI(true);
        }

        if (whileHoldingInventory)
            InventoryUI();

        if (onClosingInventory)
        {
            int weaponWheelIndex = InventoryUI();

            Controller_Spectator.LockCursor(true);
            Time.timeScale = 1;
            playerScript.ApplyStatusEffect(Controller_Character.StatusEffect.FreezeCamera, true);
            playerScript.ApplyStatusEffect(Controller_Character.StatusEffect.DisableShooting, true);

            ToggleUI(false);
        }
    }

    int[] customizeableSlots = new int[4] { 0, 0, 0, 0 };



    public Image[] UI_Cores;
    public void Button_OnClick(int buttonIndex)
    {
        int currentSlotIndex = Mathf.FloorToInt(buttonIndex / 2);
        int inputDirection = (buttonIndex - (currentSlotIndex * 2)) == 1 ? 1 : -1;

        bool isMainCore = currentSlotIndex > customizeableSlots.Length - 2;

        { // Get the correct core based on the cutimizeAbleSlotIndex.
            int currentIndex = customizeableSlots[currentSlotIndex];


            if (isMainCore && WeaponBuildingMode == WeaponBuildingModes.CoreBased)
                StoreIndexes(Modules[currentIndex], false);

            if (GetNextAvailableModule(currentIndex, inputDirection != 1, isMainCore ? Module.ModuleSlots.Core : Module.ModuleSlots.SubCore, out int newIndex, out Module nextModule))
            {


                ApplyModule(currentSlotIndex, newIndex);

                if (isMainCore && WeaponBuildingMode == WeaponBuildingModes.CoreBased)
                    StoreIndexes(nextModule, true);

                Debug.Log("Swapped module to " + nextModule.name + " at position '" + newIndex + "'.");
            }
        }



        //Debug.Log("Increased Slot '" + currentSlotIndex + "' by " + inputDirection + ". The slot now contains '" + customizeableSlots[currentSlotIndex] + "'.");
    }

    void ApplyModule(int slotIndex, int moduleIndex)
    {
        customizeableSlots[slotIndex] = moduleIndex;
        Module currentModule = Modules[moduleIndex];

        #region Apply UI
        if (UI_Cores.Length >= slotIndex)
        {
            UI_Cores[slotIndex].sprite = currentModule.Icon != null ? currentModule.Icon : missingIcon;
        }
        #endregion
    }

    bool GetNextAvailableModule(int currentIndex, bool reverse, Module.ModuleSlots slotType, out int nextIndex, out Module nextModule)
    {
        for (int i = 0; i < Modules.Length; i++)
        {
            #region Next Index
            currentIndex += (reverse ? -1 : 1);

            if (currentIndex == Modules.Length)
                currentIndex -= Modules.Length;
            if (currentIndex == -1)
                currentIndex += Modules.Length;
            #endregion

            nextModule = Modules[currentIndex];

            bool isCorrectSlot = nextModule.moduleSlot == slotType;
            bool isUnlocked = nextModule.state == Module.Availability.isAvailable;
            bool isInUse = isModuleInUse(currentIndex);

            if (isCorrectSlot && isUnlocked && (allowDuplicates || !isInUse))
            {
                nextIndex = currentIndex;
                return true;
            }

        }
        Debug.LogError("Could not find a valid module.");

        nextIndex = -1;
        nextModule = null;
        return false;
    }





    #region Tools

    void StoreIndexes(Module currentModule, bool loadIndexes)
    {
        if (currentModule.storedIndexes.Length != customizeableSlots.Length)
        {
            currentModule.storedIndexes = new int[customizeableSlots.Length];
            Debug.Log("Loaded main core for the first time");

            loadIndexes = false; // If there is nothing to load, then I might as well just save.
        }

        if (!loadIndexes)
        {
            for (int i = 0; i < customizeableSlots.Length; i++)
            {
                currentModule.storedIndexes[i] = customizeableSlots[i];
            }
        }
        else
        {
            for (int i = 0; i < customizeableSlots.Length - 1; i++)
                ApplyModule(i, currentModule.storedIndexes[i]);
            
        }

    }

    void ToggleUI(bool activate, bool instantFade = false)
    {
        if (Canvas_WeaponSwitch != null)
        {
            Canvas_WeaponSwitch.SetActive(true); // Always turn on.

            Image backgroundImage = Canvas_WeaponSwitch.GetComponent<Image>();

            backgroundImage.color = tintColor;
            backgroundImage.CrossFadeColor(activate ? tintColor : Color.clear, instantFade ? 0 : tintFadeTime, true, true);

            GameObject foreGround = Canvas_WeaponSwitch.transform.GetChild(0).gameObject;
            foreGround.SetActive(activate);
        }
    }

    bool isModuleInUse(int moduleIndex)
    {
        for (int i = 0; i < customizeableSlots.Length; i++)
            if (customizeableSlots[i] == moduleIndex)
                return true;

        return false;
    }

    int InventoryUI()
    {

        Vector2 cursorPosition = Input.mousePosition;
        Vector2 screenCenter = new Vector2(Screen.width, Screen.height) / 2;
        Vector2 offsetFromCenter = (cursorPosition - screenCenter);
        float distanceFromCenter = offsetFromCenter.magnitude / Screen.height;
        offsetFromCenter = offsetFromCenter.normalized;

        float angle = Mathf.Atan2(offsetFromCenter.y, offsetFromCenter.x) * Mathf.Rad2Deg;
        if (angle < 0)
            angle += 360;

       
        return 0;

    }

    public static Transform GetChildByName(string name, Transform parent)
    {
        Transform[] transforms = parent.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform currentTransform = transforms[i];

            if (currentTransform.name == name)
                return currentTransform;
        }
        Debug.LogWarning("Error, could not find '" + name + "'.");

        return null;
    }

    #endregion
}
