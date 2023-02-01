using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tools_Animator : MonoBehaviour
{
    [System.Serializable]
    public struct CustomAnimation
    {
        public string name;
        public Sprite[] sprites;
        public bool DoLoop;
        public bool startRandom;
    }

    public CustomAnimation[] Animations;
    private CustomAnimation CurrentAnimation;

    [Header("Settings")]
    public int FrameRate = 10;
    private float frameTimer;
    private int frameIndex;

    [Header("Assign Variables")]
    public SpriteRenderer spriteRenderer;

    void Update()
    {
        frameTimer += Time.deltaTime;
        if (frameTimer > 1f / FrameRate)
        {
            frameTimer -= 1f / FrameRate;
            OnTick();
        }
    }


    public void OnTick()
    {
        if (CurrentAnimation.sprites == null)
            return;

        spriteRenderer.sprite = CurrentAnimation.sprites[frameIndex];

        int spriteLength = CurrentAnimation.sprites.Length;

        if (frameIndex < spriteLength - 1)
            frameIndex++;
        else if (CurrentAnimation.DoLoop)
            frameIndex = 0;
    }

    public void Play(string name, bool forceReplay = false)
    {
        if (!forceReplay && CurrentAnimation.name == name)
            return;

        for (int i = 0; i < Animations.Length; i++)
            if (Animations[i].name == name)
            {
                CurrentAnimation = Animations[i];
                frameIndex = Animations[i].startRandom ? Random.Range(0, Animations[i].sprites.Length) : 0;
                return;
            }

        Debug.LogWarning("Could not find the animation");
    }

}
