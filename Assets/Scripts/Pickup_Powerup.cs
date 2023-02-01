using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pickup_Powerup : MonoBehaviour
{
    Transform playerTransform;

    [Header("Settings")]
    public int HealAmount = 100;
    public int pickUpDistance = 2;

    public Sound[] sounds;
    bool isActive;


    void Start()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        isActive = playerTransform != null;
    }


    // Update is called once per frame
    void Update()
    {
        Vector3 center = transform.position;

        if (isActive && Vector3.Distance(center, playerTransform.position) < pickUpDistance)
        {
            // On Pickup

            OnPickup();

        }
    }

    void OnPickup()
    {

        Component_Health healthScipt = playerTransform.GetComponent<Component_Health>();
        healthScipt.OnHealing(HealAmount);

        isActive = false;
        Destroy(gameObject);
    }
}
