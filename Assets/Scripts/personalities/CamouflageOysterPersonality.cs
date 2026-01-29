using UnityEngine;

public class CamouflageOysterPersonality : MushroomPersonality
{
    [Header("Camouflage Behavior")]
    public float detectionAngle = 60f;
    public float sneakSpeed = 1.5f;
    public float fleeSpeed = 9f;
    public float maxSneakDistance = 20f;
    public float checkInterval = 0.1f; 

    private Vector3 fleeStartPosition;
    private bool isBeingWatched = false;
    private float lastCheckTime = 0f;

    public override void UpdateBehavior()
    {
        // Only check visibility periodically for performance
        if (Time.time - lastCheckTime > checkInterval)
        {
            CheckIfBeingWatched();
            lastCheckTime = Time.time;
        }

        switch (mushroomAI.currentState)
        {
            case MushroomState.Hidden:
                HandleHiddenState();
                break;

            case MushroomState.Idle:
                HandleIdleState();
                break;

            case MushroomState.Alert:
                HandleAlertState();
                break;

            case MushroomState.Fleeing:
                HandleFleeingState();
                break;
        }
    }

    void CheckIfBeingWatched()
    {
        if (mushroomAI.Player == null)
        {
            isBeingWatched = false;
            return;
        }

        // Get player's camera
        Camera playerCamera = Camera.main;
        if (playerCamera == null)
        {
            // Fallback to using player transform forward
            Vector3 directionToMushroom = (transform.position - mushroomAI.Player.position).normalized;
            float angle = Vector3.Angle(mushroomAI.Player.forward, directionToMushroom);
            isBeingWatched = angle < detectionAngle && mushroomAI.PlayerInRange;
            return;
        }

        // Calculate direction from camera to mushroom
        Vector3 cameraToMushroom = transform.position - playerCamera.transform.position;
        float distanceToCamera = cameraToMushroom.magnitude;

        // Check if mushroom is in front of camera
        float dotProduct = Vector3.Dot(playerCamera.transform.forward, cameraToMushroom.normalized);

        // Convert to angle
        float viewAngle = Mathf.Acos(Mathf.Clamp(dotProduct, -1f, 1f)) * Mathf.Rad2Deg;

        // Additional raycast check to see if mushroom is actually visible (not blocked by walls)
        bool hasLineOfSight = false;
        bool isInViewCone = viewAngle < detectionAngle;

        if (isInViewCone)
        {
            RaycastHit hit;
            Vector3 rayOrigin = playerCamera.transform.position;
            Vector3 rayDirection = cameraToMushroom.normalized;

            if (Physics.Raycast(rayOrigin, rayDirection, out hit, distanceToCamera + 1f))
            {
                // Check if raycast hit this mushroom or any of its children
                MushroomAI hitMushroom = hit.transform.GetComponentInParent<MushroomAI>();
                hasLineOfSight = hitMushroom == mushroomAI;

                if (Time.frameCount % 60 == 0)
                {
                    Debug.Log($"Raycast hit: {hit.transform.name}, Is this mushroom: {hasLineOfSight}");
                }
            }
        }

        // Must be in view cone, in range, AND have line of sight to be considered "watched"
        isBeingWatched = isInViewCone && mushroomAI.PlayerInRange && hasLineOfSight;

        // Debug
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"Oyster {name}: Angle={viewAngle:F1}, InViewCone={isInViewCone}, InRange={mushroomAI.PlayerInRange}, LineOfSight={hasLineOfSight}, BeingWatched={isBeingWatched}");
        }
    }

    void HandleHiddenState()
    {
        mushroomAI.StopMushroom();

        if (mushroomAI.StateTimer > 2f)
        {
            ChangeState(MushroomState.Idle);
        }
    }

    void HandleIdleState()
    {
        mushroomAI.StopMushroom();

        // Start sneaking as soon as player is in range
        if (mushroomAI.PlayerInRange)
        {
            ChangeState(MushroomState.Alert);
        }
    }

    void HandleAlertState()
    {
        if (mushroomAI.Player == null)
        {
            ChangeState(MushroomState.Hidden);
            return;
        }

        if (isBeingWatched)
        {
            // Freeze completely when being watched
            mushroomAI.StopMushroom();

            if (Time.frameCount % 30 == 0)
            {
                Debug.Log($"Oyster FROZEN - Being watched!");
            }

            // If player gets too close while watching, panic and flee
            float distanceToPlayer = Vector3.Distance(transform.position, mushroomAI.Player.position);
            if (distanceToPlayer < 3f)
            {
                fleeStartPosition = transform.position;
                ChangeState(MushroomState.Fleeing);
            }
        }
        else
        {
            // Player is in range but not looking - sneak away!
            if (mushroomAI.Player != null)
            {
                Vector3 sneakDirection = (transform.position - mushroomAI.Player.position).normalized;
                mushroomAI.MoveMushroom(sneakDirection, sneakSpeed);

                if (Time.frameCount % 30 == 0)
                {
                    Debug.Log($"Oyster SNEAKING - Not being watched!");
                }
            }
        }

        // Check if escaped far enough
        float distanceMoved = Vector3.Distance(transform.position, mushroomAI.OriginalPosition);
        if (distanceMoved > maxSneakDistance)
        {
            ChangeState(MushroomState.Hidden);
        }

        // Return to hidden if player leaves range
        if (!mushroomAI.PlayerInRange)
        {
            ChangeState(MushroomState.Hidden);
        }
    }

    void HandleFleeingState()
    {
        if (mushroomAI.Player != null)
        {
            // Flee at max speed regardless of being watched
            Vector3 fleeDirection = (transform.position - mushroomAI.Player.position).normalized;
            mushroomAI.MoveMushroom(fleeDirection, fleeSpeed);
        }

        float distanceFled = Vector3.Distance(transform.position, fleeStartPosition);
        if (distanceFled > 10f || !mushroomAI.PlayerInRange)
        {
            mushroomAI.StopMushroom();
            ChangeState(MushroomState.Hidden);
        }
    }

    public override void OnStateChanged(MushroomState fromState, MushroomState toState)
    {
        Debug.Log($"Camouflage Mushroom {transform.name}: {fromState} -> {toState}");

        if (toState == MushroomState.Alert)
        {
            PlayRustleSound();
        }
        else if (toState == MushroomState.Fleeing)
        {
            PlayRustleSound();
        }
    }

    void PlayRustleSound()
    {
        if (data.rustleSounds != null && data.rustleSounds.Length > 0)
        {
            var audioSource = GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.PlayOneShot(data.rustleSounds[Random.Range(0, data.rustleSounds.Length)]);
            }
        }
    }

    void OnDrawGizmos()
    {
        if (mushroomAI == null || mushroomAI.Player == null) return;

        Camera playerCamera = Camera.main;
        if (playerCamera == null) return;

        Gizmos.color = isBeingWatched ? Color.red : Color.green;
        Gizmos.DrawLine(playerCamera.transform.position, transform.position);

        // Draw a sphere at mushroom position to show watched status
        Gizmos.color = isBeingWatched ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        // Draw detection cone from camera
        Gizmos.color = Color.yellow;
        Vector3 forward = playerCamera.transform.forward * mushroomAI.Data.detectionRange;
        Vector3 right = Quaternion.Euler(0, detectionAngle, 0) * forward;
        Vector3 left = Quaternion.Euler(0, -detectionAngle, 0) * forward;

        Gizmos.DrawRay(playerCamera.transform.position, forward);
        Gizmos.DrawRay(playerCamera.transform.position, right);
        Gizmos.DrawRay(playerCamera.transform.position, left);
    }
}