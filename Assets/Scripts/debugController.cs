using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class debugController : MonoBehaviour
{
    public static debugController Instance { get; private set; }

    [Header("Noclip")]
    [SerializeField] private KeyCode noclipToggleKey = KeyCode.Insert;
    [SerializeField] private float noclipSpeed = 8f;
    [SerializeField] private float noclipSprintMultiplier = 2f;
    [SerializeField] private KeyCode noclipUpKey = KeyCode.Space;
    [SerializeField] private KeyCode noclipDownKey = KeyCode.LeftControl;

    [Header("Scene Hotkeys")]
    [SerializeField] private KeyCode menuSceneKey = KeyCode.F1;
    [SerializeField] private KeyCode homeSceneKey = KeyCode.F2;
    [SerializeField] private KeyCode level1SceneKey = KeyCode.F3;
    [SerializeField] private KeyCode joelSceneKey = KeyCode.F4;
    [SerializeField] private string menuSceneName = "Menu";
    [SerializeField] private string homeSceneName = "HomeScene";
    [SerializeField] private string level1SceneName = "Level1";
    [SerializeField] private string joelSceneName = "JoelsScene";

    private GameObject playerRoot;
    private OverheadController overheadController;
    private PlayerMotor playerMotor;
    private CharacterController characterController;
    private Rigidbody playerRigidbody;
    private Collider[] playerColliders;
    private bool[] playerColliderEnabledStates;
    private bool noclipEnabled;
    private bool playerMovementWasEnabled = true;
    private bool characterControllerWasEnabled = true;
    private bool rigidbodyWasKinematic;
    private bool rigidbodyUsedGravity;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        ResolvePlayerReferences();
        CapturePlayerState();
        ApplyNoclipState(false);
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Update()
    {
        HandleHotkeys();

        if (noclipEnabled)
        {
            UpdateNoclipMovement();
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResolvePlayerReferences();

        if (!noclipEnabled)
        {
            CapturePlayerState();
            return;
        }

        CapturePlayerState();

        if (noclipEnabled)
        {
            ApplyNoclipState(true);
        }
    }

    void HandleHotkeys()
    {
        if (Input.GetKeyDown(noclipToggleKey))
        {
            ToggleNoclip();
        }

        if (Input.GetKeyDown(menuSceneKey))
        {
            ForceLoadScene(menuSceneName);
        }
        else if (Input.GetKeyDown(homeSceneKey))
        {
            ForceLoadScene(homeSceneName);
        }
        else if (Input.GetKeyDown(level1SceneKey))
        {
            ForceLoadScene(level1SceneName);
        }
        else if (Input.GetKeyDown(joelSceneKey))
        {
            ForceLoadScene(joelSceneName);
        }
    }

    void ToggleNoclip()
    {
        noclipEnabled = !noclipEnabled;
        ResolvePlayerReferences();

        if (noclipEnabled)
            CapturePlayerState();

        ApplyNoclipState(noclipEnabled);
        Debug.Log($"Debug noclip {(noclipEnabled ? "enabled" : "disabled")}");
    }

    void ApplyNoclipState(bool enabled)
    {
        if (playerRoot == null)
            return;

        if (overheadController != null)
            overheadController.SetMovementEnabled(enabled ? false : playerMovementWasEnabled);

        if (playerMotor != null)
            playerMotor.SetMovementEnabled(enabled ? false : playerMovementWasEnabled);

        if (characterController != null)
            characterController.enabled = enabled ? false : characterControllerWasEnabled;

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
            playerRigidbody.isKinematic = enabled ? true : rigidbodyWasKinematic;
            playerRigidbody.useGravity = enabled ? false : rigidbodyUsedGravity;
        }

        if (playerColliders != null && playerColliderEnabledStates != null)
        {
            for (int i = 0; i < playerColliders.Length; i++)
            {
                Collider playerCollider = playerColliders[i];
                if (playerCollider == null || playerCollider.isTrigger)
                    continue;

                playerCollider.enabled = enabled ? false : playerColliderEnabledStates[i];
            }
        }
    }

    void UpdateNoclipMovement()
    {
        if (playerRoot == null)
            return;

        Transform cameraTransform = Camera.main != null ? Camera.main.transform : null;
        Vector3 forward = cameraTransform != null ? cameraTransform.forward : transform.forward;
        Vector3 right = cameraTransform != null ? cameraTransform.right : transform.right;
        Vector3 up = Vector3.up;

        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        float climb = 0f;

        if (Input.GetKey(noclipUpKey))
            climb += 1f;
        if (Input.GetKey(noclipDownKey))
            climb -= 1f;

        float speed = noclipSpeed * (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? noclipSprintMultiplier : 1f);
        Vector3 movement = (forward * vertical + right * horizontal + up * climb) * speed * Time.deltaTime;

        playerRoot.transform.position += movement;

        if (playerRigidbody != null && playerRigidbody.isKinematic)
        {
            playerRigidbody.MovePosition(playerRoot.transform.position);
        }
    }

    void ResolvePlayerReferences()
    {
        playerRoot = GameObject.FindWithTag("Player");

        if (playerRoot == null)
        {
            overheadController = FindFirstObjectByType<OverheadController>();
            if (overheadController != null)
                playerRoot = overheadController.gameObject;
        }

        if (playerRoot == null)
        {
            playerMotor = FindFirstObjectByType<PlayerMotor>();
            if (playerMotor != null)
                playerRoot = playerMotor.gameObject;
        }

        if (playerRoot == null)
        {
            characterController = FindFirstObjectByType<CharacterController>();
            if (characterController != null)
                playerRoot = characterController.gameObject;
        }

        if (playerRoot == null)
        {
            playerRigidbody = FindFirstObjectByType<Rigidbody>();
            if (playerRigidbody != null)
                playerRoot = playerRigidbody.gameObject;
        }

        if (playerRoot == null)
            return;

        overheadController = playerRoot.GetComponent<OverheadController>() ?? playerRoot.GetComponentInParent<OverheadController>();
        playerMotor = playerRoot.GetComponent<PlayerMotor>() ?? playerRoot.GetComponentInParent<PlayerMotor>();
        characterController = playerRoot.GetComponent<CharacterController>() ?? playerRoot.GetComponentInParent<CharacterController>();
        playerRigidbody = playerRoot.GetComponent<Rigidbody>() ?? playerRoot.GetComponentInParent<Rigidbody>();
        playerColliders = playerRoot.GetComponentsInChildren<Collider>(true);
    }

    void CapturePlayerState()
    {
        if (playerRoot == null)
            return;

        if (overheadController != null)
            playerMovementWasEnabled = overheadController.IsMovementEnabled();

        if (playerMotor != null)
            playerMovementWasEnabled = playerMotor.IsMovementEnabled();

        if (characterController != null)
            characterControllerWasEnabled = characterController.enabled;

        if (playerRigidbody != null)
        {
            rigidbodyWasKinematic = playerRigidbody.isKinematic;
            rigidbodyUsedGravity = playerRigidbody.useGravity;
        }

        if (playerColliders == null)
            playerColliders = playerRoot.GetComponentsInChildren<Collider>(true);

        playerColliderEnabledStates = new bool[playerColliders.Length];
        for (int i = 0; i < playerColliders.Length; i++)
        {
            playerColliderEnabledStates[i] = playerColliders[i] != null && playerColliders[i].enabled;
        }
    }

    void ForceLoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("DebugController: Scene name is empty.");
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}
