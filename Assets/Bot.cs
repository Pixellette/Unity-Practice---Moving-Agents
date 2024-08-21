using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;
using UnityEngine.AI;

public class Bot : MonoBehaviour
{
    NavMeshAgent agent;
    public GameObject target;  // find the cop
    Drive ds;

    [SerializeField] bool cooldown = false; 

    // 3 key variables of the Wander concept. Change these to change the behaviour
    [Header ("Wander Settings")]
        [SerializeField] float wanderRadius = 10;
        [SerializeField] public float wanderDistance = 20;
        [SerializeField] float wanderJitter = 1; 
        Vector3 wanderTarget = Vector3.zero; // cannot be local as needs to remember between calls

    [Header ("Hide Settings")]
        [SerializeField] float hideDistance = 50.0f;
        [SerializeField] float checkHideDist = 10.0f;
        [SerializeField] float bounceRaybackDist = 100.0f;

    // Start is called before the first frame update
    void Start()
    {
        agent = this.GetComponent<NavMeshAgent>();
        ds = target.GetComponent<Drive>(); // This is costly to do so do it once
    }

    // Update is called once per frame
    void Update()
    {
        // Flee(target.transform.position); // find the cop and set as target. 
        //Persue();
        // Evade();
        if(!cooldown)
        {
            if(CanSeeTarget() && CanSeeMe())
            {
                CleverHide();
                cooldown = true; 
                Invoke("BehaviourCooldown", 5);
            }
            else {
                Persue();
            }
        }
        
    }

    void Seek(Vector3 location)
    {
        agent.SetDestination(location);
    } // End of Seek Method

    void Persue() // Persue is different from Seek as it predicts where the target will be to intercept them
    {
        Vector3 targetDir = target.transform.position - this.transform.position;

        // --------- Section covers switching to Seek when persue is inefficient ---------
        float relativeHeading = Vector3.Angle(this.transform.forward, this.transform.TransformVector(target.transform.forward)); // Angle between their forward directions 
        float toTarget = Vector3.Angle(this.transform.forward, this.transform.TransformVector(targetDir));
        // Check target isn't behind us already, but in a way that makes sense
        // OR has stopped! 
        if ((toTarget > 90 && relativeHeading < 20) ||  ds.currentSpeed < 0.01f) // note not doing == 0 as can have weird glitches
        {
            Seek(target.transform.position);
            Debug.Log("Seeking..."); // TODO: debug
            return;
        }
        // ---------------------------------------------------------------------------------

        //calc look ahead 
        Debug.Log("Persuing"); // TODO: debug
        float lookAhead = targetDir.magnitude/(agent.speed + ds.currentSpeed);
        Seek(target.transform.position + target.transform.forward * lookAhead); // note that forward = 1 as already normalized for us
    } // End of Persue Method

    void Flee(Vector3 location)
    {
        Vector3 fleeVector = location - this.transform.position;
        agent.SetDestination(this.transform.position - fleeVector);
    } // End of Flee Method

    void Evade()
    {
        Vector3 targetDir = target.transform.position - this.transform.position;
        float lookAhead = targetDir.magnitude/(agent.speed + ds.currentSpeed);
        Flee(target.transform.position + target.transform.forward * lookAhead); // note that forward = 1 as already normalized for us
        Debug.Log("Evading"); // TODO: debug
    } // End of Evade Method

    void Wander()
    {
        wanderTarget += new Vector3(UnityEngine.Random.Range(-1.0f, 1.0f) * wanderJitter,   // X
                                    0,                                                      // Y
                                    UnityEngine.Random.Range(-1.0f, 1.0f) * wanderJitter);  // Z

        // Move the target back onto the circle (currently ON the Agent)
        wanderTarget.Normalize(); // get a better number
        wanderTarget *= wanderRadius; // push it out to the right length

        // Move circle to *infront* of Agent
        Vector3 targetLocal = wanderTarget + new Vector3(0, 0, wanderDistance); // local because we are imagining the Agent as the center of the world
        Vector3 targetWorld = this.gameObject.transform.InverseTransformVector(targetLocal); // Now convert to world location 

        // Finally Seek the target location
        Seek(targetWorld);
    }

