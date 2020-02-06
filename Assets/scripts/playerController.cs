using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterControllerSettings
{
    public CharacterControllerSettings() { }
    public CharacterControllerSettings(CharacterController controller) {
        Center = controller.center;
        Radius = controller.radius;
        Height = controller.height;
    }
    public Vector3 Center;
    public float Radius;
    public float Height;
    public void copyTo(CharacterController receivingController)
    {
        receivingController.center = Center;
        receivingController.radius = Radius;
        receivingController.height = Height;
    }
}

[RequireComponent(typeof(Camera))]
[RequireComponent(typeof(CharacterController))]
//[RequireComponent(typeof(Animator))]
public class playerController : MonoBehaviour
{
    CharacterControllerSettings walkSettings;
    CharacterControllerSettings swimSettings;

    /// <summary>
    /// Get saved and swapped in when the character is swimming
    /// </summary>
    public CharacterController swimControllerSettings;
    CharacterController activeController;
    Animator animator;
    //public Camera mainCamera;
    /// <summary>
    /// Needs a camera, and the camera gameobject needs a character controller for swimming collisions
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

        activeController = GetComponent<CharacterController>();
        walkSettings = new CharacterControllerSettings(activeController);
        swimSettings = new CharacterControllerSettings(swimControllerSettings);

        animator = GetComponent<Animator>();
        //camera = GetComponent<Camera>();
        //headCollider = head.GetComponent<CapsuleCollider>();
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

    //private void OnCollisionEnter(Collision collision)
    //{

    //}
    private void OnCollisionStay(Collision collision)
    {
        print(collision.gameObject.name);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit) // for pushing other objects while moving
    {
        print("hit " + hit.gameObject.name);
        //moveOffset += hit.
    }

    private void FixedUpdate()
    {
        if (inAirZone && !underWaterSurface) isSwimming = false;
        else isSwimming = true;

        const float MIN_X = 0.0f; float MAX_X = 360.0f; const float MIN_Y = -90.0f; const float MAX_Y = 90.0f;

        //print(Input.GetAxis("Mouse X") + " X " + X);

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
                swimSettings.copyTo(activeController);

                //moveOffset = oldCamPosition - headCamera.transform.position;
                //print("Wasn't swimming. oldCamPosition " + oldCamPosition + " headCamera.transform.position " + headCamera.transform.position);
            }
        }
        else
        {
            Vector3 oldCamPosition = headCamera.transform.position;
            //characterController.enabled = true;

            transform.rotation = Quaternion.Euler(0, X, 0.0f);
            head.transform.localRotation = Quaternion.Euler(Y, 0, 0);

            if (wasSwimming)
            {
                headCamera.transform.localPosition = cameraOffsetWalking;
                walkSettings.copyTo(activeController);

                //moveOffset = oldCamPosition - headCamera.transform.position;
                //print("Was swimming. oldCamPosition " + oldCamPosition + " headCamera.transform.position " + headCamera.transform.position);
            }
        }
        //moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0.0f, Input.GetAxis("Vertical"));
        //moveDirection *= speed;
        if (!isSwimming)
        {
            if (activeController.isGrounded) moveDirection *= walkDrag;
            else moveDirection *= airDrag;
        }
        else moveDirection *= swimDrag;

        float accel = walkAccel;
        if (!isSwimming) { if (!activeController.isGrounded) accel = airAccel; }
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
        //moveDirection *= speed;

        //print("vel " + moveDirection);

        //moveDirection = moveDirection * transform.rotation;
        if(activeController.isGrounded) if (Input.GetButton("Jump")) moveDirection.y = jumpSpeed;

        // Apply gravity. Gravity is multiplied by deltaTime twice (once here, and once below
        // when the moveDirection is multiplied by deltaTime). This is because gravity should be applied
        // as an acceleration (ms^-2)
        if (!isSwimming) moveDirection.y -= gravity * Time.fixedDeltaTime;

        // Move the controller
        activeController.Move(moveDirection * Time.deltaTime + moveOffset);
        moveOffset = Vector3.zero;

        animator.SetFloat("speed", moveDirection.magnitude);
        animator.SetBool("IsSwimming", isSwimming);

        neckbone.localScale = Vector3.zero; // hide the head

        inAirZone = false;
        underWaterSurface = false;
        wasSwimming = isSwimming;
    }
}
