using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Weapon_Switching : MonoBehaviour
{
    #region Enums

    public enum WeaponBuildingModes
    {
        CoreBased, Freeform
    }

    #endregion



    Controller_Character playerScript;
	Weapon_Versatilium weaponScript;
    Weapon_Arsenal arsenalScript;

    GameObject canvas;

    Image weaponWheel;
    Image weaponWheel_Hover;

    [Header("Must Be Assigned")]
    public GameObject Canvas_WeaponSwitch;


    [Header("Settings")]
    public WeaponBuildingModes WeaponBuildingMode = WeaponBuildingModes.CoreBased;
    public KeyCode InventoryKey = KeyCode.Tab;
    public Sound[] sounds;

    [Header("Settings - Visuals")]
    public int SlowTimeBy = 10;
    public Color tintColor = Color.blue;
    public float tintFadeTime = 1;

    #region Start & Update
    void Start()
    {
        playerScript = GetComponent<Controller_Character>();
        weaponScript = GetComponent<Weapon_Versatilium>();
        arsenalScript = GetComponent<Weapon_Arsenal>();

        canvas = GameObject.Find("_Canvas");
        if (canvas == null)
            Debug.LogWarning("Could not find the '_Canvas' prefab");

        if (Canvas_WeaponSwitch != null)
        {
            Image backgroundImage = Canvas_WeaponSwitch.GetComponent<Image>();

            backgroundImage.gameObject.SetActive(true);
            backgroundImage.color = tintColor;
            backgroundImage.CrossFadeColor(Color.clear, 0, true, true);
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
    }

				#endregion

	void OpenInventory(int keyState)
    {
        bool onOpenInventory = keyState == 1;
        bool whileHoldingInventory = keyState == 0;
        bool onClosingInventory = keyState == 2;

        if (onOpenInventory) // On Opening
        {
            Controller_Spectator.LockCursor(false);
            Time.timeScale = (1f / SlowTimeBy);

            playerScript.ApplyStatusEffect(Controller_Character.StatusEffect.FreezeCamera);
            playerScript.ApplyStatusEffect(Controller_Character.StatusEffect.DisableShooting);

            if (Canvas_WeaponSwitch != null)
            {
                Image backgroundImage = Canvas_WeaponSwitch.GetComponent<Image>();

                backgroundImage.gameObject.SetActive(true);
                backgroundImage.CrossFadeColor(tintColor, tintFadeTime, true, true);
            }

          


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



            if (Canvas_WeaponSwitch != null)
            {
                Image backgroundImage = Canvas_WeaponSwitch.GetComponent<Image>();

                backgroundImage.color = tintColor;
                backgroundImage.CrossFadeColor(Color.clear, tintFadeTime, true, true);
            }

        }
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
}
