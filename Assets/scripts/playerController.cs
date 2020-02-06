using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(Camera))]
//[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class playerController : MonoBehaviour
{
    Rigidbody rigidbody;
    CapsuleCollider capsulecCollider;


    Animator animator;
    /// <summary>
    /// Needs a camera
    /// </summary>
    public Transform head;
    Camera headCamera;
    //CapsuleCollider headCollider;
    public float swimmingAngle = 45;
    /// <summary>
    /// Move the head down a bit so it's in the right place
    /// </summary>
    public Vector3 CameraOffsetSwimming = new Vector3(0, -.3f, .2f);
    Vector3 cameraOffsetWalking;

    public triggerZone waterDetector;   // if this is in water, character swims
    /// <summary>
    /// for scaling the head to 0
    /// </summary>
    public Transform neckbone;

    public float groundRayRadius = .5f;
    public float groundRayLength = .1f;

    public float walkSpeed = 6.0f;
    public float jumpSpeed = 8.0f;
    public float gravity = 9.81f;

    public float walkDrag = .8f;    // higher number means less drag; 1 is no drag
    public float walkAccel = .8f;
    public float airDrag = .95f;
    public float airAccel = .3f;

    public float swimSpeed = 10f;

    public float swimDrag = .9f;
    public float swimAccel = .5f;

    public bool isSwimming;
    public bool isGrounded; // feet touching ground
    public bool wasSwimming { get; private set; }

    // mouse
    float X; float Y;
    public float Sensitivity = 100;

    private Vector3 moveDirection = Vector3.zero;

    bool inAirZone = false;
    bool underWaterSurface = false;

    // For adding extra player movement. Note: this 'teleport' will half fail if stuff is in the way, it seems
    Vector3 moveOffset;
 
    void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        capsulecCollider = GetComponent<CapsuleCollider>();
        foreach (Transform tf in head.transform)
        {
            if (tf.GetComponent<Camera>() != null) { headCamera = tf.GetComponent<Camera>(); }
        }
        cameraOffsetWalking = headCamera.transform.localPosition;

        Vector3 euler = transform.rotation.eulerAngles;
        X = euler.x;
        Y = euler.y;

        waterDetector.ignoreChildCollisions = transform;
        waterDetector.onTriggerStay += waterStay;
    }

    public void waterStay(Collider other)
    {
        //print(other.gameObject.name);
        if (other.gameObject.name.ToLower().Contains("busairzone")) inAirZone = true;
        if (other.gameObject.name.ToLower().Contains("watersurface")) underWaterSurface = true;
    }


    private void OnCollisionStay(Collision collision)
    {
        //print(collision.gameObject.name);
    }

    private void FixedUpdate()
    {
        if (inAirZone && !underWaterSurface) isSwimming = false;
        else isSwimming = true;

        rigidbody.angularVelocity = Vector3.zero;

        RaycastHit hit;
        isGrounded = Physics.SphereCast(transform.position, groundRayRadius, -(transform.up), out hit, groundRayLength);
        if (isGrounded)
        {
            //transform.rotation = Quaternion.LookRotation(Vector3.Cross(transform.right, hit.normal)   // tilts character along ground
            //print(hit.collider.gameObject.name + Vector3.Angle(hit.normal, new Vector3(0,1,0)));
            // TODO: Stop character from walking up steep slopes
        }


        // Mouse
        const float MIN_X = 0.0f; float MAX_X = 360.0f; const float MIN_Y = -90.0f; const float MAX_Y = 90.0f;

        X += Input.GetAxis("Mouse X") * (Sensitivity * Time.deltaTime);
        if (X < MIN_X) X += MAX_X;
        else if (X > MAX_X) X -= MAX_X;
        Y -= Input.GetAxis("Mouse Y") * (Sensitivity * Time.deltaTime);
        if (Y < MIN_Y) Y = MIN_Y;
        else if (Y > MAX_Y) Y = MAX_Y;
        if (Cursor.lockState != CursorLockMode.Locked) 
        { Y = 0; X = transform.rotation.eulerAngles.y; print("X " + X); }
        Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible;


        if (isSwimming)
        {
            Vector3 oldCamPosition = headCamera.transform.position;

            transform.rotation = Quaternion.Euler(Y + swimmingAngle, X, 0.0f);
            head.transform.localRotation = Quaternion.Euler(-swimmingAngle, 0, 0);

            if (!wasSwimming)
            {
                headCamera.transform.localPosition = CameraOffsetSwimming;
            }
        }
        else
        {
            Vector3 oldCamPosition = headCamera.transform.position;

            transform.rotation = Quaternion.Euler(0, X, 0.0f);
            head.transform.localRotation = Quaternion.Euler(Y, 0, 0);

            if (wasSwimming)
            {
                headCamera.transform.localPosition = cameraOffsetWalking;
            }
        }

        if (!isSwimming)
        {
            if (isGrounded) moveDirection *= walkDrag;
            else moveDirection *= airDrag;
        }
        else moveDirection *= swimDrag;

        float accel = walkAccel;
        if (!isSwimming) { if (!isGrounded) accel = airAccel; }
        else accel = swimAccel;

        Vector3 targetVel;
        if (!isSwimming)
        {
            targetVel = (Input.GetAxisRaw("Horizontal") * transform.right + Input.GetAxisRaw("Vertical") * transform.forward);
        }
        else
        {
            targetVel = (Input.GetAxisRaw("Horizontal") * head.right + Input.GetAxisRaw("Vertical") * head.forward);
            targetVel += Input.GetAxisRaw("UpDown") * new Vector3(0, 1, 0);
        }

        targetVel = Vector3.Normalize(targetVel) * walkSpeed;
        moveDirection = moveDirection * (1- accel) + targetVel * accel;
        
        if (isGrounded) if (Input.GetButton("Jump")) moveDirection.y = jumpSpeed;

        // Apply gravity. Gravity is multiplied by deltaTime twice (once here, and once below
        // when the moveDirection is multiplied by deltaTime). This is because gravity should be applied
        // as an acceleration (ms^-2)
        if (!isSwimming) moveDirection.y -= gravity * Time.fixedDeltaTime;

        // Apply movement
        rigidbody.velocity = moveDirection;
        moveOffset = Vector3.zero;

        if (isSwimming) animator.SetFloat("speed", moveDirection.magnitude);
        else animator.SetFloat("speed", moveDirection.xz().magnitude);
        animator.SetBool("IsSwimming", isSwimming);

        neckbone.localScale = Vector3.zero; // hide the head

        inAirZone = false;
        underWaterSurface = false;
        wasSwimming = isSwimming;
    }
}
