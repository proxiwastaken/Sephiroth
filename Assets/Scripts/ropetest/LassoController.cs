using UnityEngine;
using System.Collections.Generic;

public class LassoController : MonoBehaviour
{
    [Header("Rope Configuration")]
    public GameObject ropeSegmentPrefab;
    public int ropeSegments = 15;
    public float segmentLength = 0.5f;
    public float ropeWidth = 0.1f;

    [Header("Physics Settings")]
    public float springForce = 50f;
    public float springDamper = 5f;
    public float throwForce = 5f; 
    public float reelSpeed = 3f; 
    public LayerMask catchableLayer = 1;
    private bool isReeling = false;
    private float maxRopeLength = 10f; 


    [Header("Input")]
    public KeyCode throwKey = KeyCode.Q;
    public KeyCode retractKey = KeyCode.R;

    [Header("Visual")]
    public Material ropeMaterial;
    public float lineWidth = 0.05f;

    [Header("Anchor Point")]
    public Transform ropeAnchor; // Where rope attaches to player

    private List<GameObject> ropeSegmentObjects = new List<GameObject>();
    private List<RopeSegment> ropeSegmentScripts = new List<RopeSegment>();
    private LineRenderer lineRenderer;
    private Transform playerTransform;
    private Camera playerCamera;

    // Invisible anchor for rope attachment
    private GameObject anchorObject;
    private Rigidbody anchorRigidbody;

    private bool isThrown = false;
    private bool isRetracting = false;
    private GameObject caughtObject;

    void Start()
    {
        playerTransform = transform;
        playerCamera = Camera.main;

        SetupRopeAnchor();
        SetupVisualRope();
        CreateRope();
    }

    void SetupRopeAnchor()
    {
        // Create invisible anchor point that follows the player
        anchorObject = new GameObject("RopeAnchor");
        anchorObject.transform.SetParent(transform);

        // Position anchor at player's hand/chest level
        if (ropeAnchor != null)
            anchorObject.transform.position = ropeAnchor.position;
        else
            anchorObject.transform.localPosition = new Vector3(0.5f, 1.5f, 0f);

        anchorRigidbody = anchorObject.AddComponent<Rigidbody>();
        anchorRigidbody.isKinematic = true;
        anchorRigidbody.useGravity = false;
    }

    void Update()
    {
        HandleInput();
        UpdateAnchorPosition();
        UpdateVisualRope();
    }

    void UpdateAnchorPosition()
    {
        // Keep anchor following player
        if (ropeAnchor != null)
            anchorObject.transform.position = ropeAnchor.position;
        else
            anchorObject.transform.position = transform.position + Vector3.up * 1.5f + transform.right * 0.5f;
    }

