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
    public triggerZone frontWheels;
    public triggerZone backWheels;

    Rigidbody physicalRB;
    Rigidbody visualRB;

    public float forwardPower = 50000;
    public float rotationPower = 20000;
    public float forwardTilt = 20;
    public float uprightForce = 1000;
    public float maxFloatFoce = 3000;

    public float drivePower = 50000;

    // Start is called before the first frame update
    void Start()
    {
        visualRB = GetComponent<Rigidbody>();
        physicalRB = physicalBus.GetComponent<Rigidbody>();

        physicalRB.centerOfMass = new Vector3(0, 1,0);

        frontWheels.onTriggerStay += frontDrive;
        backWheels.onTriggerStay += backDrive;
    }

    void frontDrive(Collider other)
    {
        //print("front");
        if (Input.GetKey(KeyCode.H) || Input.GetKey(KeyCode.G))
        {
            Vector3 force;
            if (Input.GetKey(KeyCode.J)) force = physicalBus.right + -physicalBus.forward*.5f;
            else if (Input.GetKey(KeyCode.L)) force = -physicalBus.right + -physicalBus.forward*.5f;
            else force = -physicalBus.forward;
            force = force.normalized * drivePower;
            if (Input.GetKey(KeyCode.G)) force = -force;
            print("force " + force);
            physicalRB.AddForceAtPosition(force, frontWheels.transform.position + frontWheels.transform.up * .1f);
        }
    }
    void backDrive(Collider other)
    {
        //print("back");
        if (Input.GetKey(KeyCode.H)) physicalRB.AddRelativeForce(new Vector3(0, 0, -drivePower));
        if (Input.GetKey(KeyCode.G)) physicalRB.AddRelativeForce(new Vector3(0, 0, drivePower));
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

        // Force it upright
        Vector3 angles = physicalBus.localRotation.eulerAngles;
        angles.x += forwardTilt;
        angles.x = angles.x > 180 ? angles.x - 360 : angles.x;
        angles.y = 0;//angles.y > 180 ? angles.y - 360 : angles.y;
        angles.z = angles.z > 180 ? angles.z - 360 : angles.z;
        physicalRB.AddRelativeTorque(-angles * uprightForce);

        //print(angles);

        //physicalRB.AddForceAtPosition(new Vector3(0, 1000,0), physicalBus.position + new Vector3(0, 10, 0));    // it just death spins
        physicalRB.AddForce(new Vector3(0, maxFloatFoce, 0));   // float force

        visualRB.MovePosition(physicalBus.position);
        visualRB.MoveRotation(physicalBus.rotation);
        //physicalRB.MovePosition(transform.position + -transform.forward * 1 * Time.fixedDeltaTime);
    }
}
