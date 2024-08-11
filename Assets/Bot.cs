using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;
using UnityEngine.AI;

public class Bot : MonoBehaviour
{
    NavMeshAgent agent;
    public GameObject target;  // find the cop
    Drive ds;

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
        Evade();
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


} // End of Class
