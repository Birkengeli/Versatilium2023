using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows;
using UnityEngine.XR;
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
        isStatic = 1 << 5,
        staticAim = 1 << 6,
        alwaysShooting = 1 << 7,
        ignoresPlayer = 1 << 8,
        startsPassive = 1 << 9,
        doesNotChasePlayer = 1 << 10,
        CUSTOM_AdditionalTurretBehaviors = 1 << 11,
    }

    [System.Flags]
    public enum AnimationFlag
    {

        Walking = 1 << 1,
        Walking_Combat = 1 << 2,
        Death = 1 << 3,
        Idle = 1 << 4,

        Turning = 1 << 5,

        WakingUp = 1 << 6,
        GoingToSleep = 1 << 7,
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
    Vector3 velocity_LastFrame;
    float timer;

    Transform eye;

    Animator anim;
    Controller_Character.StatusEffect StatusEffects = Controller_Character.StatusEffect.None;

    public Tools_Sound.SoundClip[] soundClips;

    #region Start
    void Start()
    {
        StartPos = transform.position;
        currentTargetLocation = StartPos;

        player = GameObject.FindGameObjectWithTag("Player").transform;

        anim = GetComponentInChildren<Animator>();

        foreach (Transform child in transform)
        {
            eye = child;
            if (eye.position == Vector3.zero)
                break;
        }

        Tools_Sound.Start(soundClips, transform);

        if (HasFlag((int)BehaviorTags, (int)BehaviorTag.startsPassive))
            BehaviorTags = (BehaviorTag)ApplyFlag((int)BehaviorTags, (int)BehaviorTag.ignoresPlayer);

    }
    #endregion

    #region Update
    void Update()
    {
        float timeStep = Time.deltaTime;

        bool canSeePlayer = CanSeeTarget(player, false) && !HasFlag((int)BehaviorTags, (int)BehaviorTag.ignoresPlayer);
        bool isStatic = HasFlag((int)BehaviorTags, (int)BehaviorTag.isStatic);
        bool custom_isTurret = HasFlag((int)BehaviorTags, (int)BehaviorTag.CUSTOM_AdditionalTurretBehaviors);

        if (BehaviorStates == BehaviorState.Idle)
        {
            whileIdle(timeStep, false);

            if (canSeePlayer) // On Reacting
            {
                BehaviorStates = BehaviorState.Reacting;

                if (HasFlag((int)AnimationFlags, (int)AnimationFlag.WakingUp))
                    anim.Play("Armature|Turret_Errection_Up", 2);
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


            if (!canSeePlayer) // On Searching
            {
                BehaviorStates = BehaviorState.Searching;
                timer = rememberPlayerFor;

            }
        }

        if (BehaviorStates == BehaviorState.Searching)
        {
            timer -= timeStep;

            whileSearching(timeStep);

            if (canSeePlayer) // On Returning to Active
            {
                BehaviorStates = BehaviorState.InCombat;
            }

            if (!canSeePlayer && timer < 0) // On retracting
            {
                BehaviorStates = BehaviorState.ReturningToIdle;
                timer = ReactionTime;

                if (HasFlag((int)AnimationFlags, (int)AnimationFlag.GoingToSleep))
                    anim.Play("Armature|Turret_Errection_Down", 2);
                if (HasFlag((int)AnimationFlags, (int)AnimationFlag.Turning))
                {
                    anim.Play("Armature|Turret_Pitch", 0, 0);
                    anim.Play("Armature|Turret_Yaw", 1, 0);
                }
            }
        }

        if (BehaviorStates == BehaviorState.ReturningToIdle)
        {
            timer -= timeStep;

            if (timer < 0) // on Sleep
            {
                BehaviorStates = BehaviorState.Idle;
                timer = 0;

                whileIdle(timeStep, true);
            }
        }


        if (!isStatic)
        {
            ApplyNautralForces(timeStep, true); // Friction first.
            Movement(timeStep, currentTargetLocation);
            CollisionCheck(timeStep, velocity);
        }
        Animation(timeStep);

        velocity_LastFrame = velocity;


        if (transform.position.y < -100)
        {
            Debug.LogError("An enemy fell out of the map and was automaticaly killed and disabled. ('" + transform.name + "' at position " + StartPos + ")");
            OnHit(true);
            gameObject.SetActive(false);
        }
    }

    #endregion

    #region Behavior
    void whileIdle(float timeStep, bool onIdle)
    {

        bool canMove = HasFlag((int)BehaviorTags, (int)BehaviorTag.canMove);

        if (canMove)
        {
            bool onRandomMove = Random.Range(0f, 1f) < (timeStep / moveFrequency);
            if (onRandomMove)
            {
                currentTargetLocation = WanderThowards(transform.position, moveSpeed * moveFrequency);
                //transform.LookAt(currentTargetLocation);

            }

            TurnTowards(timeStep, currentTargetLocation, false, TurnSpeed);
        }
    }

    void whileInCombat(float timeStep, bool onInCombat)
    {
        bool canAttack = HasFlag((int)BehaviorTags, (int)BehaviorTag.canAttack);
        bool canMove = HasFlag((int)BehaviorTags, (int)BehaviorTag.canMove);
        bool isStatic = HasFlag((int)BehaviorTags, (int)BehaviorTag.isStatic);
        bool staticAim = HasFlag((int)BehaviorTags, (int)BehaviorTag.staticAim);
        bool alwaysShooting = HasFlag((int)BehaviorTags, (int)BehaviorTag.alwaysShooting);

        bool hasTurnAnimation = HasFlag((int)AnimationFlags, (int)AnimationFlag.Turning);

        #region Aquire Target

        Vector3 targetPosition = player.position + player.up * 0.75f;

        if (isLeadingTarget)
            targetPosition = Target_LeadShot(targetPosition, Weapon.WeaponStats.Projectile_Speed);



        #endregion

        #region Rotate Body & Eyes

        if (!staticAim && !isStatic)
        {
            // First I turn the body thowards the Target
            // While I do that, i turn the Eye back, so that it stands "still" relative to the world.
            Vector3 eyeEuler = eye.eulerAngles; // Global Euler
            TurnTowards(timeStep, targetPosition, false, TurnSpeed, true); // Make this return the difference?
            eye.eulerAngles = eyeEuler; // This ensures the eye is still relative to the world.
        }
        if (!staticAim)
            // Then I turn the actual Eye thowards the target
            TurnTowards(timeStep, targetPosition, true, TurnSpeed * 2, false);
        #endregion

        #region Assign Respective Animation
        Vector3 directionToTarget = targetPosition - transform.position;
        Vector3 directionToTarget_Horizontal = directionToTarget - Vector3.Project(directionToTarget, transform.up);

        float yaw = Vector3.Angle(-transform.forward, directionToTarget_Horizontal);
        bool isToTheRight = Vector3.Dot(directionToTarget, transform.right) > 0;
        float angleToTarget = 180f + (isToTheRight ? -yaw : yaw);

        float eye_X = eye.localEulerAngles.x;
        float alt_Pitch = Mathf.Clamp(360f - eye_X, 0, 45); // Using the eye transform. Much smoother but not as cool.

        float eye_Y = eye.localEulerAngles.y;
        float alt_Yaw = eye_Y >= 0 ? eye_Y : 360f + eye_Y; // Using the eye transform. Much smoother but not as cool.

        if (hasTurnAnimation)
        {
           anim.Play("Armature|Turret_Pitch", 0, alt_Pitch / 45f);
           anim.Play("Armature|Turret_Yaw", 1, alt_Yaw / 360f);
        }

        #endregion

        #region Fire
        bool targetInView = (1f - Vector3.Dot(eye.forward, directionToTarget)) * 180 < ConeOfFire;

        if(targetInView || alwaysShooting)
        {

            if(canAttack)
                Weapon.OnFire(Weapon_Versatilium.TriggerFlags.SemiAutomatic);

        }

        #endregion

        #region Combat Walking

        bool chasesPlayer = !HasFlag((int)BehaviorTags, (int)BehaviorTag.doesNotChasePlayer);

        if (canMove)
        {
            if (chasesPlayer)
            {


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
            }
            else // If it does not chase the player
            {
                bool onRandomMove = Random.Range(0f, 1f) < (timeStep / moveFrequency);
                if (onRandomMove)
                {
                    currentTargetLocation = WanderThowards(transform.position, moveSpeed * moveFrequency);
                    //transform.LookAt(currentTargetLocation);

                }

            }
        }
        #endregion
    }

    void whileSearching(float timeStep)
    {

    }

    #endregion

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
    void TurnTowards(float timeStep, Vector3 targetPosition, bool turnEye, float turnSpeed, bool yawOnly = false)
    {
        Transform currentTransform = turnEye ? eye : transform;

        if(yawOnly)
            targetPosition.y = currentTransform.position.y; // Make it only rotate on Z plane

        Vector3 directionToTarget = targetPosition - currentTransform.position;

        currentTransform.forward = Vector3.Slerp(currentTransform.forward, directionToTarget.normalized, turnSpeed * timeStep);
    }

    void Movement(float timeStep, Vector3 targetLocation)
    {
        bool canMove = HasFlag((int)BehaviorTags, (int)BehaviorTag.canMove) && !HasFlag((int)BehaviorTags, (int)BehaviorTag.isStatic);
        bool freezeMovement = HasStatusEffect(Controller_Character.StatusEffect.FreezeMovement);
        bool noInput = targetLocation == Vector3.zero || Vector3.Distance(transform.position, targetLocation) < 0.25f;
        bool isGrounded = IsGrounded();

        Vector3 locationToDirection = (targetLocation - transform.position).normalized;

        if (isGrounded && canMove && !freezeMovement && !noInput)
            velocity += locationToDirection * moveSpeed * friction * timeStep;
    }

    void ApplyNautralForces(float timeStep, bool andFriction)
    {
        bool isGrounded = IsGrounded();


        Vector3 verticalVelocity = Vector3.Project(velocity, transform.up);
        Vector3 horizontalVelocity = velocity - verticalVelocity;

        bool isAscending = Vector3.Dot(transform.up, verticalVelocity) > 0;

        if (!isGrounded)
            velocity += Physics.gravity * timeStep;

        if (isGrounded)
        {
            velocity -= horizontalVelocity * friction * timeStep;

            if (isGrounded && !isAscending)
                velocity -= verticalVelocity; // If grounded, stop falling
        }
    }



    void CollisionCheck(float timeStep, Vector3 currentVelocity)
    {
        CapsuleCollider capsule = GetComponent<CapsuleCollider>();

        bool isGrounded = IsGrounded();

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

            if (distanceToStartPos > wanderDistanceMax && false)         // It's not in LoS from start pos // This caused a near-infinite loop.
                endPosition = WanderThowards(currentPos, desiredDistance); // Then try again
        }


        return endPosition;


    }


    #endregion

    #region Health

    public void OnHit(bool onDeath)
    {
        bool startsPassive = HasFlag((int)BehaviorTags, (int)BehaviorTag.startsPassive);

        if (startsPassive)
           BehaviorTags = (BehaviorTag)ApplyFlag((int)BehaviorTags, (int)BehaviorTag.ignoresPlayer, true);

        Tools_Sound.Play(soundClips, Tools_Sound.SoundFlags.OnHit);


        if (onDeath)
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

    }
    #endregion

    #region Misc.
    bool CanSeeTarget(Transform target, bool mustBeUnobstructed)
    {
        Vector3 targetPosition = target.position;
        Vector3 directionToTarget = targetPosition - transform.position;

        float distanceToTarget = directionToTarget.magnitude;

        if (distanceToTarget > DetectionRange)
            return false;

        RaycastHit[] hits = Physics.RaycastAll(transform.position, (targetPosition - transform.position).normalized, distanceToTarget, ~new LayerMask(), QueryTriggerInteraction.Ignore);


        for (int i = 0; i < hits.Length; i++)
        {
            Transform currentTransform = hits[i].transform;

            bool isTarget = currentTransform == target;
            bool isOneWayShield = !mustBeUnobstructed && currentTransform.tag == "One Way Shield";
            bool isWorld = !isTarget && !isOneWayShield;

            if (isWorld)
                return false;

        }

        return true;
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

        bool canWalk = HasFlag((int)AnimationFlags, (int)AnimationFlag.Walking);
        bool hasIdle = HasFlag((int)AnimationFlags, (int)AnimationFlag.Idle);


        if (canWalk && onWalking)
            anim.Play("Walking", 0);

        if (hasIdle && onIdle)
            anim.Play("Idle", 0);
        #endregion
    }

    bool IsGrounded()
    {
        float characterHeight = GetComponent<CapsuleCollider>().height;

        RaycastHit hit;
        Physics.Raycast(transform.position, -transform.up, out hit, characterHeight / 2);

        return hit.transform != null;
    }

    #endregion

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

    public int ApplyFlag(int mask, int effect, bool removeFlag = false)
    {
        if (!removeFlag)
        {
            mask |= effect;
        }
        else
        {
            mask &= ~effect;
        }

        return mask;
    }

    #endregion
}
