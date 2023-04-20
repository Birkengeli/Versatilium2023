using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.XR;

public class Weapon_Versatilium : MonoBehaviour
{
	#region Enums
	public enum ProjectileTypes
    {
        Hitscan, Projectile
    }

    public enum Visual_Type
    {
       None, Projectile, Laser
    }

    [System.Flags]
    public enum TriggerFlags // i think I should actually sort this in Weapon parts.
    {
        None = 0 << 0,

        SemiAutomatic = 1 << 0 | 0 << 1,
        Automatic = 1 << 1 | 0 << 0,

        Burst = 1 << 2,
        Charge = 1 << 3,
    }

	#endregion

	#region Classes & Structs

    [System.Serializable]
    public struct Projectile
    {
        [Header("General")]
        public ProjectileTypes ProjectileType;
        public Transform visualTransform;
        public Tools_Animator anim;
        public Vector3 velocity;
        public Vector3 position;
        public float gravity;
        public float lifeTime;
        public float chargePercentage;
        public int remainingBounces;

        [HideInInspector]
        public bool toBeDestroyed;
        [HideInInspector]
        public bool sciFi_detachTrail;
        [HideInInspector]
        public Transform userTransform;

        public WeaponStatistics projectileStats;

        [Header("Misc.")]
        public bool teleportUser;
        public bool isExplosive;
    }

    [System.Serializable]
    public class WeaponStatistics
	{
        [Header("General")]
        public TriggerFlags triggerTypes = TriggerFlags.SemiAutomatic;

        [Header("Burst (Semi Automatic Only)")]
        public int burstCount = 3;
        public int burst_fireRate = 4;
        [HideInInspector] public int burstCounter = 0;

        [Header("General")]
        public ProjectileTypes ProjectileType = ProjectileTypes.Hitscan;
        public float damage = 10;
        public float knockback = 10;
        public float fireRate = 2;
        public int PelletCount = 1;
        public float Deviation = 0.01f;
        public float knockback_self = 0;

        public bool inheritUserVelocity = true;
        public float targetSeekingStrength = 0;

        [Header("Range")]
        public float distanceBeforeDamageDrop = 20;
        public float Projectile_Speed = 25f; // TF2's Grenade Launcher moves at 23m/s
        public float Projectile_Gravity = 9.807f;

        [Header("Misc.")]
        public bool canTeleportUser;
        public bool counterProjectile;
        public bool isExplosive;

        // public bool deleteProjectileOnImpact = true;
        public int bounceCount = 0;
        //  public bool freezeOnImpact = true;

        [HideInInspector]
        public Controller_Character characterController;
    }


        #endregion

    public bool debugMode = true;
    public bool isWieldedByPlayer { get { return playerScript != null; } }
    public bool canFire = true;

    public WeaponStatistics WeaponStats;

    [Header("Inputs")]
    public KeyCode TriggerPrimary = KeyCode.Mouse0;
    public KeyCode TriggerSecondary = KeyCode.Mouse1;

    [Header("Trigger Settings: (Charge)")]
    public float Charge_minimumTime = 0.1f;
    public float Charge_maximumTime = 1f;
    private float Charge_current;

    [Header("Variables")]
    public Transform User_POV;
    public Transform Model_Weapon;
    public Transform Origin_Barrel;

    [Header("Components")]
    public ParticleSystem gunParticles;
    private AudioSource audioSource;

    public List<Projectile> Projectiles;
    public float fireRate_GlobalCD;

    Controller_Character playerScript;
    public int frameCounter;

    [Header("Visuals")]
    public Visual_Type useVisuals = Visual_Type.None;
    public GameObject projectilePrefab;
    public float ProjectileScale = 0.5f;

    public Tools_Sound.SoundClip[] soundClips;

    void Start()
    {
        playerScript = GetComponent<Controller_Character>();

        if (isWieldedByPlayer && User_POV == null)
            User_POV = GetComponentInChildren<Camera>().transform;

        if (Origin_Barrel == null)
            Origin_Barrel = User_POV;

        audioSource = GetComponent<AudioSource>();


        if (!isWieldedByPlayer)
        {
            
        }

        Tools_Sound.Start(soundClips, transform);

    }



