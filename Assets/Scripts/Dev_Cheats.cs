using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dev_Cheats : MonoBehaviour
{

    public int codeIndex = 0;
    [System.Serializable]
    public class Cheat
    {
        public string name;
        public string descriptionOptional;
        public bool countAsCheating = false;
        [Header("Incase a code must quickly be disabled (but not deleted):")]
        public bool isDisabled = false;
        [HideInInspector] public KeyCode[] keyCodes;
        [HideInInspector] public int keyCodeIndex = 0;
    }

    public Cheat[] Cheats;

    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < Cheats.Length; i++)
            Cheats[i].keyCodes = StringToKeyCode(Cheats[i].name.ToUpper());
        
    }

    // Update is called once per frame
    void Update()
    {
								if (Input.anyKeyDown)
        {
            for (int i = 0; i < Cheats.Length; i++)
            {
                RecogniseKey(i);
            }
        }

    }

    void RecogniseKey(int index)
    {

        Cheat currentCheat = Cheats[index];

        KeyCode currentKeyCode = currentCheat.keyCodes[currentCheat.keyCodeIndex];

        if (Input.GetKeyDown(currentKeyCode))
        {
            currentCheat.keyCodeIndex++;
        }
        else
            currentCheat.keyCodeIndex = 0;

        if (currentCheat.keyCodeIndex == currentCheat.keyCodes.Length)
        {
            CodeCompleted(index);
            currentCheat.keyCodeIndex = 0;
        }
    }

    KeyCode[] StringToKeyCode(string message)
    {
        KeyCode[] keyCodes = new KeyCode[message.Length];

        for (int i = 0; i < message.Length; i++)
        {
            keyCodes[i] = (KeyCode)System.Enum.Parse(typeof(KeyCode), ("" + message[i]));
        }

        return keyCodes;
    }

    public void CodeCompleted(int index)
    {
        string name = Cheats[index].name.ToLower();

        bool isDisabled = Cheats[index].isDisabled;


        print("Code Activated: '" + Cheats[index].name + "'." + (isDisabled ? "This code has however been manually disabled." : ""));

        if (isDisabled)
        {
            return;
        }

            Transform player = GameObject.FindGameObjectWithTag("Player").transform;

        if (name == "kill")
        {
            player.GetComponent<Component_Health>().WhileDead(true);
        }

        if (name == "unlock")
        {
            Weapon_Arsenal arsenal = player.GetComponent<Weapon_Arsenal>();

            for (int i = 0; i < arsenal.weaponConfigs.Length; i++)
            {
                arsenal.weaponConfigs[i].isUnlocked = true;

            }
        }

        if (name == "konami" || name == "wwssadadba")
        {
            player.GetComponent<Component_Health>().healthCurrent = 30000;
            player.GetComponent<Component_Health>().OnTakingDamage(0, Vector3.zero);
        }

        if (name == "teleport" || name == "tp")
        {
            Transform playerEyes = player.GetComponentInChildren<Camera>().transform;

            Physics.Raycast(playerEyes.position, playerEyes.forward, out RaycastHit hit);

            if (hit.transform != null)
            {
                Vector3 newLocation = hit.point + Vector3.up + playerEyes.forward * -0.5f;

                player.position = newLocation;
            }
        }

        if (name == "pain")
        {
            Component_Health.Get(player).OnTakingDamage(Random.Range(10, 99), Vector3.zero);
        }
    }
}
