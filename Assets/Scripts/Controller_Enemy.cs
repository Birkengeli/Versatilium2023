using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Controller_Character;

public class Controller_Enemy : MonoBehaviour
{
    #region Enums & Classes
    [System.Serializable]
    public class Drop
    {
        [Header("The name is just to find it easily in the list, it has no effect.")]
        public string name = "Medkit";
        public GameObject prefab;
        [Range(0f, 1.0f)]
        public float dropRate = 0.20f;
    }


    [System.Flags]
    public enum BehaviorTag
    {
        Nothing = 1 << 0,
        canMove = 1 << 1,
        canAttack = 1 << 2,
        isInvincible = 1 << 3,
        hasAirControl = 1 << 1 | 1 << 4,
    }

    [System.Flags]
    public enum AnimationFlag
    {

        Walking = 1 << 1,
        Walking_Combat = 1 << 2,
        Death = 1 << 3,
        Idle = 1 << 4,
    }

    public enum BehaviorState
    {
        Idle,
        Reacting,
        InCombat,
        Searching,
        ReturningToIdle,
    }
    #endregion

    public BehaviorState BehaviorStates = BehaviorState.Idle;

    [Header("Movement")]
    public float moveSpeed = 2;
    public float friction = 10;
    public float TurnSpeed = 5;
    [HideInInspector] public Vector3 velocity;

    [Header("Wandering")]
    public int moveFrequency = 5;
    public int wanderDistanceMax = 50;
    public float ReactionTime = 1f;
    public float rememberPlayerFor = 3f;

    [Header("Attack")]
    public float DetectionRange = 10;
    public bool isLeadingTarget = true;
    public float ConeOfFire = 45f;
    public bool isInCombat;
    public Weapon_Versatilium Weapon;

    [Header("Settings")]
    public BehaviorTag BehaviorTags = BehaviorTag.Nothing;
    public AnimationFlag AnimationFlags;
    public Drop[] drops;
    public Transform player;


    Vector3 StartPos;
    Vector3 currentTargetLocation;
    float timer;

    Animator anim;
    Controller_Character.StatusEffect StatusEffects = Controller_Character.StatusEffect.None;

    public Tools_Sound.SoundClip[] soundClips;

    // Start is called before the first frame update
    void Start()
    {
        StartPos = transform.position;
        currentTargetLocation = StartPos;

        player = GameObject.FindGameObjectWithTag("Player").transform;

        anim = GetComponentInChildren<Animator>();

        Tools_Sound.Start(soundClips, transform);
    }

    // Update is called once per frame
    void Update()
    {
        float timeStep = Time.deltaTime;

        Vector3 targetPosition = player.position;
        Vector3 directionToTarget = targetPosition - transform.position;

        float distanceToTarget = directionToTarget.magnitude;

        RaycastHit hit;
        Physics.Raycast(transform.position, (targetPosition - transform.position).normalized, out hit, DetectionRange + 0.1f);

        bool canSeeTarget = hit.transform != null && hit.transform.tag == "Player";
        bool hasDetectedTarget = distanceToTarget <= DetectionRange;

        if (BehaviorStates == BehaviorState.Idle)
        {
            whileIdle(timeStep, hasDetectedTarget && canSeeTarget);

            if (hasDetectedTarget && canSeeTarget) // On Idle
            {
                BehaviorStates = BehaviorState.Reacting;
            }
        }

        if (BehaviorStates == BehaviorState.Reacting)
        {
            timer += timeStep;

            if (timer > ReactionTime) // On Active
            {
                BehaviorStates = BehaviorState.InCombat;
                timer = ReactionTime;
            }
        }

        if (BehaviorStates == BehaviorState.InCombat)
        {

            whileInCombat(timeStep, false);


            if (!canSeeTarget) // On Searching
            {
                BehaviorStates = BehaviorState.Searching;
                timer = rememberPlayerFor;

            }
        }

        if (BehaviorStates == BehaviorState.Searching)
        {
            timer -= timeStep;

            whileSearching(timeStep);

            if (canSeeTarget) // On Returning to Active
            {
                BehaviorStates = BehaviorState.InCombat;
            }

            if (!canSeeTarget && timer < 0) // On retracting
            {
                BehaviorStates = BehaviorState.ReturningToIdle;
                timer = ReactionTime;
            }
        }

        if (BehaviorStates == BehaviorState.ReturningToIdle)
        {
            timer -= timeStep;

            if (!canSeeTarget && timer < 0) // on Sleep
            {
                BehaviorStates = BehaviorState.Idle;
                timer = 0;
            }
        }



        Movement(timeStep, currentTargetLocation);
        manualCollision(timeStep, velocity);

        Animation(timeStep);

        velocity_LastFrame = velocity;
    }

    Vector3 velocity_LastFrame;

