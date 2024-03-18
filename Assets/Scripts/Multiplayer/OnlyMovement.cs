using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class OnlyMovement : NetworkBehaviour {
    //Movement Variables
    public float walkSpeed = 6f;
    public float runSpeed = 12f;
    public float jumpPower = 7f;
    public float gravity = 20f;
    public float lookSpeed = 2f;
    public float lookXLimit = 45f;
    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0;

    //Projectile Variables
    public Transform LHFirePoint, RHFirePoint;
    private Transform projectile;
    public GameObject straightProjectilePrefab;
    public GameObject curvedProjectilePrefab;
    private float projectileSpeed = 40;
    public float fireRate = 4;
    private Vector3 destination;

    //cooldowns for straight attack
    public float cooldownDuration = 0.25f;
    private float cooldownTimer = 0.0f;
    public bool canMove = true;
    public bool canShoot = true;
    private float timeToFire;
    private bool isCooldownActive = false;

    //Other Variables
    private Camera playerCamera;
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
        //not done:
        HandleAnimation();
        //testing:
        HandleAttacks();
    }

    Camera CreatePlayerCamera() {
        GameObject cameraObject = new GameObject("PlayerCamera");
        cameraObject.transform.SetParent(transform);
        Camera newCamera = cameraObject.AddComponent<Camera>();
        // Shift camera up by one unit
        cameraObject.transform.localPosition += (Vector3.up / 2);
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

    void HandleAttacks() {
        CheckForProjectileFired();
        UpdateCooldown();
    }

    //---TESTING ---


    void CheckForProjectileFired() {
        if (!isCooldownActive && canShoot) {
            #region Check Fire1 Button
            if (Input.GetButtonDown("Fire1") && Time.time >= timeToFire) {
                StartCooldown();
                timeToFire = Time.time + 1 / fireRate;

                StartCoroutine(DelayedProjectileFiring(LHFirePoint));
            }
            #endregion

            #region Check Fire2 Button
            if (Input.GetButtonDown("Fire2") && Time.time >= timeToFire) {
                StartCooldown();
                timeToFire = Time.time + 1 / fireRate;

                StartCoroutine(DelayedProjectileFiring(RHFirePoint));
            }
            #endregion
        }
    }

    IEnumerator DelayedProjectileFiring(Transform firePoint) {
        canShoot = false; // Set canShoot to false at the beginning

        yield return new WaitForSeconds(0.5f); // Adjust the delay duration as needed

        if (Input.GetKey(KeyCode.LeftShift)) {
            // If left shift is held down, shoot curved projectile from LHFirePoint
            ShootCurvedProjectile(firePoint);
            StartCoroutine(DelayAfterFiring());
        }
        else {
            // If not holding shift, alternate shooting quick projectile between left and right hands
            ShootStraightProjectile(firePoint);
            StartCoroutine(DelayAfterFiring());
        }
    }

    IEnumerator DelayAfterFiring() {
        yield return new WaitForSeconds(1.0f);
        canShoot = true;
    }

    void StartCooldown() {
        isCooldownActive = true;
        cooldownTimer = cooldownDuration;
        canMove = false;
    }

    void UpdateCooldown() {
        if (isCooldownActive) {
            cooldownTimer -= Time.deltaTime;

            if (cooldownTimer <= 0.0f) {
                isCooldownActive = false;
                canMove = true;
            }
        }
    }

    void InstantiateStraightProjectile(Transform firePoint) {
        //Cant pass firepoint... need to fix RH and LH later
        InstantiateStraightProjectileServerRPC();
    }

    [ServerRpc (RequireOwnership = false)]
    void InstantiateStraightProjectileServerRPC () {
        var projectileObj = Instantiate(straightProjectilePrefab, LHFirePoint.position, Quaternion.identity) as GameObject;

        //NEW MULTIPLAYER LOGIC:
        NetworkObject straightProjectileNetworkObject = projectileObj.transform.GetComponent<NetworkObject>();
        straightProjectileNetworkObject.Spawn(true);

        projectileObj.GetComponent<Rigidbody>().velocity = (destination - LHFirePoint.position).normalized * projectileSpeed;
    }

    void ShootStraightProjectile(Transform firePoint) {
        //Creates a ray from the center of our screen to the target
        Ray projectileRay = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0.0f));

        RaycastHit raycastHit;

        if (Physics.Raycast(projectileRay, out raycastHit)) {
            destination = raycastHit.point;
        }
        //can get a point hit up to 1000 units away
        else {
            destination = projectileRay.GetPoint(1000);
        }
        InstantiateStraightProjectile(firePoint);
    }

    void ShootCurvedProjectile(Transform firePoint) {
        //Creates a ray from the center of our screen to the target
        Ray projectileRay = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0.0f));

        RaycastHit raycastHit;

        if (Physics.Raycast(projectileRay, out raycastHit)) {
            destination = raycastHit.point;
        }
        //if there is no raycasthit, get a point X units from raycast
        else {
            destination = projectileRay.GetPoint(10);
        }
        if (firePoint == LHFirePoint) {
            InstantiateCurvedProjectile(firePoint, destination, true);
        }
        else {
            InstantiateCurvedProjectile(firePoint, destination, false);
        }
    }

    public void InstantiateCurvedProjectile(Transform firePoint, Vector3 destination, bool isLeftHand) {
        projectile = Instantiate(curvedProjectilePrefab, firePoint.position, Quaternion.identity).transform;

        //NEW MULTIPLAYER LOGIC: 
        NetworkObject curvedProjectileNetworkObject = projectile.GetComponent<NetworkObject>();
        curvedProjectileNetworkObject.Spawn(true);

        StartCoroutine(MoveProjectile(projectile, firePoint.position, destination, isLeftHand));
    }

    IEnumerator MoveProjectile(Transform projectile, Vector3 startPoint, Vector3 endPoint, bool isLeftHand) {
        float elapsedTime = 0f;
        float duration = .5f;

        while (elapsedTime < duration) {
            projectile.position = CreateCurve(elapsedTime / duration, endPoint, startPoint, isLeftHand);
            projectile.forward = CreateCurve((elapsedTime + 0.001f) / duration, endPoint, startPoint, isLeftHand) - projectile.position;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        projectile.position = endPoint; // Ensure the final position is exactly the destination
    }

    Vector3 CreateCurve(float t, Vector3 destination, Vector3 startPoint, bool isLeftHand) {
        float offsetAmount = 10.0f; // Adjust this value based on how much you want to offset to the left
        Vector3 offset;
        Vector3 direction = (destination - LHFirePoint.position).normalized;
        if (isLeftHand) {
            offset = Vector3.Cross(direction, Vector3.up) * offsetAmount; // Calculate the left offset
        }
        else {
            offset = -Vector3.Cross(direction, Vector3.up) * offsetAmount; // Calculate the right offset
        }

        Vector3 ac = Vector3.Lerp(LHFirePoint.position, destination + offset, t);
        Vector3 cb = Vector3.Lerp(destination + offset, destination, t);
        return Vector3.Lerp(ac, cb, t);
    }
}