    void Update()
    {
        frameCounter++;

        float timeStep = Time.deltaTime;

        if(isWieldedByPlayer && canFire) // NPCs decide themselves when to run "OnFire()";
             OnFire();

        for (int i = 0; i < Projectiles.Count; i++)
            ManageProjectile(Projectiles, i, timeStep);
        
    }

    #region OnFire

    /// Only TriggerType.SemiAutomtaic is supported right now.
    public float OnFire(TriggerFlags overRide = TriggerFlags.None) 
    {
        WeaponStatistics currentStats = WeaponStats;  // The Default Firemode

        fireRate_GlobalCD -= Time.deltaTime;

        bool preventedFromShooting = isWieldedByPlayer && playerScript.HasStatusEffect(Controller_Character.StatusEffect.DisableShooting);

        bool onKeyDown = Input.GetKeyDown(TriggerPrimary) && !preventedFromShooting;
        bool onKeyRelease = Input.GetKeyUp(TriggerPrimary) && !preventedFromShooting;
        bool onKeyTrue = Input.GetKey(TriggerPrimary) && !preventedFromShooting;

        TriggerFlags triggerTypes = currentStats.triggerTypes;

        if (overRide != TriggerFlags.None)
        {
            onKeyDown = true;
            onKeyTrue = true;
        }

        


        if (HasFlag((int)triggerTypes, (int)TriggerFlags.SemiAutomatic) && !HasFlag((int)triggerTypes, (int)TriggerFlags.Burst))
        {
            if (onKeyDown && fireRate_GlobalCD < 0) // basic Fire
            {
                fireRate_GlobalCD = 1f / currentStats.fireRate;
                CreateProjectile(currentStats);
            }

        }


        if (HasFlag((int)triggerTypes, (int)TriggerFlags.SemiAutomatic) && HasFlag((int)triggerTypes, (int)TriggerFlags.Burst))
        {
            bool remainingBursts = currentStats.burstCounter > 0;

            bool onRegularFire = fireRate_GlobalCD < 0 && onKeyDown && !remainingBursts;
            bool onBurstFire = remainingBursts && fireRate_GlobalCD < 0;

            if (onRegularFire || onBurstFire) // basic Fire
            {

                if (onRegularFire)
                    currentStats.burstCounter = currentStats.burstCount;

                currentStats.burstCounter--;

                fireRate_GlobalCD = currentStats.burstCounter > 0 ? 1f / currentStats.burst_fireRate : 1f / currentStats.fireRate;
                CreateProjectile(currentStats);


            }

        }


        if (HasFlag((int)triggerTypes, (int)TriggerFlags.Automatic))
        {
            if (onKeyTrue && fireRate_GlobalCD < 0) // basic Fire
            {
                fireRate_GlobalCD = 1f / currentStats.fireRate;
                CreateProjectile(currentStats);
            }

        }

        if (HasFlag((int)triggerTypes, (int)TriggerFlags.Charge))
        {
            float chargePerentage_Clamped = Mathf.Clamp(Charge_current / Charge_maximumTime, 0, 1);

            if (onKeyDown && chargeUpVisual == null && Charge_current < Charge_minimumTime)
            {
                chargeUpVisual = Instantiate(projectilePrefab);


                chargeUpVisual.transform.localScale = Vector3.one * ProjectileScale;
                chargeUpVisual.transform.parent = Origin_Barrel;
                chargeUpVisual.transform.localPosition = Vector3.zero;

                chargeUpVisual.GetComponent<SpriteRenderer>().color *= new Color(1, 1, 1, 0.5f);
                chargeUpVisual.layer = LayerMask.NameToLayer("VisibleOnlyInFirstPerson");
            }

            if (onKeyTrue && chargeUpVisual != null)
            {
                chargeUpVisual.transform.parent = null;
                chargeUpVisual.transform.localScale = Vector3.one * ProjectileScale * chargePerentage_Clamped;
                chargeUpVisual.transform.parent = Origin_Barrel;
            }

            if (onKeyDown && fireRate_GlobalCD < 0) // Start Charge
                Charge_current = 0;
            
            if (onKeyTrue && fireRate_GlobalCD < 0) // Charging
                Charge_current += Time.deltaTime;

            if (onKeyRelease && fireRate_GlobalCD < 0) // Release Charge
            {
                Destroy(chargeUpVisual);
                chargeUpVisual = null;

                if (Charge_current < Charge_minimumTime)
                {
                    // On Tap fire
                    fireRate_GlobalCD = 1f / currentStats.fireRate;
                    CreateProjectile(currentStats);
                }
                else
                {
                    // On Charged shot
                    fireRate_GlobalCD = 1f / currentStats.fireRate;
                    CreateProjectile(currentStats);
                }

                Charge_current = 0;
            }

        }

        return fireRate_GlobalCD;

    }