    void SetupVisualRope()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.material = ropeMaterial;
        lineRenderer.startWidth = ropeWidth;
        lineRenderer.endWidth = ropeWidth;
        lineRenderer.positionCount = ropeSegments + 1;
        lineRenderer.useWorldSpace = true;
    }

    void CreateRope()
    {
        // Clear any existing rope
        DestroyRope();

        Vector3 startPos = anchorObject.transform.position;

        // Check ground level to avoid spawning rope underground
        float groundLevel = GetGroundLevel(startPos);

        for (int i = 0; i < ropeSegments; i++)
        {
            Vector3 segmentPos = startPos + Vector3.down * (i * segmentLength);

            // Prevent segments from spawning below ground
            if (segmentPos.y < groundLevel + 0.1f)
            {
                segmentPos.y = groundLevel + 0.1f + (i * 0.05f);
            }

            // Create segment
            GameObject segment = Instantiate(ropeSegmentPrefab, segmentPos, Quaternion.identity);

            // Prevent rope from colliding with player
            ConfigureRopePhysics(segment);

            // Setup segment
            RopeSegment ropeScript = segment.GetComponent<RopeSegment>();
            if (ropeScript == null)
                ropeScript = segment.AddComponent<RopeSegment>();

            ropeSegmentObjects.Add(segment);
            ropeSegmentScripts.Add(ropeScript);

            // Connect segments with Spring Joints
            if (i == 0)
            {
                // First segment connects to anchor (not player directly)
                ropeScript.ConnectToSegment(anchorRigidbody, springForce, springDamper);
            }
            else
            {
                // Connect to previous segment
                Rigidbody previousRb = ropeSegmentObjects[i - 1].GetComponent<Rigidbody>();
                ropeScript.ConnectToSegment(previousRb, springForce, springDamper);
            }

            ropeScript.SetJointDistance(segmentLength);
        }
    }

    float GetGroundLevel(Vector3 position)
    {
        RaycastHit hit;

        // Cast ray downward from anchor position
        if (Physics.Raycast(position, Vector3.down, out hit, 50f, ~LayerMask.GetMask("Rope")))
        {
            return hit.point.y;
        }

        // If no ground found, assume reasonable default
        return position.y - 10f;
    }

    void ConfigureRopePhysics(GameObject segment)
    {
        // Set rope segments to a different physics layer
        int ropeLayer = LayerMask.NameToLayer("Rope");
        if (ropeLayer == -1)
        {
            Debug.LogWarning("Rope layer not found! Using default layer.");
            ropeLayer = 0;
        }
        segment.layer = ropeLayer;

        // Get collider and make sure it doesn't collide with player
        Collider segmentCollider = segment.GetComponent<Collider>();
        if (segmentCollider == null)
        {
            // Add a small capsule collider if none exists
            CapsuleCollider cap = segment.AddComponent<CapsuleCollider>();
            cap.radius = ropeWidth * 0.1f;
            cap.height = segmentLength;
            cap.isTrigger = false;
        }

        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer != -1)
        {
            Physics.IgnoreLayerCollision(ropeLayer, playerLayer, true);
        }

        Physics.IgnoreLayerCollision(ropeLayer, ropeLayer, true);
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(throwKey) && !isThrown)
        {
            ThrowLasso();
        }

        if (Input.GetKeyDown(retractKey))
        {
            RetractLasso();
        }
    }

    void ThrowLasso()
    {
        if (isThrown) return;

        isThrown = true;
        Vector3 throwDirection = playerCamera.transform.forward;

        for (int i = 0; i < ropeSegmentObjects.Count; i++)
        {
            Rigidbody segmentRb = ropeSegmentObjects[i].GetComponent<Rigidbody>();

            float forceMultiplier = Mathf.Lerp(0.2f, 1f, (float)i / ropeSegmentObjects.Count);

            segmentRb.AddForce(throwDirection * throwForce * forceMultiplier, ForceMode.Impulse);

            // Add some upward component to make it arc nicely
            segmentRb.AddForce(Vector3.up * throwForce * 0.3f * forceMultiplier, ForceMode.Impulse);
        }

        // Catch mushroom
        StartCoroutine(CheckForCatch());
    }

    System.Collections.IEnumerator CheckForCatch()
    {
        GameObject lassoTip = ropeSegmentObjects[ropeSegmentObjects.Count - 1];

        while (isThrown && !isRetracting)
        {
            // Check if lasso tip is near any catchable objects
            Collider[] nearbyObjects = Physics.OverlapSphere(lassoTip.transform.position, 1f, catchableLayer);

            foreach (var obj in nearbyObjects)
            {
                MushroomAI mushroom = obj.GetComponent<MushroomAI>();
                if (mushroom != null && caughtObject == null)
                {
                    CatchObject(obj.gameObject);
                    yield break;
                }
            }

            yield return new WaitForFixedUpdate();
        }
    }

    void CatchObject(GameObject target)
    {
        caughtObject = target;

        // Attach the caught object to the lasso tip
        GameObject lassoTip = ropeSegmentObjects[ropeSegmentObjects.Count - 1];
        SpringJoint catchJoint = lassoTip.AddComponent<SpringJoint>();

        Rigidbody targetRb = target.GetComponent<Rigidbody>();
        if (targetRb == null)
            targetRb = target.AddComponent<Rigidbody>();

        catchJoint.connectedBody = targetRb;
        catchJoint.spring = springForce * 2f;
        catchJoint.damper = springDamper;
        catchJoint.autoConfigureConnectedAnchor = true;

        Debug.Log($"Caught {target.name}!");

        // Change mushroom state to caught
        MushroomAI mushroomAI = target.GetComponent<MushroomAI>();
        if (mushroomAI != null)
        {
            mushroomAI.ChangeState(MushroomState.Collected);
        }
    }

    void RetractLasso()
    {
        if (!isThrown) return;

        isRetracting = true;
        isReeling = true;

        // Start continuous reeling
        StartCoroutine(ReelRopeBack());
    }

    System.Collections.IEnumerator ReelRopeBack()
    {
        while (isReeling && ropeSegmentObjects.Count > 0)
        {
            for (int i = 0; i < ropeSegmentObjects.Count; i++)
            {
                if (ropeSegmentObjects[i] == null) break;

                Rigidbody segmentRb = ropeSegmentObjects[i].GetComponent<Rigidbody>();
                if (segmentRb == null) break;

                // Calculate direction to anchor
                Vector3 directionToAnchor = (anchorObject.transform.position - segmentRb.position).normalized;

                // Apply continuous gentle force toward anchor
                float reelForce = reelSpeed * (i + 1); // Segments further from anchor get pulled harder
                segmentRb.AddForce(directionToAnchor * reelForce, ForceMode.Force);

                // Add slight damping
                segmentRb.linearVelocity *= 0.95f;
            }

            // Check if rope is close enough to player to stop reeling
            if (ropeSegmentObjects.Count > 0)
            {
                float distanceToAnchor = Vector3.Distance(
                    ropeSegmentObjects[ropeSegmentObjects.Count - 1].transform.position,
                    anchorObject.transform.position);

                if (distanceToAnchor < 2f) // When tip is close enough
                {
                    isReeling = false;
                }
            }

            yield return new WaitForFixedUpdate();
        }

        Invoke(nameof(ResetLasso), 0.5f);
    }

    void FixedUpdate()
    {
        if (isThrown && !isRetracting)
        {
            LimitRopeExtension();
        }
    }

    void ResetLasso()
    {
        isThrown = false;
        isRetracting = false;
        isReeling = false;

        if (caughtObject != null)
        {
            SpringJoint[] joints = caughtObject.GetComponents<SpringJoint>();
            foreach (var joint in joints)
                Destroy(joint);

            caughtObject = null;
        }

        StopAllCoroutines();
        // Reset rope position
        CreateRope();
    }

    void LimitRopeExtension()
    {
        // Prevent rope from extending too far
        if (ropeSegmentObjects.Count > 0)
        {
            GameObject tip = ropeSegmentObjects[ropeSegmentObjects.Count - 1];
            float currentDistance = Vector3.Distance(tip.transform.position, anchorObject.transform.position);

            if (currentDistance > maxRopeLength)
            {
                // Pull the tip back toward the anchor
                Vector3 directionToAnchor = (anchorObject.transform.position - tip.transform.position).normalized;
                Vector3 targetPosition = anchorObject.transform.position + directionToAnchor * maxRopeLength;
                tip.transform.position = Vector3.Lerp(tip.transform.position, targetPosition, Time.fixedDeltaTime * 2f);
            }
        }
    }



    void UpdateVisualRope()
    {
        Vector3[] positions = new Vector3[ropeSegments + 1];
        positions[0] = anchorObject.transform.position;

        for (int i = 0; i < ropeSegmentObjects.Count; i++)
        {
            positions[i + 1] = ropeSegmentObjects[i].transform.position;
        }

        lineRenderer.SetPositions(positions);
    }

    void DestroyRope()
    {
        foreach (var segment in ropeSegmentObjects)
        {
            if (segment != null)
                Destroy(segment);
        }

        ropeSegmentObjects.Clear();
        ropeSegmentScripts.Clear();
    }

    void OnDestroy()
    {
        DestroyRope();
        if (anchorObject != null)
            Destroy(anchorObject);
    }

    // Debug visualization
    void OnDrawGizmos()
    {
        if (ropeSegmentObjects != null && ropeSegmentObjects.Count > 0)
        {
            Gizmos.color = Color.red;
            GameObject tip = ropeSegmentObjects[ropeSegmentObjects.Count - 1];
            if (tip != null)
                Gizmos.DrawWireSphere(tip.transform.position, 1f);
        }
    }
}

