using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Sound
{
    public enum SoundTypes_Weapon
    {
        Unassigned,

        OnFire,
        OnFire_Charged,
        OnReload,
    }

    public enum SoundTypes_Combat
    {
        Unassigned,

        OnTakingDamage,
        OnTakingDamageWhileInvincible,

        OnPickupHealth,
    }

    public enum SoundTypes_Environmental
    {
        Unassigned,

        OnActivation,
        WhileActivated,

        Footsteps_Generic,
    }

    public enum SoundTypes_UI
    {
        Unassigned,

        OnPressButton,
        OnWeaponSelect,
        OnWeaponSelectLocked,

    }

    [Header("You usually only pick one of these")]
    public SoundTypes_Weapon Weapon;
    public SoundTypes_Combat Combat;
    public SoundTypes_Environmental Enviormental;
    public SoundTypes_UI UI;
    public AudioClip[] audioClips;
    [Header("Reminder: Not every script needs every Sound filled out.")]
    [Header("Settings")]
    public randomTypes random = randomTypes.NeverTwice;
    [Range(0.1f, 2.0f)]
    public float volumeSlider = 1;

    private int lastIndex = 999;

				#region Play function
				public static float Play(SoundTypes_Combat soundType, Sound[] sounds, AudioSource audioSource, bool ignoreMissing = false)
    {
        for (int i = 0; i < sounds.Length; i++)
        {
            if (sounds[i].Combat == soundType)
            {
                return Play(sounds[i], audioSource);
            
            }
        }

        if (!ignoreMissing)
            Debug.LogWarning("Could Not find '" + soundType.ToString() + "' among the given Sounds.");

        return 1;

    }

    public static float Play(SoundTypes_Environmental soundType, Sound[] sounds, AudioSource audioSource, bool ignoreMissing = false)
    {
        for (int i = 0; i < sounds.Length; i++)
        {
            if (sounds[i].Enviormental == soundType)
            {
                return Play(sounds[i], audioSource);
            }
        }

        if (!ignoreMissing)
            Debug.LogWarning("Could Not find '" + soundType.ToString() + "' among the given Sounds.");

        return 1;
    }

    public static float Play(SoundTypes_Weapon soundType, Sound[] sounds, AudioSource audioSource, bool ignoreMissing = false)
    {
        for (int i = 0; i < sounds.Length; i++)
        {
            if (sounds[i].Weapon == soundType)
            {
                return Play(sounds[i], audioSource);
                
            }
        }

        if (!ignoreMissing)
            Debug.LogWarning("Could Not find '" + soundType.ToString() + "' among the given Sounds.");

        return 1;
    }

    public static float Play(SoundTypes_UI soundType, Sound[] sounds, AudioSource audioSource, bool ignoreMissing = false)
    {
        for (int i = 0; i < sounds.Length; i++)
        {
            if (sounds[i].UI == soundType)
            {
                return Play(sounds[i], audioSource);

            }
        }

        if (!ignoreMissing)
            Debug.LogWarning("Could Not find '" + soundType.ToString() + "' among the given Sounds.");

        return 1;
    }

    #endregion

    public static float Play(Sound sound, AudioSource audioSource)
    {
        AudioClip clip = sound.audioClips[0];

        if (sound.volumeSlider == 0)
            sound.volumeSlider = 1; // It will never be 0 (minimum is 0.1) so this is a quickfix.

        if (sound.audioClips.Length < 2) // If it's set to never twice and it's 1 or zero files, it will never find it.
            sound.random = randomTypes.Random;

        #region Random Order
        if (sound.random == randomTypes.Random)
            clip = sound.audioClips[Random.Range(0, sound.audioClips.Length)];

        if (sound.random == randomTypes.NeverTwice)
        {
            int randomIndex = -1;
            while (true)
            {
                randomIndex = Random.Range(0, sound.audioClips.Length);

                if (randomIndex != sound.lastIndex)
                {
                    sound.lastIndex = randomIndex;
                    clip = sound.audioClips[randomIndex];

                    break;
                }
            }
        }

        if (sound.random == randomTypes.Sequential)
        {
            int clipCount = sound.audioClips.Length;

            if (sound.lastIndex >= clipCount)
                sound.lastIndex = 0;

            clip = sound.audioClips[sound.lastIndex];
            sound.lastIndex++;
        }

        #endregion

        audioSource.PlayOneShot(clip, sound.volumeSlider);

        return clip.length;
    }

    public enum randomTypes
    { Sequential, NeverTwice, Random }

}