    GameObject chargeUpVisual;

	#endregion

	#region Projectile

    void CreateProjectile(WeaponStatistics currentStats)
    {
        if(gunParticles != null)
            gunParticles.Play();

        Tools_Sound.Play(soundClips, Tools_Sound.SoundFlags.OnUse);


     
        #region Charge Options
        float minChargePercentage = Charge_minimumTime/Charge_maximumTime;
        float chargePercentage = Mathf.Clamp(HasFlag((int)currentStats.triggerTypes, (int)TriggerFlags.Charge) ? Charge_current / Charge_maximumTime : 1, minChargePercentage, 1);
        float chargeCubed = Mathf.Pow(chargePercentage, 3);

        #endregion

        if (isWieldedByPlayer)
        {
            playerScript.velocity += User_POV.forward * currentStats.knockback_self * chargeCubed;
        }

        if (!isWieldedByPlayer)
        {

        }


        #region Hitscan
        if (currentStats.ProjectileType == ProjectileTypes.Hitscan)
        {

            for (int i = 0; i < currentStats.PelletCount; i++)
            {
                Vector3 rayDeviation = User_POV.right * Random.Range(-currentStats.Deviation, currentStats.Deviation) + User_POV.up * Random.Range(-currentStats.Deviation, currentStats.Deviation);

                Vector3 rayOrigin = User_POV.position;
                Vector3 rayDirection = User_POV.forward + rayDeviation;

                RaycastHit hit;
                Physics.Raycast(rayOrigin, rayDirection, out hit);
                bool hitSomething = hit.transform != null;


                if (useVisuals == Visual_Type.Laser)
                {
                    Vector3 laserPoint  = hitSomething ? hit.point : rayOrigin + rayDirection * 1000; 
                    float laserLength = hitSomething ? hit.distance : 1000;

                    Projectile currentProjectile = new Projectile();
                    currentProjectile.userTransform = transform;

                    currentProjectile.ProjectileType = ProjectileTypes.Hitscan;
                    currentProjectile.visualTransform = Instantiate(projectilePrefab).transform;
        
                    currentProjectile.velocity = laserPoint;
   
                    currentProjectile.position = Origin_Barrel.position;
                    currentProjectile.visualTransform.position = currentProjectile.position;
                    currentProjectile.visualTransform.LookAt(currentProjectile.velocity);

                    BeamStretcher(currentProjectile.visualTransform.GetComponent<LineRenderer>(), laserLength);
                    currentProjectile.visualTransform.GetChild(2).transform.position = currentProjectile.velocity;

                    currentProjectile.visualTransform.parent = Origin_Barrel;

                    currentProjectile.projectileStats = currentStats;

                    Projectiles.Add(currentProjectile);

                    OnHit(currentStats.damage, hit.point, Mathf.Clamp(2f - (hit.distance / currentStats.distanceBeforeDamageDrop), 0, 1), User_POV.forward, currentStats.knockback, false, transform);

                    Vector3 bounceOrigin = laserPoint;
                    Vector3 bounceDirection = Vector3.Reflect(rayDirection, hit.normal);
                    float maxBounceDistance = (currentStats.distanceBeforeDamageDrop - laserLength) / currentStats.bounceCount;

                    for (int i2 = 1; i2 < currentStats.bounceCount + 1 && hitSomething; i2++)
                    {
                        Physics.Raycast(bounceOrigin, bounceDirection, out hit, maxBounceDistance);

                        hitSomething = hit.transform != null;
                        laserPoint = hitSomething ? hit.point : currentProjectile.position + bounceDirection * maxBounceDistance;

                        currentProjectile.position = bounceOrigin; // Bouncing is done from the old laserpoint
                        currentProjectile.velocity = laserPoint; // The new laserpoint

                        currentProjectile.visualTransform = Instantiate(projectilePrefab).transform;
                        currentProjectile.visualTransform.position = currentProjectile.position;
                        currentProjectile.visualTransform.LookAt(currentProjectile.velocity);

                        BeamStretcher(currentProjectile.visualTransform.GetComponent<LineRenderer>(), laserLength);
                        currentProjectile.visualTransform.GetChild(2).transform.position = currentProjectile.velocity;

                        Projectiles.Add(currentProjectile);

                        bounceDirection = Vector3.Reflect(bounceDirection, hit.normal);
                        bounceOrigin = laserPoint;


                    }
                }



                if (debugMode)
																{
                    float debugRange = hit.distance + (hit.distance == 0 ? 100 : 0);

                    Debug.DrawRay(rayOrigin, rayDirection * debugRange, Color.red, fireRate_GlobalCD); // The firerate CD is 1f/ firerate in this tic


                    Debug.DrawLine(Origin_Barrel.position, rayOrigin + rayDirection * debugRange, Color.blue, fireRate_GlobalCD); // The firerate CD is 1f/ firerate in this tic
                }

                /// Raycast hit gives me a lot of information I need to take with me
                /// hit.point being the biggest one.
                /// 

       


            }


        }
        #endregion

        #region Simulated Projectile

        if (currentStats.ProjectileType == ProjectileTypes.Projectile)
        {
            float timeStep = Time.deltaTime;

            for (int i = 0; i < currentStats.PelletCount; i++)
            {

                bool isMultiPellet = currentStats.PelletCount > 0;
                bool isBurstFire = HasFlag((int)currentStats.triggerTypes, (int)TriggerFlags.SemiAutomatic);

                #region Spread
                bool isFixedSpread = isMultiPellet && false;
                bool isFixedDistance = isMultiPellet && false;


                Vector3 angle = Quaternion.AngleAxis(isFixedSpread ? (360 / currentStats.PelletCount) * i : Random.Range(0, 180), User_POV.forward) * User_POV.up; // Counter Clockwise. 180 Degress, because deviation is both axies
                float distance = Random.Range(-currentStats.Deviation, currentStats.Deviation);

                Vector3 rayDeviation = angle * (isFixedDistance ? currentStats.Deviation : distance);
                #endregion

                Vector3 rayOrigin = User_POV.position;
                Vector3 rayDirection = User_POV.forward + rayDeviation;

                GetComponent<Collider>().isTrigger = true; // I keep hitting myself.

                RaycastHit hit;
                Physics.Raycast(rayOrigin, rayDirection, out hit, Mathf.Infinity, ~new LayerMask(), QueryTriggerInteraction.Ignore);
                Vector3 projectileDirection = hit.transform != null ? ( hit.point - Origin_Barrel.position).normalized : (rayDirection);

                GetComponent<Collider>().isTrigger = false;


                Projectile currentProjectile = new Projectile();
                currentProjectile.userTransform = transform;

                currentProjectile.ProjectileType = ProjectileTypes.Projectile;
                currentProjectile.gravity = currentStats.Projectile_Gravity;
                currentProjectile.position = Origin_Barrel.position;
                currentProjectile.velocity = projectileDirection * currentStats.Projectile_Speed;
                currentProjectile.projectileStats = currentStats;
                currentProjectile.remainingBounces = currentStats.bounceCount;
                currentProjectile.isExplosive = currentStats.isExplosive;
                currentProjectile.chargePercentage = chargePercentage;

                if((!isMultiPellet && !isBurstFire) || (isMultiPellet && i == 0) || (isBurstFire && currentStats.burstCounter == 0)) // Applies on-hit
                {
                    currentProjectile.teleportUser = currentStats.canTeleportUser;
                }


                if (currentStats.inheritUserVelocity)
				{
                    if (isWieldedByPlayer)
                    {
                        if (currentStats.characterController == null)
                            currentStats.characterController = transform.GetComponent<Controller_Character>();

                        currentProjectile.velocity += currentStats.characterController.velocity;
                    }
                }

                if (useVisuals == Visual_Type.Projectile)
                {
                    currentProjectile.visualTransform = Instantiate(projectilePrefab).transform;
                    currentProjectile.visualTransform.position = currentProjectile.position;
                    currentProjectile.visualTransform.localScale = Vector3.one * ProjectileScale;
                }

                if (currentStats.counterProjectile)
                {
                    currentProjectile.visualTransform.AddComponent<SphereCollider>().isTrigger = true;
                    currentProjectile.visualTransform.tag = "AntiProjectile";
                }

                Projectiles.Add(currentProjectile);

                if (debugMode)
                {

                    Debug.DrawRay(currentProjectile.position, currentProjectile.velocity * timeStep, Color.red, fireRate_GlobalCD); // The firerate CD is 1f/ firerate in this tic
                    Debug.DrawLine(Origin_Barrel.position, rayOrigin + rayDirection * currentStats.Projectile_Speed * timeStep, Color.blue, fireRate_GlobalCD); // The firerate CD is 1f/ firerate in this tic
                }

            }
           
        }
        #endregion

    }
    #endregion

