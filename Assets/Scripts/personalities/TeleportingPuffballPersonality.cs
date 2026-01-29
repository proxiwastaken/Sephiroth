using UnityEngine;

public class TeleportingPuffballPersonality : MushroomPersonality
{
    [Header("Teleport Behavior")]
    public float alertTime = 0.5f;
    public float teleportMinDistance = 8f;
    public float teleportMaxDistance = 15f;
    public int maxTeleports = 3;
    public float cooldownTime = 1.5f;
    public GameObject teleportEffect;

    private int teleportCount = 0;

    public override void UpdateBehavior()
    {
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
                HandleCooldownState();
                break;
        }
    }

    void HandleHiddenState()
    {
        mushroomAI.StopMushroom();

        // Reset after hiding for a bit
        if (mushroomAI.StateTimer > 3f)
        {
            teleportCount = 0;
            ChangeState(MushroomState.Idle);
        }
    }

    void HandleIdleState()
    {
        mushroomAI.StopMushroom();

        // Player detected - get ready to teleport
        if (mushroomAI.PlayerInRange)
        {
            Debug.Log($"Puffball: Player detected! Moving to Alert state");
            ChangeState(MushroomState.Alert);
        }
    }

    void HandleAlertState()
    {
        mushroomAI.StopMushroom();

        // Wait a moment before teleporting
        if (mushroomAI.StateTimer > alertTime)
        {
            if (teleportCount < maxTeleports)
            {
                Debug.Log($"Puffball: Teleporting! (Count: {teleportCount + 1}/{maxTeleports})");
                TeleportAway();
                teleportCount++;
                ChangeState(MushroomState.Fleeing); // Using as cooldown state
            }
            else
            {
                // Out of teleports, hide
                Debug.Log("Puffball: Out of teleports, hiding!");
                ChangeState(MushroomState.Hidden);
            }
        }

        // If player leaves range during alert, go back to idle
        if (!mushroomAI.PlayerInRange)
        {
            ChangeState(MushroomState.Idle);
        }
    }

    void HandleCooldownState()
    {
        mushroomAI.StopMushroom();

        // After cooldown, check if player is still nearby
        if (mushroomAI.StateTimer > cooldownTime)
        {
            if (mushroomAI.PlayerInRange && teleportCount < maxTeleports)
            {
                // Player still close, teleport again
                ChangeState(MushroomState.Alert);
            }
            else if (teleportCount >= maxTeleports)
            {
                // No more teleports available
                ChangeState(MushroomState.Hidden);
            }
            else
            {
                // Player left, go back to idle
                ChangeState(MushroomState.Idle);
            }
        }
    }

    void TeleportAway()
    {
        if (mushroomAI.Player == null) return;

        Transform mushroomRoot = mushroomAI.transform;

        // Spawn effect at current position
        if (teleportEffect != null)
        {
            Instantiate(teleportEffect, mushroomRoot.position, Quaternion.identity);
        }

        // Calculate teleport direction - away from player
        Vector3 directionFromPlayer = (mushroomRoot.position - mushroomAI.Player.position).normalized;

        // Random angle deviation
        float randomAngle = Random.Range(-45f, 45f);
        Vector3 rotatedDirection = Quaternion.Euler(0, randomAngle, 0) * directionFromPlayer;

        // Calculate distance
        float distance = Random.Range(teleportMinDistance, teleportMaxDistance);
        Vector3 newPosition = mushroomRoot.position + (rotatedDirection * distance);

        // Raycast downward to find ground
        RaycastHit hit;
        Vector3 rayStart = newPosition + Vector3.up * 10f;

        if (Physics.Raycast(rayStart, Vector3.down, out hit, 50f, ~LayerMask.GetMask("Rope")))
        {
            newPosition = hit.point + Vector3.up * 0.5f;
        }
        else
        {
            // If no ground found, keep Y position
            newPosition.y = mushroomRoot.position.y;
        }

        mushroomRoot.position = newPosition;
        mushroomAI.UpdateOriginalPosition(newPosition);

        // Spawn effect at new position
        if (teleportEffect != null)
        {
            Instantiate(teleportEffect, mushroomRoot.position, Quaternion.identity);
        }

        PlayTeleportSound();

        Debug.Log($"Puffball teleported ROOT to {newPosition} (Distance: {Vector3.Distance(newPosition, mushroomAI.Player.position):F1}m from player)");
    }

    public override void OnStateChanged(MushroomState fromState, MushroomState toState)
    {
        Debug.Log($"Teleporting Mushroom {mushroomAI.transform.name}: {fromState} -> {toState} (Teleports: {teleportCount}/{maxTeleports})");

        if (toState == MushroomState.Alert)
        {
            PlayRustleSound();
        }
        else if (toState == MushroomState.Hidden)
        {
            // Will reset teleport count when coming back to idle
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

    void PlayTeleportSound()
    {
        // Play a special teleport sound if available, otherwise use rustle
        PlayRustleSound();
    }

    // Debug visualization
    void OnDrawGizmos()
    {
        if (mushroomAI == null || mushroomAI.Player == null) return;

        Transform mushroomRoot = mushroomAI.transform;

        // Draw teleport range
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(mushroomRoot.position, teleportMinDistance);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(mushroomRoot.position, teleportMaxDistance);

        // Draw line to player
        Gizmos.color = mushroomAI.PlayerInRange ? Color.red : Color.green;
        Gizmos.DrawLine(mushroomRoot.position, mushroomAI.Player.position);
    }
}