using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PaddleController : MonoBehaviour
{
    [Header("VR Hand/Movement")]
    [Tooltip("The VR controller or hand transform that the paddle should follow.")]
    public Transform vrHand;                 

    [Tooltip("How dwquickly the paddle follows the hand's position/rotation.")]
    public float followSpeed = 20f;

    [Tooltip("Maximum speed the paddle can move. Reduces overpowered hits.")]
    public float maxPaddleSpeed = 8f;

    [Header("Capsule Sweep Fallback")]
    [Tooltip("Radius used in capsule sweep to detect near misses.")]
    public float paddleRadius = 0.1f;
    
    [Tooltip("Small impulse used if the sweep detects a missed collision.")]
    public float nudgeForce = 2f;

    [Tooltip("Maximum distance for the capsule sweep (based on how far the paddle moves per frame).")]
    public float maxSweepDistance = 0.5f;
    
    [Tooltip("Minimum relative speed of paddle before we apply a nudge.")]
    public float minNudgePaddleSpeed = 2f;

    [Header("Debug")]
    public bool showDebugCapsule = false;
    public bool debugLogs = false;

    private Rigidbody rb;
    private Vector3 previousPosition;
    private Vector3 paddleVelocity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Paddle is Kinematic since we directly control it via MovePosition.
        rb.isKinematic = true;

        // Continuous Speculative helps for kinematic bodies.
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        previousPosition = transform.position;
    }

    private void FixedUpdate()
    {
        FollowVRHand();
        PerformCapsuleSweep();
    }

    /// <summary>
    /// Smoothly move/rotate the paddle to the VR hand while clamping velocity.
    /// </summary>
    private void FollowVRHand()
    {
        if (vrHand == null)
            return;

        // Lerp to the hand's position/rotation for a smoother feel.
        Vector3 targetPos = Vector3.Lerp(transform.position, vrHand.position, followSpeed * Time.fixedDeltaTime);
        Quaternion targetRot = Quaternion.Lerp(transform.rotation, vrHand.rotation, followSpeed * Time.fixedDeltaTime);

        rb.MovePosition(targetPos);
        rb.MoveRotation(targetRot);

        // Calculate paddle velocity (for potential collision logic).
        paddleVelocity = (transform.position - previousPosition) / Time.fixedDeltaTime;
        
        // Limit the paddle velocity to avoid overpowered hits.
        if (paddleVelocity.magnitude > maxPaddleSpeed)
        {
            paddleVelocity = paddleVelocity.normalized * maxPaddleSpeed;
        }

        previousPosition = transform.position;
    }

    /// <summary>
    /// Capsule sweep to catch missed collisions (tunneling). 
    /// If we detect the ball, we apply a small nudge to correct it.
    /// </summary>
    private void PerformCapsuleSweep()
    {
        // If the paddle isn't moving quickly enough, we won't nudge.
        if (paddleVelocity.magnitude < minNudgePaddleSpeed)
            return;

        Vector3 startPos = previousPosition;
        Vector3 endPos = transform.position;
        float sweepDist = Vector3.Distance(startPos, endPos);

        // Only sweep if the paddle moved less than our threshold in this frame.
        // If the user teleported the paddle or something, we ignore it here.
        if (sweepDist > maxSweepDistance)
            return;

        // Raycast in the direction of movement with a capsule.
        if (Physics.CapsuleCast(startPos, endPos, paddleRadius, paddleVelocity.normalized, out RaycastHit hit, sweepDist))
        {
            if (hit.collider.CompareTag("Ball"))
            {
                Rigidbody ballRb = hit.collider.attachedRigidbody;
                if (ballRb != null)
                {
                    // We'll gently nudge the ball out of tunneling.
                    // Reflection of its velocity to mimic a bounce direction.
                    Vector3 reflectionDir = Vector3.Reflect(ballRb.velocity.normalized, hit.normal).normalized;
                    
                    // Add minimal force so we don't "bullet" the ball.
                    ballRb.AddForce(reflectionDir * nudgeForce, ForceMode.Impulse);

                    if (debugLogs)
                    {
                        Debug.Log($"Capsule sweep detected ball at {hit.point}, applying nudge {nudgeForce}");
                    }
                }
            }
        }

        // For debug: visualize the capsule path.
        if (showDebugCapsule)
        {
            Debug.DrawLine(startPos, endPos, Color.green);
        }
    }

    /// <summary>
    /// OnCollisionEnter is mostly left for logging or tiny force corrections,
    /// because we let the PhysicMaterial handle bounces naturally.
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Ball"))
        {
            // By default, do nothing – rely on PhysicMaterials for bounce.
            // Optionally apply a very small impulse if you want extra "pop" 
            // (but keep it minimal to avoid bullet hits).
            //
            // Example:
            // Rigidbody ballRb = collision.collider.attachedRigidbody;
            // if (ballRb != null) { ballRb.AddForce(collision.contacts[0].normal * 0.5f, ForceMode.Impulse); }

            if (debugLogs)
                Debug.Log("Paddle collided with ball - letting PhysicMaterial handle bounce.");
        }
    }
}
