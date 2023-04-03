using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static Unity.IO.LowLevel.Unsafe.AsyncReadManagerMetrics;

public class Tools_Sound : MonoBehaviour
{
    #region Classes & Enums
    [System.Flags]
    public enum SoundFlags
    {
        None = 0 << 0,

        // Combat / Damage
        OnCombat = 1 << 9,
        OnHit = 1 << 1,
        OnHitInvincible = 1 << 7,
        OnDeath = 1 << 2,

        // Interaction / Shooting
        OnUse = 1 << 3,
        OnFire = 1 << 4,
        OnAltFire = 1 << 5,

        // Movement
        WhileMoving = 1 << 6,
        WhileIdle = 1 << 8,


    }

    public enum randomTypes
    {
        Sequential,
        NeverTwice,
        Random
    }

    [System.Serializable]
    public class SoundClip
    {
        public string description = "Unnamed";
        public SoundFlags soundFlags;
        public AudioClip[] audioClips;
        [Header("Settings")]
        public randomTypes random = randomTypes.NeverTwice;
        [Range(0.1f, 1.0f)]
        public float volumeSlider = 0.5f;

        [HideInInspector]
        public int lastIndex;
        [HideInInspector]
        public AudioSource audioSource;
    }
    #endregion

    public static void Start(SoundClip[] soundClips, Transform transform)
    {
        for (int i = 0; i < soundClips.Length; i++)
        {
            soundClips[i].audioSource = transform.GetComponent<AudioSource>();
        }
    }

    public static float Play(SoundClip[] soundClips, SoundFlags flag, bool warnAboutMissingSound = true)
    {
        for (int i = 0; i < soundClips.Length; i++)
        {
            if (HasFlag((int)soundClips[i].soundFlags, (int) flag))
            {
                if (soundClips[i].audioClips.Length != 0)
                    return PlayRandomClip(soundClips[i]);
                
                if(warnAboutMissingSound)
                    Debug.LogWarning("Did find '" + flag + "', but it has no audioClips.");
                return -1;
            }
        }

        if (warnAboutMissingSound)
            Debug.LogWarning("Could not find '" + flag + "' among the given SoundClips.");

        return -1;
    }

    static float PlayRandomClip(SoundClip soundClip)
    {
        if (soundClip.audioSource == null)
        {
            Debug.LogWarning("No Audio Source detected; Did you remember to run 'Tools_Sound.Start()'?");
            return -1f;
        }

        if (soundClip.audioClips.Length < 2) // If it's set to never twice and it's 1 or zero files, it will never find it.
            soundClip.random = randomTypes.Random;

        int clipIndex = -1;
        int clipCount = soundClip.audioClips.Length;

        #region Random Order
        if (soundClip.random == randomTypes.Random)
            clipIndex = Random.Range(0, clipCount);

        if (soundClip.random == randomTypes.NeverTwice)
        {
            int randomIndex = -1;
            while (true)
            {
                randomIndex = Random.Range(0, clipCount);

                if (randomIndex != soundClip.lastIndex)
                {
                    soundClip.lastIndex = randomIndex;
                    clipIndex = randomIndex;

                    break;
                }
            }
        }

        if (soundClip.random == randomTypes.Sequential)
        {
            if (soundClip.lastIndex >= clipCount)
                soundClip.lastIndex = 0;

            clipIndex = soundClip.lastIndex;
            soundClip.lastIndex++;
        }

        #endregion


        AudioClip audioClip = soundClip.audioClips[clipIndex];
        PlayManually(audioClip, soundClip.audioSource, soundClip.volumeSlider);

        return audioClip.length;
    }


    public static float PlayManually(AudioClip audioClip, AudioSource audioSource, float volume)
    {
        audioSource.PlayOneShot(audioClip, volume);
        return audioClip.length;
    }

    #region Misc tools
    static bool HasFlag(int mask, int effect)
    {
        return (mask & effect) != 0;
    }

    // I will need to play X sound
    // I'll need to play a sound manually.
    // Array, "name", ignoreMissingSound = false
    #endregion
}
