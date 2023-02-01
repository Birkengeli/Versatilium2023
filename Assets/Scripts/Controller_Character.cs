using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller_Character : MonoBehaviour
{
    [System.Flags]
    public enum StatusEffect
    {
        None = 0 << 0,
        //DisableCamera_Tick = 1 << 2 | 1 << 0, // Does not play well with the sprite script.
        FreezeCamera = 1 << 1,
        FreezeMovement = 1 << 2,

        DisableShooting = 1 << 3,
        PlayerIsInMenu = 1 << 1 | 1 << 2 | 1 << 3,
    }

    public StatusEffect StatusEffects = StatusEffect.None;

    [Header("Movement Attributes")]
    public float speed = 5;
    public float friction = 10;
    public float sprintSpeedModifier = 2;
    public float jumpHeight = 2;
    public bool hasAirControl;
    public Vector3 velocity;
    private float characterHeight;



    [Header("Vertical Attributes")]

    public float gravity = 9.81f;
    public float terminalVelocity = 80.55f;
    public string fallingInformatiom = "[Read Only]";

    [Header("Camera Attributes")]
    public float turnSpeed = 2;
    private Vector2 cameraEuler;

    [Header("Settings")]
    public KeyCode FreeCamera = KeyCode.F1;
    public float FootstepFrequency = 1;

    Manager_Audio[] Sounds;

    Camera camera;

    void Start()
    {
        //Application.targetFrameRate = 60;

        camera = GetComponentInChildren<Camera>();

        Controller_Spectator.LockCursor(true);

        CapsuleCollider collider = GetComponent<CapsuleCollider>();

        characterHeight = Mathf.Max(collider.height, collider.radius * 1);
    }

    // Update is called once per frame
    void Update()
    {
        float timeStep = Time.deltaTime;

        ControllGravity(timeStep);

        ControllMovement(timeStep);
        ControllCamera(timeStep);

        manuallyCollision(timeStep, velocity);
        //transform.position += velocity * timeStep;

        Footsteps();

        if (Input.GetKeyDown(FreeCamera))
            Controller_Spectator.LockCursor(false, true);


        if (Input.GetKeyDown(KeyCode.X))
            Time.timeScale = 1f / 10;

        if (Input.GetKeyUp(KeyCode.X))
            Time.timeScale = 1f;

    }

    public void ApplyStatusEffect(StatusEffect effect, bool removeEffectInstead = false)
    {
        if (!removeEffectInstead)
        {
            StatusEffects |= effect;
        }
        else
        {
            StatusEffects &= ~effect;
        }

    }

    public bool HasStatusEffect(StatusEffect effect)
    {
        return (StatusEffects & effect) != 0;
    }

    void ControllCamera(float timeStep)
    {
        bool freezeCamera = HasStatusEffect(StatusEffect.FreezeCamera);

        if (freezeCamera)
            return;

        float mouseX = Input.GetAxis("Mouse X") * turnSpeed;
        float mouseY = -Input.GetAxis("Mouse Y") * turnSpeed;

        cameraEuler += new Vector2(mouseY, mouseX);
        cameraEuler.x = Mathf.Clamp(cameraEuler.x, -90, 90);

        camera.transform.localEulerAngles = Vector3.right * cameraEuler.x;
        transform.localEulerAngles = Vector3.up * cameraEuler.y;
    }
    void ControllMovement(float timeStep)
    {
        bool freezeMovement = HasStatusEffect(StatusEffect.FreezeMovement);

        RaycastHit hit;
        Physics.Raycast(transform.position, -transform.up, out hit, characterHeight / 2);

        Vector3 groundNormal = hit.transform != null ? hit.normal : Vector3.up;

        float forward = (Input.GetKey(KeyCode.W) ? 1 : 0) + (Input.GetKey(KeyCode.S) ? -1 : 0);
        float sideways = (Input.GetKey(KeyCode.D) ? 1 : 0) + (Input.GetKey(KeyCode.A) ? -1 : 0);
        float speedModifier = Input.GetKey(KeyCode.LeftShift) ? sprintSpeedModifier : 1;


        Vector3 groundForward = Vector3.Cross(transform.right, groundNormal);
        Vector3 groundRight = Vector3.Cross(groundNormal, groundForward);

        Vector3 moveDirection = (groundForward * forward + groundRight * sideways).normalized;

        Vector3 verticalVelocity = Vector3.Project(velocity, Vector3.down);
        Vector3 horizontalVelocity = velocity - verticalVelocity;

        if (hasAirControl || fallingInformatiom == "I am grounded.")
        {
            velocity -= horizontalVelocity * friction * timeStep;

            if (!freezeMovement)
                velocity += moveDirection * speed * speedModifier * friction * timeStep;
        }
    }

    void ControllGravity(float timeStep)
    {
        RaycastHit hit;
        Physics.Raycast(transform.position, -transform.up, out hit, characterHeight / 2 + 0.05f, ~new LayerMask(), QueryTriggerInteraction.Ignore);

        Vector3 verticalVelocity = Vector3.Project(velocity, Vector3.down);

        bool isGrounded = hit.transform != null && velocity.y < 0.1f;
        float gravityStep = gravity * timeStep;

        if (isGrounded)
        {
            velocity -= verticalVelocity;
            transform.position += Vector3.up * ((characterHeight / 2) - hit.distance); // Doesn't work well with a moving car

            if (false && hit.transform.tag == "Vehicle")
            {
                Rigidbody CarRB = hit.transform.GetComponent<Rigidbody>();

                Vector3 velDifference = CarRB.velocity - velocity;

                transform.position += CarRB.velocity * Time.fixedDeltaTime;

            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                float jumpStrength = Mathf.Sqrt(-2f * -gravity * jumpHeight); // The true strength of the jump
                velocity += Vector3.up * jumpStrength;
            }
        }
        else
        {
            //velocity -= verticalVelocity * gravityStep;
            //velocity += Vector3.down * terminalVelocity * gravityStep;

            velocity -= Vector3.up * gravityStep;
        }

        {
            float currentFallspeed = Mathf.Round(verticalVelocity.magnitude * 100) / 100;


            if (isGrounded)
                fallingInformatiom = "I am grounded.";
            else
                fallingInformatiom = "Fallspeed: " + currentFallspeed + " m/s.";
        }

    }

    void manuallyCollision(float timeStep, Vector3 currentVelocity)
    {
        CapsuleCollider capsule = GetComponent<CapsuleCollider>();

        float DistanceTimesSize = (currentVelocity.magnitude * timeStep) / (capsule.radius / 2); // The radius is divided by two to make it more accurate

        //		if(DistanceTimesSize > 1)
        //			Debug.LogWarning ("A player is moving at a speed of " +  Tools.RoundWithinReason(DistanceTimesSize, 2) * 50 + "% of their hitbox radius. (" + currentVelocity.magnitude * TimeStep + " u/tick)");


        int totalMoveCount = (int)Mathf.Ceil(DistanceTimesSize);
        if (totalMoveCount == 0)
            totalMoveCount = 1;
        int remainingMoveCount = totalMoveCount;

        while (remainingMoveCount > 0) // As long as it's not moving more than the radius of the collider.
        {
            if (remainingMoveCount > 10)
            {
                Debug.LogWarning("WARNING: The Player is moving at an extreme speed. Skipping " + remainingMoveCount + " physics frames for object.");

                remainingMoveCount = 0;
                totalMoveCount = 1;
            }

            // Movement
            remainingMoveCount -= 1;
            transform.position += (currentVelocity * timeStep) / totalMoveCount;

            // // Vertical location
            //RaycastHit EndHit;
            //Physics.Raycast(transform.position, -transform.up, out EndHit, (capsule.height / 2) + 1);

            //bool hasContact = EndHit.transform != null;
            //bool isBendingKnee = hasContact && EndHit.distance < capsule.height / 2;
            //float groundDistance = EndHit.distance;
            //bool isJustAbove = hasContact && isGrounded && groundDistance < capsule.height / 2 + CharacterSettings.GroundSnap;
            //bool isAscending = Vector3.Dot(Vector3.Project(currentVelocity, CharacterSettings.Upwards), CharacterSettings.Upwards) > 0;
            //
            //if (isBendingKnee && timeStep != 0)
            //    transform.position += transform.up * (CharacterSettings.PlayerHeight / 2 - EndHit.distance) * timeStep * stairSpeed;

            // Collisions
            Collider[] overlaps = new Collider[4];
            LayerMask IgnoreLayer = ~new LayerMask();
            Vector3 End = transform.position + transform.up * (capsule.height / 2 - capsule.radius);

            int lenght = Physics.OverlapSphereNonAlloc(End, capsule.radius * 1.5f, overlaps, IgnoreLayer, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < lenght; i++)
            {
                Transform t = overlaps[i].transform;

                if (t == transform)
                    continue;

                Vector3 dir;
                float distance;

                if (Physics.ComputePenetration(capsule, transform.position + transform.up * capsule.radius, transform.rotation, overlaps[i], t.position, t.rotation, out dir, out distance))
                {
                    //Vector3 dir_Vertical = Vector3.Project(dir, transform.up);
                    //dir = dir - dir_Vertical; // This is relative horizontal, not relative to gravity.

                    // So removing vertical velocity cause you to jump through walls. This solution works, but has other weird glithces

                    transform.position = transform.position + dir * distance;

                    velocity -= Vector3.Project(currentVelocity, dir); // Removes the velocity once impacting with a wall.
                }
            }
        }
    }


    float footstepCooldown = 1; // If I keep it at 1, I won't make a step instantly after spawning
    void Footsteps()
    {
        float currentSpeed = velocity.magnitude;
        bool isGrounded = Mathf.Round(velocity.y) == 0;

        if (footstepCooldown > 0)
            footstepCooldown -= Time.deltaTime * currentSpeed / speed;

        if (footstepCooldown <= 0 && isGrounded)
        {
            Manager_Audio.Play(Sounds, Sounds_Generic.Footsteps, true);

            footstepCooldown += FootstepFrequency;
        }
    }

    bool groundCheck()
    {
        float distanceToGround = characterHeight / 2 + 0.05f;
        bool isAscending = velocity.y > 0.05f;
        Vector3 verticalVelocity = Vector3.Project(velocity, Vector3.down);

        Physics.Raycast(transform.position, -transform.up, out RaycastHit hit, distanceToGround, ~new LayerMask(), QueryTriggerInteraction.Ignore);

        return hit.transform != null && !isAscending;
    }

}
