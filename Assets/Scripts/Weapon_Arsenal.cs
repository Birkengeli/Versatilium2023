using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Weapon_Arsenal : MonoBehaviour
{

    public enum SlotType
    {
        Pistol = 0,
        Shotgun = 1,
        Rifle = 2,
        Plasma = 3,

    }

    [System.Serializable]
    public class WeaponConfiguration
    {
        public string name;
        public string[] Description = new string[2] { "This is the first line", "This is the second line" };
        public Sprite icon;
        public bool isUnlocked;

        public SlotType WeaponSlot;

        public Weapon_Versatilium.WeaponStatistics statistics;
    }

    public WeaponConfiguration[] weaponConfigs;


    [Header("Settings")]
    public int SlowTimeBy = 10;
    public bool useMouseWheel = false;
    public float switchCooldown = 1;
    private float switchCooldown_Timer;
    public AudioClip switchSound;

    [Header("Settings - UI")]
    public float CardTiltMax = 10;
    public float CardDistance = 150;

    private int weaponCurrentIndex = 0;

    Weapon_Versatilium Versatilium;
    Controller_Character playerScript;

    void Start()
    {
        Versatilium = GetComponent<Weapon_Versatilium>();
        playerScript = GetComponent<Controller_Character>();

        Versatilium.WeaponStats = weaponConfigs[weaponCurrentIndex].statistics;
        switchCooldown_Timer = switchCooldown;

        Transform baseScreen = Weapon_Switching.GetChildByName("UI_Upgrade", GameObject.Find("_Canvas").transform);
        Transform baseCard = baseScreen.GetChild(0);

        baseCard.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        int scrollDirection = Input.GetAxis("Mouse ScrollWheel") > 0 ? 1 : 0 + Input.GetAxis("Mouse ScrollWheel") < 0 ? -1 : 0;

        if (useMouseWheel && scrollDirection != 0 && switchCooldown_Timer == -1)
        {

            while (true)
            {
                weaponCurrentIndex += scrollDirection;

                if (weaponCurrentIndex == weaponConfigs.Length)
                    weaponCurrentIndex = 0;

                if (weaponCurrentIndex == -1)
                    weaponCurrentIndex = weaponConfigs.Length - 1;

                if (weaponConfigs[0].isUnlocked == false)
                {
                    weaponConfigs[0].isUnlocked = true;
                    Debug.LogWarning("Please do not lock the pistol, if there is no available weapons the code breaks.");
                }

                if(weaponConfigs[weaponCurrentIndex].isUnlocked)
                    break;
            }


            SwitchWeapon(weaponConfigs[weaponCurrentIndex]);
        }


        if (switchCooldown_Timer > 0)
            switchCooldown_Timer -= Time.deltaTime;
        else
            switchCooldown_Timer = -1;


    }


    public void ShowCards(WeaponConfiguration[] Options)
    {
        Transform baseScreen = Weapon_Switching.GetChildByName("UI_Upgrade", GameObject.Find("_Canvas").transform);
        Transform baseCard = baseScreen.GetChild(0);
        int upgradeCount = Options.Length;

        playerScript.ApplyStatusEffect(Controller_Character.StatusEffect.PlayerIsInMenu);
        Controller_Spectator.LockCursor(false);

        Time.timeScale = 1f / SlowTimeBy;

        for (int i = 1; i < baseScreen.childCount; i++)
        {
            GameObject currentCard = baseScreen.GetChild(i).gameObject;
            currentCard.transform.SetParent(null);
            Destroy(currentCard);
            currentCard.SetActive(false);
        }

        for (int i = 0; i < upgradeCount; i++)
        {
            GameObject currentCard = Instantiate(baseCard, baseScreen).gameObject;
            currentCard.gameObject.SetActive(true);

            int buttonIndex = i;
            currentCard.GetComponent<Button>().onClick.AddListener(delegate { OnClickButton(buttonIndex, Options); });

            TMPro.TMP_Text currentCard_Text = currentCard.GetComponentInChildren<TMPro.TMP_Text>();

            string cardDescription = "";
            foreach (string description in Options[i].Description)
            {
                cardDescription += description + " \n";
            }

            currentCard_Text.text = cardDescription;

            float distanceFromCenter = -(upgradeCount / 2 - 0.5f) + i;

            currentCard.transform.localPosition = Vector3.zero + Vector3.right * distanceFromCenter * CardDistance;
            currentCard.transform.localEulerAngles = Vector3.forward * distanceFromCenter * -(CardTiltMax / upgradeCount);
        }
    }

    public void OnClickButton(int index, WeaponConfiguration[] Options)
    {
        Transform baseScreen = Weapon_Switching.GetChildByName("UI_Upgrade", GameObject.Find("_Canvas").transform);
        Transform baseCard = baseScreen.GetChild(0);

        playerScript.ApplyStatusEffect(Controller_Character.StatusEffect.PlayerIsInMenu, true);
        Controller_Spectator.LockCursor(true);

        Time.timeScale = 1;

        int originalAmountOfCards = baseScreen.childCount; // This keeps shrinking, and that is annoying.

        for (int i = 1; i < originalAmountOfCards; i++)
        {
            GameObject currentCard = baseScreen.GetChild(1).gameObject; // It's always 0 index
            currentCard.transform.SetParent(null);
            Destroy(currentCard);
        }


        Debug.LogWarning("I selected '" + Options[index].name + "' as my Upgrade.");

        SwitchWeapon(Options[index], true);
    }


  

    public void SwitchWeapon(WeaponConfiguration switchToConfig, bool unlockWeapon = false)
    {
        if (unlockWeapon)
        {
            // I Unlock all other versions of this weapon.
            for (int i = 1; i < weaponConfigs.Length; i++) // I start at 1, because I never intend to lock the pistol
            {
                bool isTheSameSlot = weaponConfigs[i].WeaponSlot == switchToConfig.WeaponSlot;
                bool isUnlocked = weaponConfigs[i].isUnlocked == true;

                if (isTheSameSlot && isUnlocked)
                    weaponConfigs[i].isUnlocked = false;
            }

            switchToConfig.isUnlocked = true;
        }


        if (Versatilium.WeaponStats == switchToConfig.statistics)
            return;

        Versatilium.fireRate_GlobalCD = 0.5f;


        Versatilium.WeaponStats = switchToConfig.statistics;
        switchCooldown_Timer = switchCooldown;


        GetComponent<AudioSource>().PlayOneShot(switchSound);
    }

}