    void Hide()
    {
        // find the best hiding space - closest to agent 

        float dist = Mathf.Infinity;
        Vector3 chosenSpot = Vector3.zero;

        // check each possible hiding spot 
        for (int i = 0; i < World.Instance.GetHidingSpots().Length; i++)
        {
            Vector3 hideDir = World.Instance.GetHidingSpots()[i].transform.position - target.transform.position; // vector from the target (cop) to obstacle (tree)
            Vector3 hidePos = World.Instance.GetHidingSpots()[i].transform.position + hideDir.normalized * hideDistance; //Create a space behind the tree for our hide spot by using hideDir and a distance behind

            // Check if tree is closer than last pull - if YES then update Chosen
            if(Vector3.Distance(this.transform.position, hidePos) < dist)
            {
                chosenSpot = hidePos;
                dist = Vector3.Distance(this.transform.position, hidePos);
            }
        } // End of loop 

        Seek(chosenSpot);
    }

    void CleverHide()
    {
        // find the best hiding space - closest to agent 

        float dist = Mathf.Infinity;
        Vector3 chosenSpot = Vector3.zero;
        Vector3 chosenDir = Vector3.zero;
        GameObject chosenGO = World.Instance.GetHidingSpots()[0]; // setting to the very first one so its atleast initialised to something 

        // check each possible hiding spot 
        for (int i = 0; i < World.Instance.GetHidingSpots().Length; i++)
        {
            Vector3 hideDir = World.Instance.GetHidingSpots()[i].transform.position - target.transform.position; // vector from the target (cop) to obstacle (tree)
            Vector3 hidePos = World.Instance.GetHidingSpots()[i].transform.position + hideDir.normalized * checkHideDist; //Create a space behind the tree for our hide spot by using hideDir and a distance behind

            // Check if tree is closer than last pull - if YES then update Chosen
            if(Vector3.Distance(this.transform.position, hidePos) < dist)
            {
                chosenSpot = hidePos;
                chosenDir = hideDir;
                chosenGO = World.Instance.GetHidingSpots()[i];
                dist = Vector3.Distance(this.transform.position, hidePos);
            }
        } // End of loop 

        Collider hideCol = chosenGO.GetComponent<Collider>(); // doing a get comp in here, which is not optimal, but there's really no other option of where to put it
        Ray backRay = new Ray(chosenSpot, -chosenDir.normalized);
        RaycastHit info;
        hideCol.Raycast(backRay, out info, bounceRaybackDist); // the 'hit' point at the back will be stored in info

        Seek(info.point + chosenDir.normalized * hideDistance);
    }

    bool CanSeeTarget()
    {
        RaycastHit raycastInfo; 
        Vector3 rayToTarget = target.transform.position - this.transform.position; 
        float lookAngle = Vector3.Angle(this.transform.forward, rayToTarget); // calc angle 
        if(lookAngle < 60 && Physics.Raycast(this.transform.position, rayToTarget, out raycastInfo))
        {
            if (raycastInfo.transform.gameObject.tag == "cop")
            { 
                return true; 
            }
        }
        return false;
    }

    bool CanSeeMe() 
    {
        // Can my target see me 
        // Return TRUE if they can and FALSE if they can't 

        // TODO: 
        // Draw ray to target 
        // get target's look direction 
        // create angle 
        // check 
        
        Vector3 rayToTarget = this.transform.position - target.transform.position; 
        float lookAngle = Vector3.Angle(target.transform.forward, rayToTarget); 

        if (lookAngle < 60)
        {
            return true; 
        }

        return false;
    }

    void BehaviourCooldown() 
    {
        cooldown = false;
    }

} // End of Class