    void whileIdle(float timeStep, bool onIdle)
    {
        if (HasFlag((int)BehaviorTags, (int)BehaviorTag.canMove))
        {
            bool onRandomMove = Random.Range(0f, 1f) < (timeStep / moveFrequency);
            if (onRandomMove)
            {
                currentTargetLocation = WanderThowards(transform.position, moveSpeed * moveFrequency);
                //transform.LookAt(currentTargetLocation);

            }

            TurnTowards(timeStep, currentTargetLocation, TurnSpeed);
        }
    }

    void whileInCombat(float timeStep, bool onInCombat)
    {
        #region Targeting

        Vector3 targetPosition = player.position;

        if (isLeadingTarget)
            targetPosition = Target_LeadShot(player.position, Weapon.WeaponStats.Projectile_Speed);



        #endregion

        TurnTowards(timeStep, targetPosition, TurnSpeed);


        #region Aiming
        Vector3 directionToTarget = targetPosition - transform.position;
        Vector3 directionToTarget_Horizontal = directionToTarget - Vector3.Project(directionToTarget, transform.up);

        float yaw = Vector3.Angle(-transform.forward, directionToTarget_Horizontal);
        bool isToTheRight = Vector3.Dot(directionToTarget, transform.right) > 0;
        float angleToTarget = (180f + (isToTheRight ? -yaw : yaw)) / 360f;

        #endregion

        #region Fire
        bool targetInView = angleToTarget < ConeOfFire;

        if(targetInView)
        {

            transform.LookAt(targetPosition); // Back to the player.

            TurnTowards(timeStep, targetPosition, TurnSpeed);

            // I need to fire the Versatilium not using look, but look + angle offset

            Weapon.OnFire(Weapon_Versatilium.TriggerFlags.SemiAutomatic);

        }

        #endregion

        #region Combat Walking
        // Where can I go?
        float distanceToWall_Right = 0;
        float distanceToWall_Left = 0;

        for (int i = 0; i < 2; i++)
        {
            RaycastHit hit;
            Physics.Raycast(transform.position, transform.right * (i == 0 ? 1 : -1), out hit);

            if (hit.transform != null)
            {
                if (i == 0)
                    distanceToWall_Right = hit.distance;
                else
                    distanceToWall_Left = hit.distance;
            }

        }


        currentTargetLocation = transform.position + (transform.forward + transform.right * (distanceToWall_Right > 1 ? 1 : 0)).normalized * moveSpeed;
        currentTargetLocation = targetPosition;


        #endregion


    }

    void whileSearching(float timeStep)
    {
        Vector3 targetPosition = player.position; // Probably should be last Seen position

        TurnTowards(timeStep, targetPosition, TurnSpeed);
        currentTargetLocation = targetPosition;
    }

    void Animation(float timeStep)
    {
        #region Animation

        float currentSpeed = velocity.magnitude;
        float previousSpeed = velocity_LastFrame.magnitude;


        bool isIdle = currentSpeed <= 0.1f;
        bool onIdle = isIdle && previousSpeed > 0.1f;

        bool isWalking = currentSpeed > 0.1f;
        bool onWalking = isWalking && previousSpeed <= 0.1f;


        if (onIdle)
            print("OnIdle");

        if (onWalking)
            print("onWalking");

        bool canWalk = HasFlag((int)AnimationFlags, (int)AnimationFlag.Walking);
        bool hasIdle = HasFlag((int)AnimationFlags, (int)AnimationFlag.Idle);


        if (canWalk && onWalking)
            anim.Play("Walking", 0);

        if (hasIdle && onIdle)
            anim.Play("Idle", 0);
        #endregion
    }


	#region Lead Shots
    Vector3 targetPosition_Previous;

    Vector3 Target_LeadShot(Vector3 targetPosition_Current, float projectileSpeed) // I believe my inconsistent framerate causes some leading issues.
    {
        float timeStep = Time.deltaTime;

        Vector3 targetVelocity = targetPosition_Current - targetPosition_Previous;
        float targetSpeed = targetVelocity.magnitude / timeStep;
        float targetDistance = Vector3.Distance(targetPosition_Current, transform.position);

        float timeToCrossDistance = targetDistance / projectileSpeed;
        float oneTickAhead = (1 + timeStep);
        Vector3 targetPosition_Lead = targetPosition_Current + targetVelocity.normalized * targetSpeed * timeToCrossDistance;

        if (false)
        {
            float crossSize = 1f / 2f;

            Vector3 debugPosition = targetPosition_Lead;

            Debug.DrawRay(debugPosition - Vector3.forward * crossSize, Vector3.forward * crossSize * 2, Color.red);
            Debug.DrawRay(debugPosition - Vector3.right * crossSize, Vector3.right * crossSize * 2, Color.blue);
            Debug.DrawRay(debugPosition - Vector3.up * crossSize, Vector3.up * crossSize * 2, Color.green);
        }

        targetPosition_Previous = targetPosition_Current;

        return targetPosition_Lead;

    }

    #endregion

