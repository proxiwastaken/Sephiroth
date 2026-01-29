using UnityEngine;

public class AggressiveChanterellePersonality : MushroomPersonality
{
    [Header("Aggressive Behavior")]
    public float detectTime = 0.3f;
    public float chaseTime = 5f;
    public float chaseSpeed = 6f;
    public float attackRange = 1.5f;
    public float cooldownTime = 2f;

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
                HandleChaseState();
                break;
        }
    }

    void HandleHiddenState()
    {
        mushroomAI.StopMushroom();

        if (mushroomAI.PlayerInRange)
        {
            ChangeState(MushroomState.Alert);
        }
        else if (mushroomAI.StateTimer > cooldownTime)
        {
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
        mushroomAI.StopMushroom();

        if (mushroomAI.StateTimer > detectTime)
        {
            ChangeState(MushroomState.Fleeing); // Using Fleeing as Chase state
        }

        if (!mushroomAI.PlayerInRange)
        {
            ChangeState(MushroomState.Idle);
        }
    }

    void HandleChaseState()
    {
        if (mushroomAI.Player != null)
        {
            // Calculate direction to player
            Vector3 directionToPlayer = (mushroomAI.Player.position - transform.position).normalized;

            float distanceToPlayer = Vector3.Distance(transform.position, mushroomAI.Player.position);

            // Check if in attack range
            if (distanceToPlayer <= attackRange)
            {
                mushroomAI.StopMushroom();
                // Play attack animation or effect here
            }
            else
            {
                // Chase the player
                mushroomAI.MoveMushroom(directionToPlayer, chaseSpeed);
            }
        }

        // Stop chasing after time limit or if player is out of range
        if (mushroomAI.StateTimer > chaseTime || !mushroomAI.PlayerInRange)
        {
            mushroomAI.StopMushroom();
            ChangeState(MushroomState.Hidden);
        }
    }

    public override void OnStateChanged(MushroomState fromState, MushroomState toState)
    {
        Debug.Log($"Aggressive Mushroom {transform.name}: {fromState} -> {toState}");

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