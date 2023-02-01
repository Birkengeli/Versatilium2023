using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Component_Health : MonoBehaviour
{

    [Header("Settings")]
    public int HealthMax = 100;
    public float healthCurrent = -1;
    public float knockback_Multiplier = 1;

    [Header("Variables")]
    public bool isDead;

    [Header("Enemy only")]
    public bool hasDeathAnimation = false;

    [Header("Player only")]
    public float deathCountdown = 5f;
    private float deathCountdown_Timer = 0;
    public float deathCountdown_SpeedModifier = 2;
    public float deathCountdown_SpeedDuration = 0.5f;
    private float lastSpeedUse;
    public GameObject Prop_Weapon;

    private Image POV_Death;
    private Image UI_Healthbar_Fill;

    [Header("Components")]
    Controller_Character playerScript;
    Controller_Enemy enemyScript;
    bool isPlayer { get { return enemyScript == null; } }

    // Start is called before the first frame update
    void Start()
    {
        if (healthCurrent == -1)
            healthCurrent = HealthMax;

        deathCountdown_Timer = deathCountdown;

        enemyScript = GetComponent<Controller_Enemy>();

        if (isPlayer)
        {
            playerScript = GetComponent<Controller_Character>();

            GameObject canvas = GameObject.Find("_Canvas");

            if (canvas == null)
                Debug.Log("Error, could not find the '_Canvas'.");

            UI_Healthbar_Fill = Weapon_Switching.GetChildByName("UI_Healthbar_Fill", canvas.transform).GetComponent<Image>();

            POV_Death = Weapon_Switching.GetChildByName("POV_Death", canvas.transform).GetComponent<Image>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isDead)
            WhileDead(false);

        
        if(false)
        { // Healthreg


            float healingPercentage = 0.49f;
            float healingModifier = healingPercentage * (1 + 0.50f*3 + 1);

            if (healthCurrent < HealthMax)
                healthCurrent += HealthMax * healingPercentage * 2 * healingModifier * Time.deltaTime / 100;

            UI_Healthbar_Fill.fillAmount = healthCurrent / HealthMax;

        }
    }

    public void OnHealing(int healing)
    {

        if (!isDead)
        {

            float missingHealth = HealthMax - healthCurrent;

            healthCurrent += Mathf.Min(healing, missingHealth);


            if (isPlayer)
            {
                UI_Healthbar_Fill.fillAmount = (float)healthCurrent / HealthMax;
            }
        }



    }

    public void OnTakingDamage(int damage, Vector3 knockBack)
    {

        if (!isDead)
        {

            if (isPlayer)
            {
                playerScript.velocity = knockBack;
      
                    

            }

            if (!isPlayer)
            {

                if (enemyScript.isInvincible)
                {
                 
                    return;
                }



                if (enemyScript.enemyType == Controller_Enemy.EnemyTypes.Humanoid)
                {
                    enemyScript.velocity += knockBack * knockback_Multiplier;

                    if (!enemyScript.isInCombat)
                    {
                        Animator anim = GetComponentInChildren<Animator>();
                        anim.SetTrigger("onCombat");

                        enemyScript.isInCombat = true;
                    }
                }

            }

            healthCurrent -= damage;


        }


        if (isPlayer)
        {
            UI_Healthbar_Fill.fillAmount = (float)healthCurrent / HealthMax;
        }

        if (!isDead && healthCurrent <= 0)
            WhileDead(true); // On Death
        

    }

    public static Component_Health Get(Transform target)
    {
        Component_Health healthScript = target.GetComponent<Component_Health>();

        if (healthScript == null)
            Debug.LogWarning("Could not find a healthScript.");

        return healthScript;
    }

    public void WhileDead(bool onDeath)
    {
        isDead = true;

        if (isPlayer)
        {
            if (onDeath)
            {
                healthCurrent = 0;
                deathCountdown_Timer = 0;

                playerScript.ApplyStatusEffect(Controller_Character.StatusEffect.FreezeCamera);
                playerScript.ApplyStatusEffect(Controller_Character.StatusEffect.FreezeMovement);
                GetComponent<Weapon_Versatilium>().canFire = false;

                #region Visuals

                Transform eyes = transform.GetComponentInChildren<Camera>().transform;
                Vector3 cameraDeathEuler = new Vector3(-14.881f, 0.22f, 65.934f);
                Vector3 cameraDeathPosition = new Vector3(-0.509f, -0.576f, 0);
                eyes.localEulerAngles = cameraDeathEuler;
                eyes.localPosition = cameraDeathPosition;

                int childLength = eyes.childCount;
                for (int i = 0; i < childLength; i++)
                    eyes.GetChild(i).gameObject.SetActive(false);

                playerScript.velocity += -eyes.forward * 1f;
                playerScript.friction /= 1.5f; // Slide more.

                if (Prop_Weapon != null)
                {
                    GameObject weaponProp = Instantiate(Prop_Weapon).gameObject;

                    weaponProp.transform.forward = eyes.forward;
                    weaponProp.transform.eulerAngles += Vector3.forward * 20; // Add a little roll.
                    weaponProp.transform.position = eyes.position  + eyes.forward * 0.6f + eyes.right * 0.4f;

                    weaponProp.GetComponent<Rigidbody>().velocity = playerScript.velocity * 0.6f;
                }

                transform.GetChild(1).gameObject.SetActive(false);

                UI_Healthbar_Fill.transform.parent.gameObject.SetActive(false);
                

                #endregion
            }



            if (POV_Death != null)
            {
                POV_Death.gameObject.SetActive(true);

                float alpha = (deathCountdown_Timer) / (deathCountdown*1.5f);

                POV_Death.color = new Color(0.8f, 0, 0, alpha);
            }

            #region Spamming buttons make you respawn faster
            if (deathCountdown_Timer >= 0)
            {
                if (Input.anyKeyDown)
                    lastSpeedUse = Time.timeSinceLevelLoad;

                bool useRespawnBoost = Time.timeSinceLevelLoad < lastSpeedUse + deathCountdown_SpeedDuration;
                deathCountdown_Timer += Time.deltaTime * (useRespawnBoost ? deathCountdown_SpeedModifier : 1f);
            }
												#endregion

												if (deathCountdown_Timer > deathCountdown)
            {
                if (deathCountdown_Timer != -1) // On Time run out
                {
                    deathCountdown_Timer = -1;

                    Debug.Log("Respawning Player");
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                }
            }
        }

        if (!isPlayer)
        {
            if (onDeath)
            {

                enemyScript.enabled = false;
                GetComponent<Collider>().enabled = false;

                enemyScript.onDeath();

                if (!hasDeathAnimation)
                    transform.GetChild(0).position += Vector3.down * 100;
                else
                {
                    Animator anim = GetComponentInChildren<Animator>();
                    anim.SetBool("isDead", true);
                }
            }
        }
    }
}
