using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Component_Head : MonoBehaviour
{

    [Header("Options")]
    public float mouseyMultiplier = 8;
    public float walkMultiplier = 8;
    public float fallMultiplier = 8;
    public float smooth = 2;

    [Header("Assign")]
    public Transform viewmodel_head;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        

        Weapon_Drag(); // I might want a lesser version for HUD.
    }

    Vector3 previousForward;
    Vector3 viewVelocity;

    void Weapon_Drag()
    {
        int temp_RecoilSimulator = GetComponent<Weapon_Versatilium>().fireRate_GlobalCD > 0.3f ? 10 : 0;

        Vector3 velocity = GetComponent<Controller_Character>().velocity;
        Vector3 verticalVelocity = Vector3.Project(velocity, Vector3.down) * fallMultiplier;
        Vector3 forwardVelocity = Vector3.Project(velocity, transform.forward) * walkMultiplier;
        Vector3 strafeVelocity = Vector3.Project(velocity, transform.right) * walkMultiplier;

        float forwardSpeed = velocity.magnitude * Vector3.Dot(forwardVelocity, transform.forward);
        float strafeSpeed = velocity.magnitude * Vector3.Dot(strafeVelocity, transform.right);

        float mouseX = Input.GetAxis("Mouse X") * mouseyMultiplier + -temp_RecoilSimulator * 0.5f;
        float mouseY = -Input.GetAxis("Mouse Y") * mouseyMultiplier + temp_RecoilSimulator;

        Quaternion rotationX = Quaternion.AngleAxis(-mouseY + verticalVelocity.y + forwardSpeed, Vector3.right);
        Quaternion rotationY = Quaternion.AngleAxis(-mouseX + -strafeSpeed, Vector3.up);

        viewmodel_head.localRotation = Quaternion.Slerp(viewmodel_head.localRotation, rotationX * rotationY, smooth * Time.deltaTime);
    }
}
