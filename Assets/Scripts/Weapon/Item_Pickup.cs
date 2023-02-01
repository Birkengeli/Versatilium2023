using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item_Pickup : MonoBehaviour
{

    public enum Pickup
    {
        GiveVersatilium, RemoveVersatilium, GiveConfig
    }

    public bool debugMode = true;
    float orbitTimer = 0;

    [Header("Pickup")]
    public Pickup pickupType = Pickup.GiveConfig;
    public string[] ModuleNames = new string[2] { "Option A", "Option B" };

    [Header("Settings")]
    public float distance = 2f;

    bool isActive;
    Transform playerTransform;

    void Start()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        isActive = playerTransform != null;
    }

    void Update()
    {
        Vector3 center = transform.position;

        if (debugMode)
        {
            orbitTimer += Time.deltaTime * 360;

            Vector3 sideWay = Quaternion.AngleAxis(orbitTimer, Vector3.up) * Vector3.right;
            Debug.DrawRay(center, sideWay * distance, Color.red, Time.deltaTime);
        }

        if(isActive && Vector3.Distance(center, playerTransform.position) < distance)
								{
            // On Pickup

            OnPickup();

								}
    }


    void OnPickup()
    {

        Weapon_Versatilium weapon = playerTransform.GetComponent<Weapon_Versatilium>();
        Weapon_Arsenal arsenal = playerTransform.GetComponent<Weapon_Arsenal>();


        if (pickupType == Pickup.GiveVersatilium || pickupType == Pickup.RemoveVersatilium)
        {
            bool giveWeapon = pickupType == Pickup.GiveVersatilium;

            weapon.enabled = giveWeapon;

            if (giveWeapon)
            {
                playerTransform.GetComponent<AudioSource>().PlayOneShot(arsenal.switchSound);
            }                



            Transform[] transforms = playerTransform.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name == "Weapon_Versatilium")
                {
                    transforms[i].parent.gameObject.SetActive(giveWeapon);


                    break;
                }
            }
        }

        if (pickupType == Pickup.GiveConfig)
        {
            int optionLength = ModuleNames.Length;

            Weapon_Arsenal.WeaponConfiguration[] configs = new Weapon_Arsenal.WeaponConfiguration[optionLength];


            for (int i = 0; i < optionLength; i++)
            {

                for (int j = 0; j < arsenal.weaponConfigs.Length; j++)
                {
                    Weapon_Arsenal.WeaponConfiguration currentConfig = arsenal.weaponConfigs[j];

                    if (currentConfig.name.ToLower() == ModuleNames[i].ToLower())
                    {
                        configs[i] = currentConfig;

                        break;
                    }

                    if (j == arsenal.weaponConfigs.Length - 1)
                    {
                        Debug.LogWarning("Could not find a config called '" + ModuleNames[j] + "'.");
                        return;
                    }
                }
            }

            arsenal.ShowCards(configs);


        }

            Destroy(gameObject);
    }
}