    #region Delivery


    void OnHit(float damage, Vector3 impactPosition, float distanceModifier, Vector3 knockBack, float knockbackStrength, bool canHitMyself, Transform myself, float radius = 0.01f) // i want to skip this step at some point.
    {


        Collider[] hits = Physics.OverlapSphere(impactPosition, radius);

     

        for (int i = 0; i < hits.Length; i++)
        {
            Transform currentHit = hits[i].transform;

            bool hitPlayer = currentHit.CompareTag("Player");
            bool hitEnemy = currentHit.CompareTag("Enemy");
            bool hitMyself = currentHit == myself;

            if (hitMyself && !canHitMyself)
                continue;

            if (hitPlayer || hitEnemy)
            {
                damage = Mathf.RoundToInt(damage * distanceModifier);

                Component_Health.Get(currentHit).OnTakingDamage((int)damage, knockBack * knockbackStrength * distanceModifier);
            }


            
        }

    }

    #endregion


    Transform FindClosestTarget(Vector3 position, Vector3 forward, float radius, float coneSize = 180)
    {
        Collider[] colliders = Physics.OverlapSphere(position, radius, ~new LayerMask(), QueryTriggerInteraction.Ignore);

        float degreesToRange = -((coneSize / 360f) - 0.5f) * 2;

        float closestDistance = Mathf.Infinity;
        Transform closestTarget = null;

        for (int i = 0; i < colliders.Length; i++)
        {
            Transform currentTarget = colliders[i].transform;
            Vector3 directionToTarget = (currentTarget.position - position).normalized;


            bool isForwardEnough = Vector3.Dot(forward, directionToTarget) > degreesToRange;
            bool isTaggedCorrectly = currentTarget.tag == "Target";
            float distance = Vector3.Distance(position, currentTarget.position);

            if (isForwardEnough && distance < closestDistance && isTaggedCorrectly)
            {
                closestDistance = distance;
                closestTarget = currentTarget;
            }

        }

        return closestTarget;
    }

