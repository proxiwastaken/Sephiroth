using UnityEngine;

public class ThirdPersonController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotationSmoothTime = 0.1f;

    [Header("Camera")]
    public Transform cameraTransform;
    public float mouseSensitivity = 2f;
    public float minYAngle = -30f;
    public float maxYAngle = 60f;
    public float cameraDistance = 5f;
    public float cameraHeight = 2f;

    private CharacterController characterController;
    private float rotationVelocity;
    private float verticalRotation = 0f;
    private Vector3 velocity;
    private bool isGrounded;

    void Start()
    {
        characterController = GetComponent<CharacterController>();

        // Lock cursor to center of screen
        Cursor.lockState = CursorLockMode.Locked;

        // Set up camera if not assigned
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        HandleGroundCheck();
        HandleMovement();
        HandleRotation();
        HandleCamera();
        HandleGravity();
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

        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        if (direction.magnitude >= 0.1f)
        {
            // Calculate movement relative to camera
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;
            Vector3 moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

            // Move character
            characterController.Move(moveDirection.normalized * moveSpeed * Time.deltaTime);

            // Rotate character towards movement direction
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref rotationVelocity, rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
        }
    }

    void HandleRotation()
    {
        // Mouse look for camera
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, minYAngle, maxYAngle);

        cameraTransform.RotateAround(transform.position, Vector3.up, mouseX);
    }

    void HandleCamera()
    {
        // Position camera behind and above player
        Vector3 targetPosition = transform.position
            - cameraTransform.forward * cameraDistance
            + Vector3.up * cameraHeight;

        cameraTransform.position = targetPosition;

        // Apply vertical rotation
        Vector3 euler = cameraTransform.eulerAngles;
        cameraTransform.rotation = Quaternion.Euler(verticalRotation, euler.y, 0);
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
}

