using UnityEngine;

public class RopeSegment : MonoBehaviour
{
    [Header("Rope Settings")]
    public float segmentMass = 0.1f;
    public float drag = 1f;
    public float angularDrag = 1f;

    private Rigidbody rb;
    private SpringJoint joint;

    void Start()
    {
        SetupPhysics();
    }

    void SetupPhysics()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();

        rb.mass = segmentMass;
        rb.linearDamping = drag;
        rb.angularDamping = angularDrag;
        rb.useGravity = true;
    }

    public void ConnectToSegment(Rigidbody targetRb, float springForce, float damper)
    {
        joint = gameObject.AddComponent<SpringJoint>();
        joint.connectedBody = targetRb;
        joint.spring = springForce;
        joint.damper = damper;
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedAnchor = Vector3.zero;
        joint.anchor = Vector3.zero;
    }

    public void SetJointDistance(float distance)
    {
        if (joint != null)
        {
            joint.minDistance = distance * 0.8f;
            joint.maxDistance = distance * 1.2f;
        }
    }
}

