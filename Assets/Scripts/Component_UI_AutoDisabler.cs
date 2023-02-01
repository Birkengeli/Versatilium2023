using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Component_UI_AutoDisabler : MonoBehaviour
{
    [Header("Time before this component automatically disables itself.")]
    public float countDown = 0;

    void Update()
    {
        if (countDown > 0)
        {
            countDown -= Time.deltaTime;

            if (countDown <= 0)
            {
                countDown = 0;
                gameObject.SetActive(false);
            }
        }
    }
}
