using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class OnlyMovement : NetworkBehaviour {
    public float walkSpeed = 6f;
    public float runSpeed = 12f;
    public float jumpPower = 7f;
    public float gravity = 20f;
    public float lookSpeed = 2f;
    public float lookXLimit = 45f;

    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0;
    public Camera playerCamera;

    CharacterController characterController;
    Animator characterAnimations;

    void Start() {
        if (!IsOwner) return;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        characterController = GetComponent<CharacterController>();

        Transform childTransform = transform.Find("Y Bot Character");
        characterAnimations = childTransform.GetComponent<Animator>();

        playerCamera = CreatePlayerCamera();
    }

    void Update() {
        if (!IsOwner) return;

        HandleMovement();
        HandleRotation();
        //not done, for debugging
        HandleAnimation();
    }

    Camera CreatePlayerCamera() {
        GameObject cameraObject = new GameObject("PlayerCamera");
        cameraObject.transform.SetParent(transform);
        Camera newCamera = cameraObject.AddComponent<Camera>();
        // Add any additional settings you need for the camera
        return newCamera;
    }

    void HandleMovement() {
        //if (!IsOwner) return;

        //if (characterController.canMove) {
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float curSpeedX = (isRunning ? runSpeed : walkSpeed) * Input.GetAxis("Vertical");
        float curSpeedY = (isRunning ? runSpeed : walkSpeed) * Input.GetAxis("Horizontal");

        float movementDirectionY = moveDirection.y;
        moveDirection = (forward * curSpeedX) + (right * curSpeedY);

        if (Input.GetButton("Jump") && characterController.isGrounded) {
            moveDirection.y = jumpPower;
        }
        else {
            moveDirection.y = movementDirectionY;
        }

        if (!characterController.isGrounded) {
            moveDirection.y -= gravity * Time.deltaTime;
        }

        characterController.Move(moveDirection * Time.deltaTime);
        //}
    }

    void HandleRotation() {
        //if (!IsOwner) return;

        //if (fpsProjectile.canMove) {
        rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
        transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);
        //}
    }

    void HandleAnimation() {
        if (Input.GetKeyDown(KeyCode.Mouse0)) {
            characterAnimations.SetBool("LeftStraight", true);
        }
    }
}
