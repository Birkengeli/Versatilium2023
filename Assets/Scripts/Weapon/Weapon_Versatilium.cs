using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

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

    public enum TriggerTypes
    {
        None, SemiAutomatic, Automatic, Charge, Sight
    }

    [System.Flags]
    public enum OptionReplace // i think I should actually sort this in Weapon parts.
    {
        Nothing = 0 << 0,

        Damage = 1 << 0,
        Firerate = 1 << 1,
        TriggerPrimary = 1 << 2,
        TriggerSecondary = 1 << 3,
    }
				#endregion

	#region Classes & Structs

    [System.Serializable]
    public struct Projectile
    {
        public ProjectileTypes ProjectileType;
        public Transform visualTransform;
        public Tools_Animator anim;
        public Vector3 velocity;
        public Vector3 position;
        public float gravity;
        public float lifeTime;
        public int remainingBounces;

        [HideInInspector]
        public bool toBeDestroyed;
        [HideInInspector]
        public bool sciFi_detachTrail;
        [HideInInspector]
        public Transform userTransform;

        public ProjectileStatistics projectileStats;
    }

    [System.Serializable]
    public class WeaponStatistics
	{
        [Header("General")]
        public TriggerTypes triggerType = TriggerTypes.SemiAutomatic;

        [Header("Burst (Semi Automatic Only)")]
        public bool firesInBurst = false;
        public int burstCount = 3;
        public int burst_fireRate = 4;
        [HideInInspector] public int burstCounter = 0;


        public ProjectileStatistics Primary;
        public ProjectileStatistics Secondary;
 
        [HideInInspector]
        public Controller_Character characterController;
    }

    [System.Serializable]
    public class ProjectileStatistics
    {
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




        // public bool deleteProjectileOnImpact = true;
        public int bounceCount = 0;
        //  public bool freezeOnImpact = true;
    }

        #endregion

    public bool debugMode = true;
    public bool isWieldedByPlayer { get { return playerScript != null; } }
    public bool canFire = true;

    public WeaponStatistics WeaponStats;
    public WeaponStatistics WeaponStats_Alt;

    [Header("Inputs")]
    public KeyCode TriggerPrimary = KeyCode.Mouse0;
    public KeyCode TriggerSecondary = KeyCode.Mouse1;

    public TriggerTypes triggerType = TriggerTypes.SemiAutomatic;
    public TriggerTypes triggerType_Secondary = TriggerTypes.Sight;

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
    public float OnFire(TriggerTypes overRide = TriggerTypes.None) 
    {
        WeaponStatistics currentStats = WeaponStats;  // The Default Firemode

        fireRate_GlobalCD -= Time.deltaTime;

        bool preventedFromShooting = isWieldedByPlayer && playerScript.HasStatusEffect(Controller_Character.StatusEffect.DisableShooting);

        bool onKeyDown = Input.GetKeyDown(TriggerPrimary) && !preventedFromShooting;
        bool onKeyRelease = Input.GetKeyUp(TriggerPrimary) && !preventedFromShooting;
        bool onKeyTrue = Input.GetKey(TriggerPrimary) && !preventedFromShooting;

        triggerType = currentStats.triggerType;

        if (overRide != TriggerTypes.None)
        {
            onKeyDown = true;
            onKeyTrue = true;
        }




        if (triggerType == TriggerTypes.SemiAutomatic && !currentStats.firesInBurst)
        {
            if (onKeyDown && fireRate_GlobalCD < 0) // basic Fire
            {
                fireRate_GlobalCD = 1f / currentStats.Primary.fireRate;
                CreateProjectile(currentStats, currentStats.Primary);
            }

        }


        if (triggerType == TriggerTypes.SemiAutomatic && currentStats.firesInBurst)
        {
            bool remainingBursts = currentStats.burstCounter > 0;

            bool onRegularFire = fireRate_GlobalCD < 0 && onKeyDown && !remainingBursts;
            bool onBurstFire = remainingBursts && fireRate_GlobalCD < 0;

            if (onRegularFire || onBurstFire) // basic Fire
            {

                if (onRegularFire)
                    currentStats.burstCounter = currentStats.burstCount;

                currentStats.burstCounter--;

                fireRate_GlobalCD = currentStats.burstCounter > 0 ? 1f / currentStats.burst_fireRate : 1f / currentStats.Primary.fireRate;
                CreateProjectile(currentStats, currentStats.Primary);


            }

        }


        if (triggerType == TriggerTypes.Automatic)
        {
            if (onKeyTrue && fireRate_GlobalCD < 0) // basic Fire
            {
                fireRate_GlobalCD = 1f / currentStats.Primary.fireRate;
                CreateProjectile(currentStats, currentStats.Primary);
            }

        }

        if (triggerType == TriggerTypes.Charge)
        {

            if (onKeyDown && fireRate_GlobalCD < 0) // Start Charge
                Charge_current = 0;
            
            if (onKeyTrue && fireRate_GlobalCD < 0) // Charging
                Charge_current += Time.deltaTime;

            if (onKeyRelease && fireRate_GlobalCD < 0) // Release Charge
            {
                if (Charge_current < Charge_minimumTime)
                {
                    // On Tap fire
                    fireRate_GlobalCD = 1f / currentStats.Primary.fireRate;
                    CreateProjectile(currentStats, currentStats.Primary);
                }
                else
                {
                    // On Charged shot
                    fireRate_GlobalCD = 1f / currentStats.Secondary.fireRate;
                    CreateProjectile(WeaponStats_Alt, currentStats.Secondary);
                }

                Charge_current = 0;
            }

        }

        return fireRate_GlobalCD;

    }

				#endregion

	#region Projectile

    void CreateProjectile(WeaponStatistics currentStats, ProjectileStatistics projectileStats)
    {
        if(gunParticles != null)
            gunParticles.Play();


        if (isWieldedByPlayer)
        {
            playerScript.velocity += User_POV.forward * projectileStats.knockback_self;
        }

        if (!isWieldedByPlayer)
        {

        }

        #region Charge Options
        float chargePercentage = (triggerType == TriggerTypes.Charge) ? Charge_current / Charge_maximumTime : 1;
        

								#endregion

		#region Hitscan
		if (projectileStats.ProjectileType == ProjectileTypes.Hitscan)
        {

            for (int i = 0; i < projectileStats.PelletCount; i++)
            {
                Vector3 rayDeviation = User_POV.right * Random.Range(-projectileStats.Deviation, projectileStats.Deviation) + User_POV.up * Random.Range(-projectileStats.Deviation, projectileStats.Deviation);

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

                    currentProjectile.projectileStats = projectileStats;

                    Projectiles.Add(currentProjectile);

                    OnHit(projectileStats, hit.point, Mathf.Clamp(2f - (hit.distance / projectileStats.distanceBeforeDamageDrop), 0, 1), User_POV.forward, false, transform);

                    Vector3 bounceOrigin = laserPoint;
                    Vector3 bounceDirection = Vector3.Reflect(rayDirection, hit.normal);
                    float maxBounceDistance = (projectileStats.distanceBeforeDamageDrop - laserLength) / projectileStats.bounceCount;

                    for (int i2 = 1; i2 < projectileStats.bounceCount + 1 && hitSomething; i2++)
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

        if (projectileStats.ProjectileType == ProjectileTypes.Projectile)
        {
            float timeStep = Time.deltaTime;

            for (int i = 0; i < projectileStats.PelletCount; i++)
            {
                #region Spread
                bool isMultiPellet = projectileStats.PelletCount > 0;
                bool isFixedSpread = isMultiPellet && false;
                bool isFixedDistance = isMultiPellet && false;

                Vector3 angle = Quaternion.AngleAxis(isFixedSpread ? (360 / projectileStats.PelletCount) * i : Random.Range(0, 180), User_POV.forward) * User_POV.up; // Counter Clockwise. 180 Degress, because deviation is both axies
                float distance = Random.Range(-projectileStats.Deviation, projectileStats.Deviation);

                Vector3 rayDeviation = angle * (isFixedDistance ? projectileStats.Deviation : distance);
                #endregion

                Vector3 rayOrigin = User_POV.position;
                Vector3 rayDirection = User_POV.forward + rayDeviation;

                RaycastHit hit;
                Physics.Raycast(rayOrigin, rayDirection, out hit);
                Vector3 projectileDirection = hit.transform != null ? ( hit.point - Origin_Barrel.position).normalized : (rayDirection);

                Projectile currentProjectile = new Projectile();
                currentProjectile.userTransform = transform;

                currentProjectile.ProjectileType = ProjectileTypes.Projectile;
                currentProjectile.gravity = projectileStats.Projectile_Gravity;
                currentProjectile.position = Origin_Barrel.position;
                currentProjectile.velocity = projectileDirection * projectileStats.Projectile_Speed;
                currentProjectile.projectileStats = projectileStats;
                currentProjectile.remainingBounces = projectileStats.bounceCount;

                if(projectileStats.inheritUserVelocity)
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

                Projectiles.Add(currentProjectile);

                if (debugMode)
                {

                    Debug.DrawRay(currentProjectile.position, currentProjectile.velocity * timeStep, Color.red, fireRate_GlobalCD); // The firerate CD is 1f/ firerate in this tic
                    Debug.DrawLine(Origin_Barrel.position, rayOrigin + rayDirection * projectileStats.Projectile_Speed * timeStep, Color.blue, fireRate_GlobalCD); // The firerate CD is 1f/ firerate in this tic
                }

            }

            
           
        }
        #endregion

    }
    #endregion

    #region Delivery


    void OnHit(ProjectileStatistics projectileStats, Vector3 impactPosition, float distanceModifier, Vector3 knockBack, bool canHitMyself, Transform myself) // i want to skip this step at some point.
    {


        Collider[] hits = Physics.OverlapSphere(impactPosition, 0.01f);

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
                int damage = Mathf.RoundToInt((projectileStats.damage * distanceModifier) / projectileStats.PelletCount);

                Component_Health.Get(currentHit).OnTakingDamage(damage, (knockBack * projectileStats.knockback * distanceModifier) / projectileStats.PelletCount);
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

            if (currentProjectile.visualTransform != null)
                currentProjectile.visualTransform.localScale = Vector3.one * ProjectileScale * distanceScale;

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
                Physics.Raycast(currentProjectile.position, currentProjectile.velocity.normalized, out hit, currentProjectile.velocity.magnitude * timeStep, ~new LayerMask(), QueryTriggerInteraction.Collide);

                bool hitSomething = hit.transform != null;
                bool hitTrigger = hitSomething && hit.collider.isTrigger == true;
                bool hitTheWorld = hitSomething && !hitTrigger;
                bool hitMyself = hit.transform == currentProjectile.userTransform;
                bool hasGoneFar = currentProjectile.lifeTime * currentProjectile.velocity.magnitude > 2; // the projectile has traveled more than 2 meters.

                bool unBounceableSurface = hitSomething && hit.transform.tag == "No Bouncing Projectile";
                bool bounceableSurface = hitSomething && hit.transform.tag == "Always Bounces Projectile";
                bool hitOneWayShield = hitSomething && hit.collider.transform.tag == "One Way Shield" && Vector3.Dot(hit.collider.transform.forward, currentProjectile.velocity.normalized) > 0;
                bool hitActivator = hitSomething && hit.transform.tag == "Activator";

                if ((hitTheWorld || hitActivator) && (!hitMyself || hasGoneFar) && !hitOneWayShield) // If it hit something AND it didn't hitmyself OR it has goen far enough
                    hasImpacted = true;



                if (hasImpacted)
                {
                    OnHit(currentProjectile.projectileStats, hit.point, distanceScale, currentProjectile.velocity.normalized, hasGoneFar, transform);

                    if (hitActivator)
                    {
                        hit.transform.GetComponent<Trigger_Activator>().OnActivation(currentProjectile.visualTransform.gameObject);

                        unBounceableSurface = true;
                    }

                    if (currentProjectile.remainingBounces > 0 && !unBounceableSurface || bounceableSurface)
                    {
                        if(!bounceableSurface)
                            currentProjectile.remainingBounces--;
                        hasImpacted = false;

                        currentProjectile.position = hit.point; // Reset from the wall where it bounced;

                        Vector3 reflectedVelocity = Vector3.Reflect(currentProjectile.velocity, hit.normal);

                        currentProjectile.velocity = reflectedVelocity;


                    }

                    else if(currentProjectile.remainingBounces == 0 && currentProjectile.sciFi_detachTrail)
                    {
                        GameObject Explosion = currentProjectile.visualTransform.GetChild(0).gameObject;

                        float TimerEXP = 0.5f;

                        if (currentProjectile.sciFi_detachTrail)
                        {
                            foreach (Transform child in currentProjectile.visualTransform)
                            {
                                Destroy(child.gameObject, 1f);
                                child.parent = null;
                            }
                        }
                        GameObject E = Instantiate(Explosion, currentProjectile.visualTransform.position, Explosion.transform.rotation);
                        Destroy(E, TimerEXP);
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

            if(currentProjectile.visualTransform != null)
                currentProjectile.visualTransform.position = currentProjectile.position;

            currentProjectile.toBeDestroyed = distanceScale == 0 || hasImpacted;
        }
        #endregion

        projectileArray[index] = currentProjectile;

        #region Destruction
        if (currentProjectile.toBeDestroyed && currentProjectile.visualTransform != null)
        {

            if (currentProjectile.visualTransform != null)
            {
                foreach (Transform child in currentProjectile.visualTransform)
                {
                    Destroy(child.gameObject, 0); // This should actually be pooled.
                    child.parent = null;
                }

                Destroy(currentProjectile.visualTransform.gameObject);
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
}
