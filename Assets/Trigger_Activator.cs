using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trigger_Activator : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        transform.tag = "Activator";
    }

    [System.Serializable]
    public class Event
    {
        public string name = "N/A";
        public GameObject gameObject;

    }

    public Event[] Events;

    public Tools_Sound.SoundClip[] soundClips;

    // Update is called once per frame
    void Update()
    {
        Tools_Sound.Start(soundClips, transform);
    }

    public void OnActivation(GameObject projectileVisuals, float originalScale)
    {
        transform.tag = "Untagged";

        CapsuleCollider collider = GetComponent<CapsuleCollider>();

        float height = Mathf.Max(collider.height, collider.radius) / collider.center.y;


        projectileVisuals = Instantiate(projectileVisuals);
        projectileVisuals.transform.localScale = Vector3.one * originalScale;
        projectileVisuals.transform.parent = transform;
        projectileVisuals.transform.localPosition = Vector3.up * collider.center.y;


        Tools_Sound.Play(soundClips, Tools_Sound.SoundFlags.OnUse);


        foreach (Event currentEvent in Events)
        {
            if (currentEvent.gameObject != null)
                currentEvent.gameObject.SetActive(!currentEvent.gameObject.active);
        }

    }
}
