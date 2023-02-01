using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller_Spectator : MonoBehaviour
{
    [Header("Movement Attributes")]
    public float speed = 10;
    public float friction = 2;
    public float sprintSpeedModifier = 2;

    [Header("Camera Attributes")]
    public float turnSpeed = 2;
    private Vector2 cameraEuler;

    [Header("Settings")]
    public KeyCode FreeCamera = KeyCode.F1;

    Camera camera;
    Vector3 velocity;

    void Start()
    {
        camera = GetComponent<Camera>();

        LockCursor(true);
    }

    // Update is called once per frame
    void Update()
    {
        float timeStep = Time.deltaTime;

        if(!Cursor.visible)
								{
            ControllMovement(timeStep);
            ControllCamera(timeStep);
								}

        if (Input.GetKeyDown(FreeCamera))
            LockCursor(false, true);
    }

    void ControllCamera(float timeStep)
				{
        float mouseX = Input.GetAxis("Mouse X") * turnSpeed;
        float mouseY = -Input.GetAxis("Mouse Y") * turnSpeed;

        cameraEuler += new Vector2(mouseY, mouseX);
        cameraEuler.x = Mathf.Clamp(cameraEuler.x, -90, 90);

        transform.eulerAngles = cameraEuler;
    }

				void ControllMovement(float timeStep)
				{
        float forward = (Input.GetKey(KeyCode.W) ? 1 : 0) + (Input.GetKey(KeyCode.S) ? -1 : 0);
        float sideways = (Input.GetKey(KeyCode.D) ? 1 : 0) + (Input.GetKey(KeyCode.A) ? -1 : 0);
        float speedModifier = Input.GetKey(KeyCode.LeftShift) ? sprintSpeedModifier : 1;

        Vector3 moveDirection = (transform.forward * forward + transform.right * sideways).normalized;

        velocity -= velocity * friction * timeStep;
        velocity += moveDirection * speed * speedModifier * friction * timeStep;

        transform.position += velocity * timeStep;
    }

				#region Tools
				public static void LockCursor(bool lockCursor, bool useToggleInstead = false)
    {
        if (useToggleInstead)
            lockCursor = Cursor.visible;

        Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !lockCursor;
    }
				#endregion
}