    public void OnHit(string testValue)
    {
        Debug.Log(testValue + " + " + 99999 + " actual damage.");
    }

    public void ManageProjectile(List<Projectile> projectileArray, int index, float timeStep)
    {
        Projectile currentProjectile = projectileArray[index];

        bool hasImpacted = false;
        bool hasVisuals = currentProjectile.visualTransform != null;

		#region Hitscan
		if (currentProjectile.ProjectileType == ProjectileTypes.Hitscan)
        {
            float laserLifeTime = 0.1f;

            float distanceModifier = currentProjectile.lifeTime / laserLifeTime;

            currentProjectile.lifeTime += timeStep;

            float distance = Vector3.Distance(currentProjectile.position, currentProjectile.velocity);
            float oldScale = ProjectileScale;

            currentProjectile.visualTransform.LookAt(currentProjectile.velocity);
            currentProjectile.visualTransform.localScale = Vector3.one;

            currentProjectile.toBeDestroyed = currentProjectile.lifeTime > laserLifeTime;
        }

        #endregion

        #region Projectile

        if (currentProjectile.ProjectileType == ProjectileTypes.Projectile)
        {

            float distanceModifier = (currentProjectile.velocity.magnitude * currentProjectile.lifeTime) / currentProjectile.projectileStats.distanceBeforeDamageDrop;
            float distanceScale = Mathf.Clamp(2 - distanceModifier, 0, 1);
            float currentScale = ProjectileScale * distanceScale * currentProjectile.chargePercentage;

            if (hasVisuals)
                currentProjectile.visualTransform.localScale = Vector3.one * currentScale;

            if(false)
            { // Adjust the velocity for homing.
                Transform target = FindClosestTarget(currentProjectile.position, currentProjectile.velocity.normalized, 1000, 360);
                if (target != null)
                {
                    Vector3 directionToTarget = (target.position - currentProjectile.position);
                    currentProjectile.velocity += directionToTarget * 10 * timeStep;
                }
            }

            {
                RaycastHit hit;
                Physics.SphereCast(currentProjectile.position, currentScale / 2, currentProjectile.velocity.normalized, out hit, currentProjectile.velocity.magnitude * timeStep, ~new LayerMask(), QueryTriggerInteraction.Collide);

                bool hitSomething = hit.transform != null;
                bool hitTrigger = hitSomething && hit.collider.isTrigger == true;
                bool hitTheWorld = hitSomething && !hitTrigger;
                bool hitMyself = hit.transform == currentProjectile.userTransform;
                bool hitCharacter = hitSomething && (hit.transform.tag == "Player" || hit.transform.tag == "Enemy");
                bool hasGoneFar = currentProjectile.lifeTime * currentProjectile.velocity.magnitude > 2; // the projectile has traveled more than 2 meters.

                bool unBounceableSurface = hitSomething && hit.transform.tag == "No Bouncing Projectile";
                bool bounceableSurface = hitSomething && hit.transform.tag == "Always Bounces Projectile";
                bool hitThroughOneWayShield = hitSomething && hit.collider.transform.tag == "One Way Shield" && Vector3.Dot(hit.collider.transform.forward, currentProjectile.velocity.normalized) > 0;
                bool hitActivator = hitSomething && hit.transform.tag == "Activator";
                bool hitAntiProjectile = DidCollideWithPlayerProjectile(currentProjectile.position, currentScale);


                if (hitAntiProjectile)
                    bounceableSurface = true;

                if ((hitTheWorld || hitActivator || hitAntiProjectile) && (!hitMyself || hasGoneFar) && !hitThroughOneWayShield) // If I hit any of these AND I did NOT hit any of those.
                    hasImpacted = true;

                if (hasImpacted)
                {
                    float pellets = currentProjectile.projectileStats.PelletCount;
                    OnHit(currentProjectile.projectileStats.damage / pellets, hit.point, distanceScale, currentProjectile.velocity.normalized, currentProjectile.projectileStats.knockback / pellets, hasGoneFar, transform);

                    if (hasVisuals && hitActivator)
                    {
                        hit.transform.GetComponent<Trigger_Activator>().OnActivation(currentProjectile.visualTransform.gameObject, currentScale);

                        unBounceableSurface = true;
                    }

                    if (true && !hitActivator && !hitCharacter) // Decals
                    {
                        Transform decal = Instantiate(currentProjectile.visualTransform.gameObject).transform;

                        decal.forward = hit.normal;
                        decal.position = hit.point + hit.normal * currentScale / 2 + currentProjectile.velocity.normalized * currentScale / 2;
                        //decal.parent = hit.transform;

                        Tools_Animator spriteAnim = decal.GetComponent<Tools_Animator>();
                        spriteAnim.Play("Impact");

                        decal.GetComponent<Tools_Sprite>().enabled = false;
                    }

                    if (currentProjectile.remainingBounces > 0 && !unBounceableSurface || bounceableSurface)
                    {
                        if(!bounceableSurface)
                            currentProjectile.remainingBounces--;
                        hasImpacted = false;

                        currentProjectile.position = hit.point + (hit.normal * currentScale / 2); // Makes sure the projectile bounces from the correct position.; // Reset from the wall where it bounced;

                        Vector3 reflectedVelocity = Vector3.Reflect(currentProjectile.velocity, hit.normal);

                        currentProjectile.velocity = reflectedVelocity;


                    }

                 

                }
            }

            if (debugMode)
            {
                float ColorGradeUp = distanceModifier;
                float ColorGradeDown = 1f - ColorGradeUp;

                Color customColor = new Color(ColorGradeDown, 0, ColorGradeUp);

                Debug.DrawRay(currentProjectile.position, currentProjectile.velocity * timeStep, customColor, 1f);
            }

            currentProjectile.lifeTime += timeStep;
            currentProjectile.position += currentProjectile.velocity * timeStep;
            currentProjectile.velocity += Vector3.down * currentProjectile.gravity * timeStep;

            if(hasVisuals)
                currentProjectile.visualTransform.position = currentProjectile.position;

            currentProjectile.toBeDestroyed = currentScale == 0 || hasImpacted;
        }
        #endregion

        projectileArray[index] = currentProjectile;

        #region Destruction

        if (currentProjectile.toBeDestroyed && currentProjectile.isExplosive)
        {
            GameObject explosiveEffect = Instantiate(currentProjectile.visualTransform).gameObject;
            explosiveEffect.transform.localScale *= 2;
            explosiveEffect.AddComponent<Temp_ExplosiveEffect>();

            OnHit(currentProjectile.projectileStats.damage / 2, currentProjectile.position, 1, currentProjectile.velocity.normalized, currentProjectile.projectileStats.knockback, true, transform, currentProjectile.visualTransform.localScale.magnitude * 2);
        }

        if (currentProjectile.toBeDestroyed && hasImpacted && currentProjectile.teleportUser) //  teleport module.
        {
            Vector3 positionOffset = Vector3.zero;
            Vector3 playerOffset = Vector3.up * currentProjectile.userTransform.GetComponent<CapsuleCollider>().height / 2;

            currentProjectile.userTransform.position = currentProjectile.position + positionOffset + playerOffset;
            currentProjectile.userTransform.GetComponent<Controller_Character>().velocity = Vector3.zero;
        }

        if (currentProjectile.toBeDestroyed)
        {
            if (hasVisuals)
            {
                foreach (Transform child in currentProjectile.visualTransform)
                {
                    Destroy(child.gameObject, 0); // This should actually be pooled.
                    child.parent = null;
                }

                Destroy(currentProjectile.visualTransform.gameObject);
                hasVisuals = false;
            }

            Projectiles.RemoveAt(index);
            index--; // purely cermonial. This does nothing.
        }
								#endregion
	}

