using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller_Enemy : MonoBehaviour
{

    [System.Serializable]
    public class Drop
    {
        [Header("The name is just to find it easily in the list, it has no effect.")]
        public string name = "Medkit";
        public GameObject prefab;
        [Range(0f, 1.0f)]
        public float dropRate = 0.20f;
    }


    public enum TurretStates
    {
        Sleeping,
        Erecting,
        Ready,
        Searching,
        Detracting,
    }



    public Manager_Audio[] Audio;


    public enum EnemyTypes
    {
        Turret, Humanoid
    }
    public bool debugMode = true;

    [Header("Attack")]
    public float DetectionRange = 10;
    public float AimSpeed = 100;
    public float ConeOfFire = 45;
    public bool isLeadingTarget = true;
    public Weapon_Versatilium Weapon;

    [Header("Settings")]
    public Drop[] drops;
    public EnemyTypes enemyType;
    public bool isInvincible;
    public Transform player;
    public Sound[] Sounds;
    public float rememberPlayerFor = 3f;
    private float rememberPlayer_Timer = -1;
    private bool stillRemembersPlayer { get { return rememberPlayer_Timer > 0; } }

    [Header("Turret Behavior")]
    public float ActivationTime = 1f;
    public float ActivationTime_Timer;
    public bool isRetraciting;
    public TurretStates TurretState;

    public Vector2 viewEuler;

    [Header("Humanoid Behavior")]
    public float moveSpeed = 2;
    public float friction = 10;
    Vector3 previousPosition;
    public bool isInCombat;
    Vector3 wanderTarget;
    public bool hasAirControl = true;

    [Header("Idle Behavior")]
    public int moveFrequency = 5;
    public int wanderMax = 50;
    Vector3 StartPos;

    private Vector3 player_LastKnownLocation;
    Animator anim;

    // Start is called before the first frame update
    void Start()
    {
        if (enemyType == EnemyTypes.Turret)
        {
            transform.GetChild(0).localPosition = -Vector3.forward * 0.9f;
            isInvincible = true;
        }

        StartPos = transform.position;
        wanderTarget = StartPos;

        player = GameObject.FindGameObjectWithTag("Player").transform;

        anim = GetComponentInChildren<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        float timeStep = Time.deltaTime;

        if (enemyType == EnemyTypes.Turret)
            TurretBehavior(timeStep);
        
        if (enemyType == EnemyTypes.Humanoid)
            HumanoidBehavior(timeStep);
    }

				#region Look At

				float LookAt(Vector3 targetPosition, Vector3 currentForward, Transform horizontalTransform, Transform verticalTransform)
    {
        /// I think it is possible to get the vertical need from a static point, so it doens't turn awkwardly and excessibvely while rotating

        Vector3 directionToTarget = targetPosition - horizontalTransform.position;

        float dot_Forward = Vector3.Dot(directionToTarget.normalized, -verticalTransform.up);
        float dot_Vertical = Vector3.Dot(verticalTransform.forward, directionToTarget.normalized);
        float dot_Horizontal = Vector3.Dot(horizontalTransform.right, directionToTarget.normalized);





        if (true)
        {
            bool isCloseEnough = dot_Forward > 0.999f;
            bool isToMyRight = dot_Horizontal < 0;
            bool isAboveMe = dot_Vertical < 0;

            float lerpKickIn = 1f * 0.01f;
            float lerpPercentage = dot_Forward > (1f - lerpKickIn) ? (1f - dot_Forward) / lerpKickIn : 1;

            float input_X = Time.deltaTime * AimSpeed * (isAboveMe ? 1 : -1);
            float input_Y = Time.deltaTime * AimSpeed * (isToMyRight ? 1 : -1);

            viewEuler += new Vector2(input_X, -input_Y) * lerpPercentage;
            viewEuler.x = Mathf.Clamp(viewEuler.x, -50, 50);

            verticalTransform.localEulerAngles = Vector3.right * viewEuler.x;
            horizontalTransform.localEulerAngles = Vector3.forward * viewEuler.y;
        }

        if (false && dot_Forward < 0.999f)
        {
            dot_Horizontal = Mathf.Round(dot_Horizontal * 100) / 100;

            bool isCloseEnough = dot_Forward > 0.999f;
            bool isToMyRight = dot_Horizontal <= 0;
            bool isAboveMe = dot_Vertical < 0;

            float horizontalDegrees = 180f - (1f - dot_Horizontal) * 180;

            print(Mathf.Round(horizontalDegrees) + " Degrees");

            float degreesASecond = AimSpeed * Time.deltaTime;
            float degreesOff = Mathf.Abs(horizontalDegrees);

            float input_X = Time.deltaTime * AimSpeed * (isAboveMe ? 1 : -1);
            float input_Y = Mathf.Min(degreesASecond, degreesOff) * (isToMyRight ? 1 : -1);

            input_X = 0; // Debug

            viewEuler += new Vector2(input_X, -input_Y);
            viewEuler.x = Mathf.Clamp(viewEuler.x, -50, 50);

            verticalTransform.localEulerAngles = Vector3.right * viewEuler.x;
            horizontalTransform.localEulerAngles = Vector3.forward * viewEuler.y;
        }

        return (1f - dot_Forward) * 180;
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

        if (debugMode)
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

    Controller_Character.StatusEffect StatusEffects = Controller_Character.StatusEffect.None;

    [HideInInspector]
    public Vector3 velocity;

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


    void TurretBehavior(float timeStep)
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        Transform Turret_Hinge = transform.GetChild(0);
        Transform Turret_Turret = Turret_Hinge.GetChild(0);

        RaycastHit hit;
        Physics.Raycast(transform.position, (player.position - transform.position).normalized, out hit, DetectionRange + 0.1f);

        bool canSeePlayer = hit.transform != null && hit.transform.tag == "Player";
        bool hasDetectedPlayer = distanceToPlayer <= DetectionRange;

        bool onDeploy = TurretState == TurretStates.Sleeping && hasDetectedPlayer && canSeePlayer;
        bool whileErecting = TurretState == TurretStates.Erecting;
        bool onReady = TurretState == TurretStates.Erecting && ActivationTime_Timer >= ActivationTime;
        bool whileReady = TurretState == TurretStates.Ready;
        bool whileSearching = TurretState == TurretStates.Searching;
        bool onRetracting = TurretState == TurretStates.Searching && rememberPlayer_Timer < 0;
        bool whileRetracting = TurretState == TurretStates.Detracting;
        bool onSleep = TurretState == TurretStates.Detracting && ActivationTime_Timer < 0;

        #region Deploy & Erecting
        if (onDeploy)
        {
            TurretState = TurretStates.Erecting;

            isInvincible = false;
            ActivationTime_Timer = 0;
            Manager_Audio.Play(Audio, Sounds_Turret.OnActivate);
        }

								if (whileErecting)
        {
            ActivationTime_Timer += timeStep;

            float newZ = (1f - (ActivationTime_Timer / ActivationTime)) * 0.9f;
            Turret_Hinge.localPosition = -Vector3.forward * newZ;
        }
								#endregion

								#region Ready
								if (onReady || whileReady)
        {
            if(onReady)
												{
                TurretState = TurretStates.Ready;
                ActivationTime_Timer = ActivationTime;
            }

            Vector3 playerPosition = player.position + Vector3.down * 0.5f * transform.localScale.y; // Shot at the camera

            player_LastKnownLocation = playerPosition;
            rememberPlayer_Timer = rememberPlayerFor;

            if (isLeadingTarget)
                playerPosition = Target_LeadShot(playerPosition, Weapon.WeaponStats.Primary.Projectile_Speed);

            float degreesOff = (1f - LookAt(playerPosition, Turret_Turret.forward, Turret_Hinge, Turret_Turret)) * 180;
            bool fire = false;

            if (degreesOff > ConeOfFire)
                fire = true;

            Weapon.OnFire(fire ? Weapon_Versatilium.TriggerTypes.SemiAutomatic : Weapon_Versatilium.TriggerTypes.None, Manager_Audio.Find(Audio, Sounds_Turret.OnFire));

            if (!canSeePlayer && !hasDetectedPlayer)
            {
                TurretState = TurretStates.Searching;
                rememberPlayer_Timer = rememberPlayerFor;
                player_LastKnownLocation = playerPosition;
            }

        }
        #endregion

        #region Searching
        if (whileSearching)
        {
            rememberPlayer_Timer -= timeStep;


            // If it loses sight of the player, it will keep on shooting until it gives up.
        
            float degreesOff = (1f - LookAt(player_LastKnownLocation, Turret_Turret.forward, Turret_Hinge, Turret_Turret)) * 180;
            bool fire = false;

            if (degreesOff > ConeOfFire)
                fire = true;

            Weapon.OnFire(fire ? Weapon_Versatilium.TriggerTypes.SemiAutomatic : Weapon_Versatilium.TriggerTypes.None, Manager_Audio.Find(Audio, Sounds_Turret.OnFire));

            if (hasDetectedPlayer)
            {
                TurretState = TurretStates.Ready;
                rememberPlayer_Timer = rememberPlayerFor;
            }

        }
        #endregion

        #region Retract and Sleep
        if (onRetracting || whileRetracting)
        {
            if (onRetracting)
            {
                TurretState = TurretStates.Detracting;
                Manager_Audio.Play(Audio, Sounds_Turret.OnRetract);
            }

            float degreesOffForward = LookAt(Turret_Hinge.position + -transform.up, Turret_Turret.forward, Turret_Hinge, Turret_Turret);
            if (degreesOffForward < 1)
            {
                ActivationTime_Timer -= timeStep;

                float newZ = (1f - (ActivationTime_Timer / ActivationTime)) * 0.9f;
                Turret_Hinge.localPosition = -Vector3.forward * newZ;
            }
        }

        if (onSleep)
        {
            isInvincible = true;
            ActivationTime_Timer = 0;
            TurretState = TurretStates.Sleeping;
        }
        #endregion
				}



				void HumanoidBehavior(float timeStep)
    {
        Vector3 direction = velocity.normalized;
        previousPosition = transform.position;

        Vector3 moveToThisLocation = transform.position;

        if (debugMode)
            Debug.DrawRay(transform.position, direction * 100, Color.red);

        #region Animation
        {
            float currentSpeed = velocity.magnitude / timeStep;
            float percentageMaxSpeeed = currentSpeed / moveSpeed;

            anim.SetFloat("Forward", percentageMaxSpeeed);
        }
        #endregion

        #region Movement
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        bool playerIsInDetectionRange = distanceToPlayer < DetectionRange;

        if (playerIsInDetectionRange || isInCombat)
        {
            if (!isInCombat)
            {
                isInCombat = true;
                anim.SetTrigger("onCombat");
            }

            Vector3 targetPosition = player.position;

												#region Aiming
												if (isLeadingTarget)
                targetPosition = Target_LeadShot(player.position, Weapon.WeaponStats.Primary.Projectile_Speed);


            transform.LookAt(targetPosition); // Back to the player.
            Weapon.OnFire(Weapon_Versatilium.TriggerTypes.SemiAutomatic);

            targetPosition.y = transform.position.y; // Stop it from mvoing down (Can still shoot down)

            #endregion

            #region Walking
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

         

												#endregion


            moveToThisLocation = transform.position + (transform.forward + transform.right * (distanceToWall_Right > 1 ? 1 : 0)).normalized * moveSpeed;
            moveToThisLocation = targetPosition;
        }

        if (!isInCombat && moveFrequency != 0)
        {
            bool onRandomMove = Random.Range(0f, 1f) < (timeStep / moveFrequency);

            if (onRandomMove)
            {
                wanderTarget = WanderThowards(transform.position, moveSpeed * moveFrequency);
                transform.LookAt(wanderTarget);
            }

            moveToThisLocation = wanderTarget;
        }

        #endregion

        Movement(timeStep, moveToThisLocation);
        manuallyCollision(timeStep, velocity);
    }


    public bool HasStatusEffect(Controller_Character.StatusEffect effect)
    {
        return (StatusEffects & effect) != 0;
    }
				#region Movement

				void Movement(float timeStep, Vector3 targetLocation)
    {
        bool freezeMovement = HasStatusEffect(Controller_Character.StatusEffect.FreezeMovement);
        float characterHeight = 2;

        RaycastHit hit;
        Physics.Raycast(transform.position, -Vector3.up, out hit, characterHeight / 2);
        bool isGrounded = hit.transform != null;
    
        Vector3 locationToDirection = (targetLocation - transform.position).normalized;

        Vector3 verticalVelocity = Vector3.Project(velocity, Vector3.down);
        Vector3 horizontalVelocity = velocity - verticalVelocity;

        bool isAscending = Vector3.Dot(Vector3.up, verticalVelocity) > 0;

        if (!isGrounded)
            velocity += Physics.gravity * timeStep;

        if (hasAirControl || isGrounded)
        {
            velocity -= horizontalVelocity * friction * timeStep;

            if(isGrounded && !isAscending)
                velocity -= verticalVelocity; // If grounded, stop falling

            if (!freezeMovement)
                velocity += locationToDirection * moveSpeed * friction * timeStep;
        }

        if (verticalVelocity.magnitude > 100) // If the bot falls out of the map.
        {
            GetComponent<Component_Health>().OnTakingDamage(9999, Vector3.zero);
            Destroy(gameObject);
        }
    }

    void manuallyCollision(float timeStep, Vector3 currentVelocity)
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


    #endregion

    Vector3 WanderThowards(Vector3 currentPos, float maxDistance = 999)
    {
        float randomAngle = Random.Range(0, 360);
        Vector3 direction = Quaternion.AngleAxis(randomAngle, Vector3.up) * Vector3.forward;


        Physics.Raycast(currentPos, direction, out RaycastHit hit);

        float currentDistance = (hit.transform != null && hit.distance < maxDistance) ? (hit.distance - 0.5f) : maxDistance; // I remove a little, to avoid clipping.

        Debug.DrawRay(currentPos, direction, Color.green, 4);

        Vector3 endPosition = transform.position + (direction * currentDistance);



        if (false && wanderMax < 100)
        {

            Vector3 directionToStartPos = (StartPos - endPosition).normalized;
            float distanceToStartPos = Vector3.Distance(endPosition, StartPos);

            Physics.Raycast(endPosition, directionToStartPos, out RaycastHit hit2, wanderMax);

            Debug.DrawRay(endPosition, directionToStartPos * distanceToStartPos, Color.blue, 4);

            print("" + (hit2.transform != null ? hit2.transform.name : "NULL") + ": " + hit2.distance + ", " + Vector3.Distance(endPosition, StartPos) + ".");

            bool hitValidTarget = hit2.transform != null && hit2.transform != transform;

            if (distanceToStartPos > wanderMax)         // It's not in LoS from start pos
                endPosition = WanderThowards(currentPos, maxDistance); // Then try again
        }


        return endPosition;


    }

    public void onDeath()
    {
        Manager_Audio.Play(Audio, Sounds_Turret.OnDeath);

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