    #region Movement
    void TurnTowards(float timeStep, Vector3 targetPosition, float turnSpeed)
    {
        targetPosition.y = transform.position.y; // Make it only rotate on Z plane

        Vector3 directionToTarget = targetPosition - transform.position;

        transform.forward = Vector3.Slerp(transform.forward, directionToTarget.normalized, turnSpeed * timeStep);
    }

    void Movement(float timeStep, Vector3 targetLocation)
    {
        bool freezeMovement = HasStatusEffect(Controller_Character.StatusEffect.FreezeMovement);
        float characterHeight = 2;

        bool noInput = targetLocation == Vector3.zero || Vector3.Distance(transform.position, targetLocation) < 0.25f;

        RaycastHit hit;
        Physics.Raycast(transform.position, -Vector3.up, out hit, characterHeight / 2);
        bool isGrounded = hit.transform != null;
    
        Vector3 locationToDirection = (targetLocation - transform.position).normalized;

        Vector3 verticalVelocity = Vector3.Project(velocity, Vector3.down);
        Vector3 horizontalVelocity = velocity - verticalVelocity;

        bool isAscending = Vector3.Dot(Vector3.up, verticalVelocity) > 0;

        if (!isGrounded)
            velocity += Physics.gravity * timeStep;

        if (isGrounded)
        {
            velocity -= horizontalVelocity * friction * timeStep;

            if(isGrounded && !isAscending)
                velocity -= verticalVelocity; // If grounded, stop falling

            if (!freezeMovement && !noInput)
                velocity += locationToDirection * moveSpeed * friction * timeStep;
        }
    }

    void manualCollision(float timeStep, Vector3 currentVelocity)
    {
        CapsuleCollider capsule = GetComponent<CapsuleCollider>();

        bool isGrounded = true;

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
                    dir = dir - Vector3.Project(dir, transform.up); // This is relative horizontal, not relative to gravity.

                    transform.position = transform.position + dir * distance;

                    velocity -= Vector3.Project(currentVelocity, dir); // Removes the velocity once impacting with a wall.
                }
            }
        }
    }



    Vector3 WanderThowards(Vector3 currentPos, float desiredDistance = 999)
    {
        float randomAngle = Random.Range(0, 360);
        Vector3 direction = Quaternion.AngleAxis(randomAngle, Vector3.up) * Vector3.forward;


        Physics.Raycast(currentPos, direction, out RaycastHit hit);

        float currentDistance = (hit.transform != null && hit.distance < desiredDistance) ? (hit.distance - 0.5f) : desiredDistance; // I remove a little, to avoid clipping.

        Debug.DrawRay(currentPos, direction, Color.green, 4);

        Vector3 endPosition = transform.position + (direction * currentDistance);



        if (true && wanderDistanceMax < 100)
        {

            Vector3 directionToStartPos = (StartPos - endPosition).normalized;
            float distanceToStartPos = Vector3.Distance(endPosition, StartPos);

            Physics.Raycast(endPosition, directionToStartPos, out RaycastHit hit2, wanderDistanceMax);

            Debug.DrawRay(endPosition, directionToStartPos * distanceToStartPos, Color.blue, 4);

            //print("" + (hit2.transform != null ? hit2.transform.name : "NULL") + ": " + hit2.distance + ", " + Vector3.Distance(endPosition, StartPos) + ".");

            bool hitValidTarget = hit2.transform != null && hit2.transform != transform;

            if (distanceToStartPos > wanderDistanceMax)         // It's not in LoS from start pos
                endPosition = WanderThowards(currentPos, desiredDistance); // Then try again
        }


        return endPosition;


    }


    #endregion

    public void onDeath()
    {

        bool hasDeathAnimation = HasFlag((int)AnimationFlags, (int)AnimationFlag.Death);

        if (hasDeathAnimation)
            anim.Play("Death", 0);
        else
            transform.GetChild(0).position += Vector3.down * 100;

        Tools_Sound.Play(soundClips, Tools_Sound.SoundFlags.OnDeath);

        foreach (Drop drop in drops)
        {
            float randomNumber = Random.Range(0f, 1f);

            if (randomNumber <= drop.dropRate)
            {
                GameObject dropObject = Instantiate(drop.prefab);
                dropObject.transform.position = transform.position + Vector3.up * 0.1f;
                
            }
        }

    }

    #region Tools

    public void ApplyStatusEffect(Controller_Character.StatusEffect effect, bool removeEffectInstead = false)
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


    void ApplyFlag(BehaviorTag effect, bool removeEffectInstead = false)
    {
        if (!removeEffectInstead)
        {
            BehaviorTags |= effect;
        }
        else
        {
            BehaviorTags &= ~effect;
        }

    }


    public bool HasStatusEffect(Controller_Character.StatusEffect effect)
    {
        return (StatusEffects & effect) != 0;
    }

    public bool HasFlag(int mask, int effect)
    {
        return (mask & effect) != 0;
    }

    #endregion
}
