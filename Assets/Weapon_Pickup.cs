using System.Collections;
using System.Collections.Generic;
using TMPro;
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
        Collectible = 1 << 3,
    }

    public string coreName = "N/A";
    public PickupFlags flags;

    Weapon_Switching swapScript;

    float distance;
    Transform playerTransform;

    public TMP_Text UI_Collectible;

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        playerTransform = player.transform;
        swapScript = player.GetComponent<Weapon_Switching>();

        bool isCollectible = (HasFlag((int)flags, (int)PickupFlags.Collectible));
        if (isCollectible)
        {
            distance = 1f;


            if (UI_Collectible != null)
            {
                string[] words = UI_Collectible.text.Split('/');

                float current = int.Parse(words[0]);
                float total = int.Parse(words[1]);

                UI_Collectible.text = "0/"+ (total+1);
            }

            return;
        }

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
        float distanceToPlayer = Vector3.Distance(center, playerTransform.position);

        bool isCollectible = (HasFlag((int)flags, (int)PickupFlags.Collectible));

        if (isCollectible) Collectible(true, false);

        if (distanceToPlayer < distance)
        {
            // On Pickup

            if (isCollectible)
                Collectible(false, true);
            else
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

    float velocity;

    void Collectible(bool isInUpdate, bool onPickup)
    {
        bool isTriggered = transform.parent == playerTransform;
        if (onPickup)
        {
            transform.parent = playerTransform;
            transform.localPosition = Vector3.forward * 1;
            transform.forward = playerTransform.forward;

            Transform[] allChildren = GetComponentsInChildren<Transform>();
            foreach (Transform child in allChildren)
                child.gameObject.layer = LayerMask.NameToLayer("VisibleOnlyInFirstPerson");


            Destroy(gameObject, 1f);

            if (UI_Collectible != null)
            {
                string[] words = UI_Collectible.text.Split('/');

                float current = int.Parse(words[0]);
                float total = int.Parse(words[1]);

                UI_Collectible.text = "" + (current + 1) + "/" + total;
            }

            distance = 0;
            isTriggered = true;
        }


        if (isInUpdate && isTriggered)
        {
            velocity += 0.1f * Time.deltaTime;
            transform.eulerAngles += Vector3.up * velocity * Time.deltaTime;
            transform.localPosition += (Vector3.forward + Vector3.up) * velocity;

        }

    }


    Weapon_Switching.Module GetModule(string name)
    {
        name = name.ToLower();

        for (int i = 0; i < swapScript.Modules.Length; i++)
        {
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
