using System.Collections;
using System.Collections.Generic;
using Palmmedia.ReportGenerator.Core.Reporting.Builders;
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

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnActivation(GameObject projectileVisuals)
    {
        transform.tag = "Untagged";

        CapsuleCollider collider = GetComponent<CapsuleCollider>();

        float height = Mathf.Max(collider.height, collider.radius) / collider.center.y;


        projectileVisuals = Instantiate(projectileVisuals);
        projectileVisuals.transform.parent = transform;
        projectileVisuals.transform.localPosition = Vector3.up * height / 2;


        foreach (Event currentEvent in Events)
        {
            if (currentEvent.gameObject != null)
                currentEvent.gameObject.SetActive(!currentEvent.gameObject.active);
        }

    }
}
