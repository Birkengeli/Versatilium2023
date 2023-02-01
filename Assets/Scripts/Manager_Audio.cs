using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Sounds_Turret
{
    Unassigned,

    OnFire,
    OnActivate,
    OnRetract,
    OnDeath,
}

public enum Sounds_Generic
{
    Unassigned,

    Footsteps,
    ButtonPress,
    OnTakingDamage,
}

public enum Sounds_Weapon
{
    Unassigned,

    OnFire,
}

[System.Serializable]
public class Manager_Audio
{
    [Header("You usually only pick one of these")]
    public Sounds_Turret Turret;
    public Sounds_Generic Generic;
    public Sounds_Weapon Weapon;

    [Header("Example: 'event:/Character/Enemies/Turret_Small/Turret Retract'")]
    public string audioPath = "event:/Character/Enemies/Turret_Small/Turret Retract";

    public static void Play(Manager_Audio[] sounds, Sounds_Turret soundType, bool ignoreMissingSounds = false)
				{
        for (int i = 0; i < sounds.Length; i++)
        {
            if (sounds[i].Turret == soundType)
            {
                //FMODUnity.RuntimeManager.PlayOneShot(sounds[i].audioPath);
                Debug.Log("Removed FMOD functions for now.");

                return;
            }
        }

        if (!ignoreMissingSounds)
            Debug.LogWarning("Could Not find '" + soundType.ToString() + "' among the given Sounds.");
    }

    public static void Play(Manager_Audio[] sounds, Sounds_Generic soundType, bool ignoreMissingSounds = false)
    {
        if (sounds == null)
            return;

        for (int i = 0; i < sounds.Length; i++)
        {
            if (sounds[i].Generic == soundType)
            {
                //FMODUnity.RuntimeManager.PlayOneShot(sounds[i].audioPath);
                Debug.Log("Removed FMOD functions for now.");

                return;
            }
        }

        if (!ignoreMissingSounds)
            Debug.LogWarning("Could Not find '" + soundType.ToString() + "' among the given Sounds.");
    }

    public static void Play(Manager_Audio[] sounds, Sounds_Weapon soundType, bool ignoreMissingSounds = false)
    {
        for (int i = 0; i < sounds.Length; i++)
        {
            if (sounds[i].Weapon == soundType)
            {
                //FMODUnity.RuntimeManager.PlayOneShot(sounds[i].audioPath);
                Debug.Log("Removed FMOD functions for now.");

                return;
            }
        }

        if (!ignoreMissingSounds)
            Debug.LogWarning("Could Not find '" + soundType.ToString() + "' among the given Sounds.");
    }


    public static Manager_Audio Find(Manager_Audio[] sounds, Sounds_Turret soundType, bool ignoreMissingSounds = false)
    {
        for (int i = 0; i < sounds.Length; i++)
        {
            if (sounds[i].Turret == soundType)
            {
                return sounds[i];
            }
        }

        if (!ignoreMissingSounds)
            Debug.LogWarning("Could Not find '" + soundType.ToString() + "' among the given Sounds.");

        return null;
    }

    public static Manager_Audio Find(Manager_Audio[] sounds, Sounds_Weapon soundType, bool ignoreMissingSounds = false)
    {
        for (int i = 0; i < sounds.Length; i++)
        {
            if (sounds[i].Weapon == soundType)
            {
                return sounds[i];
            }
        }

        if (!ignoreMissingSounds)
            Debug.LogWarning("Could Not find '" + soundType.ToString() + "' among the given Sounds.");

        return null;
    }

    public static void Play_Manually(Manager_Audio sound)
    {
        if (sound == null)
            return;

        //FMODUnity.RuntimeManager.PlayOneShot(sounds[i].audioPath);
        Debug.Log("Removed FMOD functions for now.");
    }
}
