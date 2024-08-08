using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Bot : MonoBehaviour
{
    NavMeshAgent agent;
    public GameObject target;  // find the cop

    // Start is called before the first frame update
    void Start()
    {
        agent = this.GetComponent<NavMeshAgent>();
    }

    // Update is called once per frame
    void Update()
    {
        // Flee(target.transform.position); // find the cop and set as target. 
        Persue();
    }

    void Seek(Vector3 location)
    {
        agent.SetDestination(location);
    }

    void Flee(Vector3 location)
    {
        Vector3 fleeVector = location - this.transform.position;
        agent.SetDestination(this.transform.position - fleeVector);
    }

    void Persue() // Persue is different from Seek as it predicts where the target will be to intercept them
    {
        Vector3 targetDir = target.transform.position - this.transform.position;

        float relativeHeading = Vector3.Angle(this.transform.forward, this.transform.TransformVector(target.transform.forward)); // Angle between their forward directions 
        float toTarget = Vector3.Angle(this.transform.forward, this.transform.TransformVector(targetDir));

        // Check target isn't behind us already, but in a way that makes sense
        // OR has stopped! 
        if ((toTarget > 90 && relativeHeading < 20) ||  target.GetComponent<Drive>().currentSpeed < 0.01f) // note not doing == 0 as can have weird glitches
        {
            Seek(target.transform.position);
            Debug.Log("Seeking...");
            return;
        }

        Debug.Log("Persuing");

        //calc look ahead 
        float lookAhead = targetDir.magnitude/(agent.speed + target.GetComponent<Drive>().currentSpeed);
        Seek(target.transform.position + target.transform.forward * lookAhead); // note that forward = 1 as already normalized for us

    }
}
