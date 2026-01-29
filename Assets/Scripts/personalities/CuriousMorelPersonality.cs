using UnityEngine;

public class CuriousMorelPersonality : MushroomPersonality
{
    [Header("Curious Behavior")]
    public float observeTime = 2f;
    public float approachSpeed = 2f;
    public float minApproachDistance = 3f;
    public float fleeTime = 2f;
    public float fleeSpeed = 8f;

    private bool hasObserved = false;

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

        if (mushroomAI.StateTimer > 1f)
        {
            hasObserved = false;
            ChangeState(MushroomState.Idle);
        }
    }

    void HandleIdleState()
    {
        mushroomAI.StopMushroom();

        if (mushroomAI.PlayerInRange)
        {
            ChangeState(MushroomState.Alert);
        }
    }

    void HandleAlertState()
    {
        if (mushroomAI.Player != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, mushroomAI.Player.position);

            // Approach player cautiously
            if (distanceToPlayer > minApproachDistance && !hasObserved)
            {
                Vector3 directionToPlayer = (mushroomAI.Player.position - transform.position).normalized;
                mushroomAI.MoveMushroom(directionToPlayer, approachSpeed);
            }
            else
            {
                mushroomAI.StopMushroom();
            }

            // After observing for a while, flee
            if (mushroomAI.StateTimer > observeTime)
            {
                hasObserved = true;
                ChangeState(MushroomState.Fleeing);
            }
        }

        if (!mushroomAI.PlayerInRange)
        {
            ChangeState(MushroomState.Hidden);
        }
    }

    void HandleFleeingState()
    {
        if (mushroomAI.Player != null)
        {
            Vector3 fleeDirection = (transform.position - mushroomAI.Player.position).normalized;
            mushroomAI.MoveMushroom(fleeDirection, fleeSpeed);
        }

        if (mushroomAI.StateTimer > fleeTime)
        {
            mushroomAI.StopMushroom();
            ChangeState(MushroomState.Hidden);
        }
    }

    public override void OnStateChanged(MushroomState fromState, MushroomState toState)
    {
        Debug.Log($"Curious Mushroom {transform.name}: {fromState} -> {toState}");

        if (toState == MushroomState.Alert)
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
}