    public void BeamStretcher (LineRenderer line, float distance)
    {
        line.SetPosition(0, line.transform.InverseTransformPoint(line.transform.position));
        line.SetPosition(1, line.transform.InverseTransformPoint(line.transform.position + line.transform.forward * distance));
    }

    GameObject Projectile_Prefab_Prep(GameObject projectilePrefab)
    {
        Destroy(projectilePrefab.GetComponent<Rigidbody>());
        Destroy(projectilePrefab.GetComponent<Collider>());

        return projectilePrefab;
    }

    public static bool HasFlag(int mask, int effect)
    {
        return (mask & effect) != 0;
    }

    bool DidCollideWithPlayerProjectile(Vector3 myProjectilePos, float myProjectileScale) // I hate this function. Shouldn't exist.
    {
        GameObject Player = GameObject.FindGameObjectWithTag("Player");
        Weapon_Versatilium playerWeapon = Player.GetComponent<Weapon_Versatilium>();

        bool isThePlayer = Player == gameObject;

        if (isThePlayer || !playerWeapon.WeaponStats.counterProjectile)
            return false;

        foreach (Projectile currentProjectile in playerWeapon.Projectiles)
        {
            float coreDistance = Vector3.Distance(currentProjectile.position, myProjectilePos);
            float projectileRadiuses = myProjectileScale / 2 + playerWeapon.ProjectileScale / 2;

            if (coreDistance <= projectileRadiuses) // Radius
            {
                // code to destroy the counter projectile.
                return true;
            }
        }

        return false;
    }
}
