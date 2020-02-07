using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class busScript : MonoBehaviour
{
    /// <summary>
    /// The bus that interacts with the scene, but not the player
    /// </summary>
    public Transform physicalBus;
    Rigidbody physicalRB;
    Rigidbody visualRB;

    public float forwardPower = 50000;
    public float rotationPower = 20000;

    // Start is called before the first frame update
    void Start()
    {
        visualRB = GetComponent<Rigidbody>();
        physicalRB = physicalBus.GetComponent<Rigidbody>();

        physicalRB.centerOfMass = new Vector3(0, 1,0);
    }

    private void FixedUpdate()
    {
        Vector3 testVelocity = Vector3.zero;
        if (Input.GetKey(KeyCode.I)) physicalRB.AddRelativeTorque(new Vector3(-rotationPower, 0, 0));
        if (Input.GetKey(KeyCode.K)) physicalRB.AddRelativeTorque(new Vector3(rotationPower, 0, 0));
        if (Input.GetKey(KeyCode.J)) physicalRB.AddRelativeTorque(new Vector3(0,-rotationPower,0));
        if (Input.GetKey(KeyCode.L)) physicalRB.AddRelativeTorque(new Vector3(0,rotationPower,0));

        if (Input.GetKey(KeyCode.H)) physicalRB.AddRelativeForce(new Vector3(0, 0, -forwardPower));
        if (Input.GetKey(KeyCode.G)) physicalRB.AddRelativeForce(new Vector3(0, 0, forwardPower));

        //Quaternion lean = Quaternion.identity * Quaternion.Inverse( physicalBus.rotation);
        //physicalRB.AddRelativeTorque(lean.eulerAngles * 5000);

        Quaternion rot1 = Quaternion.FromToRotation(physicalBus.up, -Vector3.up);
        Vector3 rot = rot1.eulerAngles;
        rot -= new Vector3(180, 180, 180);
        //rot.Scale(rot);
        //physicalRB.AddRelativeTorque(new Vector3(rot.x, 0, rot.z) * 100);
        //print("rot " + rot);

        //physicalRB.AddForceAtPosition(new Vector3(0, 1000,0), physicalBus.position + new Vector3(0, 10, 0));    // it just death spins
        physicalRB.AddForce(new Vector3(0, 3000, 0));   // float force

        visualRB.MovePosition(physicalBus.position);
        visualRB.MoveRotation(physicalBus.rotation);
        //physicalRB.MovePosition(transform.position + -transform.forward * 1 * Time.fixedDeltaTime);
    }
}
