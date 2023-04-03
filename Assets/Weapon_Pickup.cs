using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static Weapon_Versatilium;

public class Weapon_Pickup : MonoBehaviour
{
    [System.Flags]
    public enum PickupFlags // i think I should actually sort this in Weapon parts.
    {
        None = 0 << 0,

        StripAllCores = 1 << 0,

        GiveCore = 1 << 1,
        RemoveCore = 1 << 2,
    }

    public string coreName = "N/A";
    public PickupFlags flags;

    Weapon_Switching swapScript;

    float distance;
    Transform playerTransform;


    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        playerTransform = player.transform;
        swapScript = player.GetComponent<Weapon_Switching>();

        Weapon_Switching.Module currentModule = GetModule(coreName);

        Sprite sprite = currentModule != null ? currentModule.Icon : swapScript.missingIcon;
        GetComponent<SpriteRenderer>().sprite = sprite;
        distance = sprite.rect.size.x / sprite.pixelsPerUnit / 2;
        
        if(currentModule.state == Weapon_Switching.Module.Availability.isDisabled)
            gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 center = transform.position;


        if (Vector3.Distance(center, playerTransform.position) < distance)
        {
            // On Pickup

            OnPickup();

        }
    }

    void OnPickup()
    {
        gameObject.SetActive(false);


        if(HasFlag((int)flags, (int)PickupFlags.StripAllCores))
        {

            for (int i = 0; i < swapScript.Modules.Length; i++)
            {
                Weapon_Switching.Module currentModule = swapScript.Modules[i];

                if (currentModule.moduleSlot == Weapon_Switching.Module.ModuleSlots.SubCore && currentModule.state == Weapon_Switching.Module.Availability.isAvailable)
                    currentModule.state = Weapon_Switching.Module.Availability.isLocked;
            }

            GetModule("-empty-").state = Weapon_Switching.Module.Availability.isAvailable;
            

            for (int i = 0; i < swapScript.UI_Cores.Length - 1; i++)
            {
                swapScript.Button_OnClick(i * 2);
            }
        }

        if (HasFlag((int)flags, (int)PickupFlags.GiveCore))
        {
            GetModule(coreName).state = Weapon_Switching.Module.Availability.isAvailable;
        }

        if (HasFlag((int)flags, (int)PickupFlags.RemoveCore))
        {
            GetModule(coreName).state = Weapon_Switching.Module.Availability.isLocked;

            for (int i = 0; i < swapScript.UI_Cores.Length - 1; i++)
            {
                swapScript.Button_OnClick(i * 2);
            }
        }
    }

    Weapon_Switching.Module GetModule(string name)
    {
        name = name.ToLower();

        for (int i = 0; i < swapScript.Modules.Length; i++)
        {
            print(swapScript.Modules[i].name.ToLower());

            if (swapScript.Modules[i].name.ToLower() == name)
               return swapScript.Modules[i];
        }

        Debug.LogWarning("Could not find '" + name + "'.");
        return null;
    }
    static bool HasFlag(int mask, int effect)
    {
        return (mask & effect) != 0;
    }
}
