using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder.Shapes;
using static Unity.IO.LowLevel.Unsafe.AsyncReadManagerMetrics;
using static Weapon_Pickup;

[RequireComponent(typeof(SpriteRenderer))]

[RequireComponent(typeof(AudioSource))]

public class Health_Pickup : MonoBehaviour
{
    [Range(0, 100)]
    public int Healing = 50;

    public Tools_Sound.SoundClip sound;

    Transform player;
    Component_Health healthScript;

    float distance;

    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        healthScript = player.GetComponent<Component_Health>();

        UnityEngine.Sprite sprite = GetComponent<SpriteRenderer>().sprite;
        distance = sprite.rect.size.x / sprite.pixelsPerUnit / 2 * transform.localScale.x;

        Tools_Sound.Start(new Tools_Sound.SoundClip[1] { sound }, transform);

    }

    // Update is called once per frame
    void Update()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer < distance && distance != -1)
        {
            OnPickup();
            GetComponent<SpriteRenderer>().enabled = false;

            distance = -1;
        }   
    }

    void OnPickup()
    {
        healthScript.healthCurrent += Healing;
        if(healthScript.healthCurrent > 100)
            healthScript.healthCurrent = 100;

        float duration = Tools_Sound.Play(new Tools_Sound.SoundClip[1] { sound }, Tools_Sound.SoundFlags.OnUse);
        Destroy(gameObject, duration);

    }
}
