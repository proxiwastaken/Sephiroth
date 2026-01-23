using UnityEngine;
using System.Collections;

public enum MushroomState
{
    Hidden,
    Idle,
    Alert,
    Fleeing,
    Collected
}

public class MushroomAI : MonoBehaviour
{
    [Header("Mushroom Configuration")]
    public MushroomData mushroomData;
    public Transform player;

    [Header("Visual")]
    public GameObject mushroomModel;
    public Animator animator;

    [Header("Detection")]
    public LayerMask playerLayer = 1;

    // State Management
    public MushroomState currentState = MushroomState.Hidden;
    private MushroomState previousState;

    // Components
    private MushroomPersonality personality;
    private Collider mushroomCollider;
    private Rigidbody rb;

    // Runtime data
    private float stateTimer;
    private bool playerInRange;
    private Vector3 originalPosition;
    private Vector3 fleeDirection;

    void Start()
    {
        InitializeMushroom();
    }

    void InitializeMushroom()
    {
        // Get components
        mushroomCollider = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();

        // Find player if not assigned
        if (player == null)
        {
            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        // Load personality behavior
        if (mushroomData != null && mushroomData.personalityPrefab != null)
        {
            var personalityObj = Instantiate(mushroomData.personalityPrefab, transform);
            personality = personalityObj.GetComponent<MushroomPersonality>();
            personality.Initialize(this, mushroomData);
        }

        // Store original position
        originalPosition = transform.position;

        // Set initial state
        ChangeState(MushroomState.Hidden);
    }

    void Update()
    {
        if (player == null || mushroomData == null) return;

        UpdateDetection();
        UpdateStateBehavior();
        UpdateVisuals();
    }

    void UpdateDetection()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        playerInRange = distanceToPlayer <= mushroomData.detectionRange;

        // Debug logging
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"Mushroom {name}: Distance={distanceToPlayer:F1}, InRange={playerInRange}, State={currentState}");
        }
    }

    void UpdateStateBehavior()
    {
        stateTimer += Time.deltaTime;

        // Let personality handle state logic
        if (personality != null)
        {
            personality.UpdateBehavior();
        }
        else
        {
            // Default behavior if no personality
            DefaultBehavior();
        }
    }

    void DefaultBehavior()
    {
        switch (currentState)
        {
            case MushroomState.Hidden:
                if (playerInRange)
                    ChangeState(MushroomState.Alert);
                break;

            case MushroomState.Alert:
                if (!playerInRange)
                    ChangeState(MushroomState.Hidden);
                break;
        }
    }

    void UpdateVisuals()
    {
        // Update model visibility and position based on state
        switch (currentState)
        {
            case MushroomState.Hidden:
                if (mushroomModel != null)
                {
                    Vector3 hiddenPos = originalPosition + Vector3.down * mushroomData.hideDepth;
                    mushroomModel.transform.position = Vector3.Lerp(
                        mushroomModel.transform.position,
                        hiddenPos,
                        Time.deltaTime * mushroomData.hideSpeed);
                }
                break;

            case MushroomState.Idle:
            case MushroomState.Alert:
                if (mushroomModel != null)
                {
                    mushroomModel.transform.position = Vector3.Lerp(
                        mushroomModel.transform.position,
                        originalPosition,
                        Time.deltaTime * mushroomData.hideSpeed);
                }
                break;
        }

        // animator setup for future states
        if (animator != null)
        {
            animator.SetInteger("State", (int)currentState);
        }
    }

    public void ChangeState(MushroomState newState)
    {
        if (currentState == newState) return;

        previousState = currentState;
        currentState = newState;
        stateTimer = 0f;

        // Notify personality of state change
        if (personality != null)
        {
            personality.OnStateChanged(previousState, currentState);
        }

        OnStateChanged();
    }

    public void StopMushroom()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
        }
    }

    public void UpdateFleeDirection(Vector3 newDirection)
    {
        fleeDirection = newDirection;
    }

    void OnDrawGizmosSelected()
    {
        if (mushroomData == null) return;

        // Detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, mushroomData.detectionRange);

        // Current flee direction
        if (currentState == MushroomState.Fleeing)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, fleeDirection * 3f);

            // Show current direction to player
            if (player != null)
            {
                Gizmos.color = Color.blue;
                Vector3 dirToPlayer = (player.position - transform.position).normalized;
                Gizmos.DrawRay(transform.position, dirToPlayer * 2f);
            }
        }
    }

    void OnStateChanged()
    {
        switch (currentState)
        {
            case MushroomState.Fleeing:
                StartFleeing();
                break;

            case MushroomState.Collected:
                OnCollected();
                break;
        }
    }

    void StartFleeing()
    {
        if (player != null)
        {
            fleeDirection = (transform.position - player.position).normalized;
        }
    }

    public void MoveMushroom(Vector3 direction, float speed)
    {
        if (rb != null)
        {
            rb.linearVelocity = new Vector3(direction.x * speed, rb.linearVelocity.y, direction.z * speed);
        }
    }

    public void OnCollected()
    {
        // Notify mail system
        if (MailSystem.Instance != null)
        {
            MailSystem.Instance.UpdateMushroomProgress(mushroomData.mushroomType, 1);

            // Refresh UI
            var ui = FindObjectOfType<MushroomListUI>();
            if (ui != null) ui.Refresh();
        }

        // Play collection effect
        if (mushroomData.collectionEffect != null)
        {
            Instantiate(mushroomData.collectionEffect, transform.position, Quaternion.identity);
        }

        // Destroy mushroom
        Destroy(gameObject, 0.1f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && currentState != MushroomState.Collected)
        {
            ChangeState(MushroomState.Collected);
        }
    }

    // Getters for personality scripts
    public float StateTimer => stateTimer;
    public bool PlayerInRange => playerInRange;
    public Transform Player => player;
    public Vector3 OriginalPosition => originalPosition;
    public Vector3 FleeDirection => fleeDirection;
    public MushroomData Data => mushroomData;
}

