using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tools_Sprite : MonoBehaviour
{
    public Transform CameraTransform;
    SpriteRenderer spriteRenderer;

    [Header("Sprite Settings")]
    public float spritePlaySpeed = 10;
    private float spritePlayTimer = 0;
    private int spritePlayIndex = 0;

    // Start is called before the first frame update
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (CameraTransform == null)
            CameraTransform = Camera.main.transform;
    }

    // Update is called once per frame
    void Update()
    {
        SpritePerspective();
    }


    void SpritePerspective()
    {
        spriteRenderer.transform.forward = -CameraTransform.forward; // The Actual renderer always faces me.
        spritePlayTimer += Time.deltaTime;

    }
}
