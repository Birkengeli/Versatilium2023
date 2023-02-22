using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test_TurretController : MonoBehaviour
{

    public float DetectionRange = 10;
    public float ActivationTime = 1;
    public float rememberPlayerFor = 5;
    float timer = 0;


    public enum TurretStates
    {
        Sleeping,
        Erecting,
        Active,
        Searching,
        Retracting,
    }
    public TurretStates TurretState = TurretStates.Sleeping;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float timeStep = Time.deltaTime;

        Transform player = GameObject.FindGameObjectWithTag("Player").transform;
        Animator anim = GetComponentInChildren<Animator>();

        Vector3 targetPosition = player.position;

        Vector3 directionToTarget = targetPosition - transform.position;

        Debug.DrawRay(transform.position, directionToTarget, Color.white);

        Vector3 directionToTarget_Horizontal = directionToTarget - Vector3.Project(directionToTarget, transform.up);

        float distanceToTarget = directionToTarget_Horizontal.magnitude; // Distance from the turret in a cyllinder.


        RaycastHit hit;
        Physics.Raycast(transform.position, (targetPosition - transform.position).normalized, out hit, DetectionRange);

        bool canSeeTarget = hit.transform != null && hit.transform.tag == "Player";
        bool hasDetectedTarget = distanceToTarget <= DetectionRange;

        if (TurretState == TurretStates.Sleeping)
        {

            if (hasDetectedTarget && canSeeTarget) // On Deploy
            {
                TurretState = TurretStates.Erecting;
                anim.Play("Armature|Turret_Errection_Up", 2);
            }
        }

        if (TurretState == TurretStates.Erecting)
        {
            timer += timeStep;

            if(timer > ActivationTime) // On Active
            {
                TurretState = TurretStates.Active;
                timer = ActivationTime;

                anim.Play("Empty State", 2);

            }
        }

        if (TurretState == TurretStates.Active)
        {
            if (!canSeeTarget) // On Searching
            {
                TurretState = TurretStates.Searching;
                timer = rememberPlayerFor;

                anim.Play("Empty State", 0);
                anim.Play("Empty State", 1);
            }
        }

        if (TurretState == TurretStates.Searching)
        {
            timer -= timeStep;

            if (canSeeTarget) // On Returning to Active
            {
                TurretState = TurretStates.Active;
            }

            if (!canSeeTarget && timer < 0) // On retracting
            {
                TurretState = TurretStates.Retracting;
                timer = ActivationTime;
                anim.Play("Armature|Turret_Errection_Down", 2);

                anim.Play("Empty State", 0);
                anim.Play("Empty State", 1);
            }
        }

        if (TurretState == TurretStates.Retracting)
        {
            timer -= timeStep;

            if (!canSeeTarget && timer < 0) // on Sleep
            {
                TurretState = TurretStates.Sleeping;
                timer = 0;


            }
        }






        if (TurretState == TurretStates.Active)
        {

            Debug.DrawRay(transform.position, directionToTarget_Horizontal.normalized, Color.blue);
            Debug.DrawRay(transform.position, transform.right, Color.blue * 0.5f);

            float yaw = Vector3.Angle(-transform.forward, directionToTarget_Horizontal);
            bool isRightOfTurret = Vector3.Dot(directionToTarget, transform.right) > 0;

            string animationName = "Armature|Turret_Yaw_" + (isRightOfTurret ? "Right" : "Left");
            float animationPercentage = (180f + (isRightOfTurret ? -yaw : yaw)) / 360f;

            //print("Angle: '" + yaw + "' to the " + (isRightOfTurret ? "Right." : "Left."));

            anim.Play("Armature|Turret_Yaw", 1, animationPercentage);

        }

        if(TurretState == TurretStates.Active)
        {
            Vector3 newRelativeForward = directionToTarget_Horizontal.normalized;
            Vector3 newRelativeRight = Vector3.Cross(newRelativeForward, Vector3.up);

            Vector3 directionToTarget_Vertical = directionToTarget - Vector3.Project(directionToTarget.normalized, newRelativeRight);

            Debug.DrawRay(transform.position, directionToTarget_Vertical.normalized, Color.red);
            Debug.DrawRay(transform.position, newRelativeForward, Color.red * 0.5f);

            float maxPitch = 45f;

            float pitchClamp = Mathf.Clamp(Vector3.Angle(newRelativeForward, directionToTarget_Vertical), 1, maxPitch); // I'm clamping it above 0, just incase isAboveTurret isn't precise enough.
            bool isAboveTurret = Vector3.Dot(directionToTarget, transform.up) > 0;
            float animationPercentage = pitchClamp / maxPitch;

            anim.Play("Armature|Turret_Pitch", 0, animationPercentage);
        }
    }
}
