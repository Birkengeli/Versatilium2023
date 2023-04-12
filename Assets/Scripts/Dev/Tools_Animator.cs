using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Tools_Animator : MonoBehaviour
{
    [System.Serializable]
    public struct CustomAnimation
    {
        public string name;
        public Sprite[] sprites;
        public bool DoLoop;
        public bool deleteAtEnd;
        public bool startRandom;
    }

    public CustomAnimation[] Animations;
    [HideInInspector] public CustomAnimation CurrentAnimation;

    [Header("Settings")]
    public int FrameRate = 10;
    private float frameTimer;
    [HideInInspector] public int frameIndex;
    public bool playOnAwake = true;

    [Header("Assign Variables")]
    public SpriteRenderer spriteRenderer;

    private void Awake()
    {
        if (playOnAwake && Animations.Length > 0)
            Play(Animations[0].name);
        
    }
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
        else if (CurrentAnimation.deleteAtEnd)
            Destroy(gameObject);
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
