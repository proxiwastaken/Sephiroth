using UnityEngine;

public class OverheadController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotationSmoothTime = 0.1f;

    [Header("Camera Settings")]
    public Transform cameraTransform;
    public float cameraHeight = 15f;
    public float cameraAngle = 45f; // Angle for isometric
    public float cameraDistance = 10f; // How far back the camera sits
    public bool useOrthographic = true; // animal crossing
    public float orthographicSize = 8f;

    [Header("Camera Collision")]
    public bool enableCameraCollision = true;
    public LayerMask wallLayerMask = -1; // What layers count as walls
    public float cameraCollisionRadius = 0.5f; // Camera collision sphere radius
    public float minCameraDistance = 2f; // Minimum distance camera can be from player
    public float collisionSmoothTime = 0.2f; // How quickly camera adjusts when hitting walls

    [Header("Camera Controls")]
    public bool allowCameraRotation = false;
    public float cameraRotationSpeed = 2f;

    private CharacterController characterController;
    private Camera cam;
    private float rotationVelocity;
    private Vector3 velocity;
    private bool isGrounded;
    private Vector3 cameraOffset;

    // Camera collision variables
    private float currentCameraDistance;
    private float targetCameraDistance;
    private float cameraDistanceVelocity;

    void Start()
    {
        characterController = GetComponent<CharacterController>();

        // Set up camera if not assigned
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }

        cam = cameraTransform.GetComponent<Camera>();

        // Initialize camera distance
        currentCameraDistance = cameraDistance;
        targetCameraDistance = cameraDistance;

        SetupCamera();
        CalculateCameraOffset();
    }

    void SetupCamera()
    {
        if (cam == null) return;

        // Set camera projection
        if (useOrthographic)
        {
            cam.orthographic = true;
            cam.orthographicSize = orthographicSize;
        }
        else
        {
            cam.orthographic = false;
            cam.fieldOfView = 60f; // Perspective FOV
        }

        // Position and angle the camera for overhead view
        PositionCamera();
    }

    void CalculateCameraOffset()
    {
        // Calculate the offset from player to camera based on angle and current distance
        Vector3 direction = Quaternion.Euler(cameraAngle, 0, 0) * Vector3.back;
        cameraOffset = direction * currentCameraDistance + Vector3.up * cameraHeight;
    }

    void PositionCamera()
    {
        if (cameraTransform == null) return;

        // Position camera above and at an angle
        cameraTransform.position = transform.position + cameraOffset;

        // Look at the player with the specified angle
        cameraTransform.LookAt(transform.position + Vector3.up);

        // Adjust the angle for isometric 
        Vector3 euler = cameraTransform.eulerAngles;
        euler.x = cameraAngle;
        cameraTransform.rotation = Quaternion.Euler(euler);
    }

    void Update()
    {
        HandleGroundCheck();
        HandleMovement();
        HandleCameraControls();
        HandleCameraCollision();
        HandleCameraFollow();
        HandleGravity();

        // UI interaction
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = Cursor.lockState == CursorLockMode.Locked ?
                CursorLockMode.None : CursorLockMode.Locked;
        }
    }

    void HandleCameraCollision()
    {
        if (!enableCameraCollision || cameraTransform == null)
        {
            targetCameraDistance = cameraDistance;
        }
        else
        {
            CheckCameraCollision();
        }

        // Smoothly adjust camera distance
        currentCameraDistance = Mathf.SmoothDamp(currentCameraDistance, targetCameraDistance,
            ref cameraDistanceVelocity, collisionSmoothTime);

        // Recalculate offset with new distance
        CalculateCameraOffset();
    }

    void CheckCameraCollision()
    {
        Vector3 playerPosition = transform.position;
        Vector3 desiredCameraDirection = Quaternion.Euler(cameraAngle, 0, 0) * Vector3.back;
        Vector3 desiredCameraPosition = playerPosition + desiredCameraDirection * cameraDistance + Vector3.up * cameraHeight;

        // Cast a ray from player to desired camera position
        Vector3 rayDirection = (desiredCameraPosition - playerPosition).normalized;
        float maxDistance = Vector3.Distance(playerPosition, desiredCameraPosition);

        RaycastHit hit;
        if (Physics.SphereCast(playerPosition, cameraCollisionRadius, rayDirection, out hit, maxDistance, wallLayerMask))
        {
            float safeDistance = hit.distance - cameraCollisionRadius;
            Vector3 hitPoint = playerPosition + rayDirection * safeDistance;
            Vector3 directionToHit = hitPoint - playerPosition;


            Vector3 horizontalDirection = new Vector3(directionToHit.x, 0, directionToHit.z);
            float horizontalDistance = horizontalDirection.magnitude;

            float adjustedCameraDistance = horizontalDistance / desiredCameraDirection.magnitude;

            targetCameraDistance = Mathf.Max(adjustedCameraDistance, minCameraDistance);
        }
        else
        {
            targetCameraDistance = cameraDistance;
        }
    }

    void HandleGroundCheck()
    {
        isGrounded = characterController.isGrounded;
    }

    void HandleMovement()
    {
        // Get input
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 inputDirection = new Vector3(horizontal, 0f, vertical).normalized;

        if (inputDirection.magnitude >= 0.1f)
        {
            // For overhead view, we want movement relative to world axes
            Vector3 moveDirection;

            if (allowCameraRotation)
            {
                // Move relative to camera orientation
                Vector3 cameraForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
                Vector3 cameraRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;

                moveDirection = (cameraForward * inputDirection.z + cameraRight * inputDirection.x);
            }
            else
            {
                // Move relative to world axes (classic Animal Crossing style)
                moveDirection = inputDirection;
            }

            // Move character
            characterController.Move(moveDirection * moveSpeed * Time.deltaTime);

            // Rotate character to face movement direction
            if (moveDirection != Vector3.zero)
            {
                float targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref rotationVelocity, rotationSmoothTime);
                transform.rotation = Quaternion.Euler(0f, angle, 0f);
            }
        }
    }

    void HandleCameraControls()
    {
        if (allowCameraRotation)
        {
            // camera rotation Q and E
            if (Input.GetKey(KeyCode.Q))
            {
                RotateCamera(-cameraRotationSpeed * Time.deltaTime);
            }
            if (Input.GetKey(KeyCode.E))
            {
                RotateCamera(cameraRotationSpeed * Time.deltaTime);
            }
        }

        // Zoom in/out with scroll wheel
        if (useOrthographic && cam != null)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0f)
            {
                orthographicSize = Mathf.Clamp(orthographicSize - scroll * 2f, 3f, 15f);
                cam.orthographicSize = orthographicSize;
            }
        }
    }

    void RotateCamera(float rotationAmount)
    {
        cameraTransform.RotateAround(transform.position, Vector3.up, rotationAmount);
        CalculateCameraOffset();
    }

    void HandleCameraFollow()
    {
        if (cameraTransform == null) return;

        // Smoothly follow the player with collision-adjusted position
        Vector3 targetPosition = transform.position + cameraOffset;
        cameraTransform.position = Vector3.Lerp(cameraTransform.position, targetPosition, Time.deltaTime * 5f);

        // Always look at player
        // cameraTransform.LookAt(transform.position + Vector3.up);
    }

    void HandleGravity()
    {
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        velocity.y += Physics.gravity.y * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }

    public void SetCameraHeight(float height)
    {
        cameraHeight = height;
        CalculateCameraOffset();
    }

    public void SetCameraAngle(float angle)
    {
        cameraAngle = angle;
        CalculateCameraOffset();
        PositionCamera();
    }

    public void SetCameraDistance(float distance)
    {
        cameraDistance = distance;
        targetCameraDistance = distance;
    }

    public void SetOrthographicSize(float size)
    {
        if (cam != null && useOrthographic)
        {
            orthographicSize = size;
            cam.orthographicSize = orthographicSize;
        }
    }

    // Debug visualization
    void OnDrawGizmosSelected()
    {
        if (!enableCameraCollision || cameraTransform == null) return;

        // Draw the collision detection ray
        Vector3 playerPosition = transform.position;
        Vector3 desiredCameraDirection = Quaternion.Euler(cameraAngle, 0, 0) * Vector3.back;
        Vector3 desiredCameraPosition = playerPosition + desiredCameraDirection * cameraDistance + Vector3.up * cameraHeight;

        // Draw ray from player to desired camera position
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(playerPosition, desiredCameraPosition);

        // Draw collision sphere at camera position
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(cameraTransform.position, cameraCollisionRadius);

        // Draw min distance sphere
        Gizmos.color = Color.green;
        Vector3 minDistancePos = playerPosition + desiredCameraDirection * minCameraDistance + Vector3.up * cameraHeight;
        Gizmos.DrawWireSphere(minDistancePos, cameraCollisionRadius);
    }
}
