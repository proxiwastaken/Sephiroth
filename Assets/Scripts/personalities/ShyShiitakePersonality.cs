using UnityEngine;

public class ShyShiitakePersonality : MushroomPersonality
{
    [Header("Shy Behavior")]
    public float hideTime = 10000f;
    public float alertTime = 0.5f;
    public float fleeTime = 3f;
    public float fleeDistance = 8f;
    public float fleeDirectionUpdateRate = 0.2f;

    private Vector3 fleeStartPosition;
    private float lastFleeDirectionUpdate = 0f;

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
                HandleFleeingState();
                break;
        }
    }

    void HandleHiddenState()
    {
        mushroomAI.StopMushroom();

        if (!mushroomAI.PlayerInRange && mushroomAI.StateTimer > hideTime)
        {
            ChangeState(MushroomState.Idle);
        }
    }

    void HandleIdleState()
    {
        mushroomAI.StopMushroom();

        // If player comes near, get alert
        if (mushroomAI.PlayerInRange)
        {
            ChangeState(MushroomState.Alert);
        }
    }

    void HandleAlertState()
    {
        mushroomAI.StopMushroom();

        // Wait a moment, then flee
        if (mushroomAI.StateTimer > alertTime)
        {
            ChangeState(MushroomState.Fleeing);
        }

        // If player leaves during alert, go back to idle
        if (!mushroomAI.PlayerInRange)
        {
            ChangeState(MushroomState.Idle);
        }
    }

    void HandleFleeingState()
    {
        // Continuously update flee direction
        UpdateFleeDirection();

        // Move away from player using current flee direction
        if (mushroomAI.Player != null)
        {
            Vector3 currentFleeDirection = GetCurrentFleeDirection();
            mushroomAI.MoveMushroom(currentFleeDirection, data.fleeSpeed);
        }

        // Calculate how far we've fled from starting position
        float distanceFled = Vector3.Distance(transform.position, fleeStartPosition);

        // Stop fleeing based on time OR distance
        if (mushroomAI.StateTimer > fleeTime || distanceFled > fleeDistance)
        {
            mushroomAI.StopMushroom();
            ChangeState(MushroomState.Hidden);
        }
    }

    void UpdateFleeDirection()
    {
        if (Time.time - lastFleeDirectionUpdate > fleeDirectionUpdateRate)
        {
            if (mushroomAI.Player != null)
            {
                Vector3 newFleeDirection = (transform.position - mushroomAI.Player.position).normalized;
                mushroomAI.UpdateFleeDirection(newFleeDirection);
                lastFleeDirectionUpdate = Time.time;
            }
        }
    }

    Vector3 GetCurrentFleeDirection()
    {
        if (mushroomAI.Player != null)
        {
            return (transform.position - mushroomAI.Player.position).normalized;
        }
        return mushroomAI.FleeDirection;
    }

    public override void OnStateChanged(MushroomState fromState, MushroomState toState)
    {
        Debug.Log($"Mushroom {transform.name}: {fromState} -> {toState}");

        if (toState == MushroomState.Alert)
        {
            PlayRustleSound();
        }
        else if (toState == MushroomState.Fleeing)
        {
            fleeStartPosition = transform.position;
            lastFleeDirectionUpdate = 0f;
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
}