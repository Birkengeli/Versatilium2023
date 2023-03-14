using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Temp_ExplosiveEffect : MonoBehaviour
{
    public float lifeTime = 0.5f;
    float timer = 0;
    float startSize;
    void Start()
    {
        startSize = transform.localScale.magnitude;
        transform.localScale = Vector3.zero;
    }


    void Update()
    {
        timer += Time.deltaTime;

        transform.localScale = Vector3.one * (timer / lifeTime * startSize);

        if (timer > lifeTime)
            Destroy(gameObject);
    }
}
