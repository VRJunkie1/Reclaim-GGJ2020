using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SwordFish : MonoBehaviour
{
    public triggerZone nose;
    public Rigidbody rb;

    // Start is called before the first frame update
    void Start()
    {
        nose.onTriggerStay += noseHit;
        rb = GetComponent<Rigidbody>();
    }

    void noseHit(Collider other)
    {
        print("nose " + other.gameObject.name);
    }

    // Update is called once per frame
    void Update()
    {
        //float sidewaysDrag = .7f;


        Vector3 forwardSpeed = rb.velocity;
        forwardSpeed.Scale(-transform.forward);
        print("rb.velocity " + rb.velocity.magnitude + " velocity " + forwardSpeed.magnitude);
        print("sideways " + (rb.velocity.magnitude - forwardSpeed.magnitude));

        rb.velocity = forwardSpeed + (rb.velocity.magnitude - forwardSpeed.magnitude) * transform.forward;  // trying to make it drift forward and drag in other directions, when not moving under power
        //rb.velocity 
    }
}
