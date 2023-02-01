using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Weapon_Switching : MonoBehaviour
{
    #region Enums

    public enum WeaponBuildingModes
    {
        Premade, Modular
    }

    #endregion



    Controller_Character playerScript;
				Weapon_Versatilium weaponScript;
    Weapon_Arsenal arsenalScript;

    GameObject canvas;
    Image tint;
    Image weaponWheel;
    Image weaponWheel_Hover;

    [Header("Settings")]
    public WeaponBuildingModes WeaponBuildingMode = WeaponBuildingModes.Premade;
    public KeyCode InventoryKey = KeyCode.Tab;
    public Sound[] sounds;

    [Header("Settings - Visuals")]
    public int SlowTimeBy = 10;
    public Color tintColor = Color.blue;
    public float tintAlphaOverride = 0.5f;
    public Color selectColor = Color.black;
    public float tintFadeTime = 1;
    public float deadZone = 0.15f;

    #region Start & Update
    void Start()
    {
        playerScript = GetComponent<Controller_Character>();
        weaponScript = GetComponent<Weapon_Versatilium>();
        arsenalScript = GetComponent<Weapon_Arsenal>();

        canvas = GameObject.Find("_Canvas");
        if (canvas == null)
            Debug.LogWarning("Could not find the '_Canvas' prefab");

        tint = GetChildByName("UI_Tint", canvas.transform).GetComponent<Image>();

        tintColor.a = tintAlphaOverride;
        Color tintClear = tintColor;
        tintClear.a = 0;
        tint.CrossFadeColor(tintClear, tintFadeTime, true, true);

        weaponWheel = GetChildByName("UI_Weapon_Wheel", canvas.transform).GetComponent<Image>();
        weaponWheel.CrossFadeColor(tintClear, tintFadeTime, true, true);

        weaponWheel_Hover = GetChildByName("UI_Weapon_Wheel_Hover", canvas.transform).GetComponent<Image>();
        weaponWheel_Hover.CrossFadeColor(selectColor + new Color(0, 0, 0, -1), tintFadeTime, true, true);

        weaponWheel.transform.parent.gameObject.SetActive(false);

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
        if (WeaponBuildingMode == WeaponBuildingModes.Premade)
        {
            if (keyState == 1) // On Opening
            {
                Controller_Spectator.LockCursor(false);
                Time.timeScale = (1f / SlowTimeBy);
                playerScript.ApplyStatusEffect(Controller_Character.StatusEffect.FreezeCamera);
                playerScript.ApplyStatusEffect(Controller_Character.StatusEffect.DisableShooting);

                tint.gameObject.SetActive(true);
                tintColor.a = tintAlphaOverride;
                tint.color = tintColor;
                tint.CrossFadeColor(tintColor, tintFadeTime, true, true);

                weaponWheel.CrossFadeColor(Color.white, tintFadeTime / 2, true, true);
                weaponWheel_Hover.CrossFadeColor(selectColor, tintFadeTime / 2, true, true);

                weaponWheel.transform.parent.gameObject.SetActive(true);

                int iconCount = CountVisibleWeapons();

                int iconIndex = 0;
                for (int i = 0; i < arsenalScript.weaponConfigs.Length; i++)
                {
                    if (!arsenalScript.weaponConfigs[i].isUnlocked)
                        continue;

                    GameObject baseIcon = weaponWheel.transform.parent.GetChild(1).gameObject;
                    baseIcon.SetActive(false);

                    GameObject newIcon = Instantiate(baseIcon, weaponWheel.transform.parent);
                    newIcon.SetActive(true);

                    newIcon.GetComponent<Image>().sprite = arsenalScript.weaponConfigs[i].icon;
                    newIcon.GetComponent<Image>().color = Color.white * (arsenalScript.weaponConfigs[i].isUnlocked ? 1 : 0.5f);

                    float itemAngle = (360f / iconCount) * iconIndex + (180 / iconCount); // "180 / iconCount" should ensure icons stay in the middle.
                    Vector2 IconPos = Quaternion.AngleAxis(itemAngle, Vector3.forward) * Vector3.right;
                    newIcon.transform.localPosition = IconPos * 100;

                    iconIndex++;
                }


            }

            if (keyState == 0)
                InventoryUI();



            if (keyState == 2)
            {
                for (int i = 3; i < weaponWheel.transform.parent.childCount; i++)
                    Destroy(weaponWheel.transform.parent.GetChild(i).gameObject); // Destroy old Icons



                int weaponWheelIndex = InventoryUI();

																#region Turn Wheel Index into Arsenal Index
																for (int i = 0; i < arsenalScript.weaponConfigs.Length; i++)
                {
                    bool isUnlocked = arsenalScript.weaponConfigs[i].isUnlocked;


                    if (isUnlocked && weaponWheelIndex == 0)
                    {
                        weaponWheelIndex = i;
                        break;
                    }

                    if (isUnlocked)
                        weaponWheelIndex--;
                }
																#endregion

																arsenalScript.SwitchWeapon(arsenalScript.weaponConfigs[weaponWheelIndex]);


                Controller_Spectator.LockCursor(true);
                Time.timeScale = 1;
                playerScript.ApplyStatusEffect(Controller_Character.StatusEffect.FreezeCamera, true);
                playerScript.ApplyStatusEffect(Controller_Character.StatusEffect.DisableShooting, true);

                Color tintClear = tintColor;
                tintClear.a = 0;
                tint.CrossFadeColor(tintClear, tintFadeTime, true, true);

                weaponWheel.CrossFadeColor(tintClear, tintFadeTime / 2, true, true);
                weaponWheel_Hover.CrossFadeColor(selectColor + new Color(0,0,0,-1), tintFadeTime / 2, true, true);

                weaponWheel.transform.parent.gameObject.SetActive(false);
            }
        }
    }

    int CountVisibleWeapons()
    {

        int counter = 0;
        int iconCount = arsenalScript.weaponConfigs.Length;

        for (int i = 0; i < iconCount; i++)
        {
            if(arsenalScript.weaponConfigs[i].isUnlocked)
                counter++;
        }


        return counter;
    }

    int InventoryUI()
    {
        int iconCount = CountVisibleWeapons();

        Vector2 cursorPosition = Input.mousePosition;
        Vector2 screenCenter = new Vector2(Screen.width, Screen.height) / 2;
        Vector2 offsetFromCenter = (cursorPosition - screenCenter);
        float distanceFromCenter = offsetFromCenter.magnitude / Screen.height;
        offsetFromCenter = offsetFromCenter.normalized;

        float angle = Mathf.Atan2(offsetFromCenter.y, offsetFromCenter.x) * Mathf.Rad2Deg;
        if (angle < 0)
            angle += 360;

        float sectionSize = 360f / iconCount;
        int sectionIndex = Mathf.FloorToInt(angle / sectionSize);

        if (distanceFromCenter > deadZone) // Deadzone
        {
            weaponWheel_Hover.fillAmount = 1f / iconCount;
            weaponWheel_Hover.transform.eulerAngles = Vector3.forward * sectionSize * sectionIndex;
        }

        return sectionIndex;

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
