using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PaddleController : MonoBehaviour
{
    [Header("Paddle Movement Settings")]
    public Transform vrHand;                 // VR controller or hand transform
    public float followSpeed = 20f;          // Speed at which the paddle follows the VR hand
    public float maxPaddleSpeed = 8f;        // Maximum allowed paddle velocity

    [Header("Capsule Sweep Settings")]
    public float paddleRadius = 0.1f;        // Radius of the paddle for capsule sweep
    public float nudgeForce = 2f;            // Minimal force applied to correct tunneling
    public float maxSweepDistance = 1f;      // Maximum distance for the capsule sweep

    [Header("Debug Options")]
    public bool showDebugCapsule = false;
    public bool debugLogs = false;

    private Rigidbody rb;
    private Vector3 previousPosition;
    private Vector3 paddleVelocity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        previousPosition = transform.position;
    }

    private void FixedUpdate()
    {
        FollowHand();
        PerformCapsuleSweep();
    }

    private void FollowHand()
    {
        if (vrHand == null) return;

        // Smoothly move and rotate the paddle to follow the VR hand
        Vector3 targetPosition = Vector3.Lerp(transform.position, vrHand.position, followSpeed * Time.fixedDeltaTime);
        Quaternion targetRotation = Quaternion.Lerp(transform.rotation, vrHand.rotation, followSpeed * Time.fixedDeltaTime);

        rb.MovePosition(targetPosition);
        rb.MoveRotation(targetRotation);

        // Calculate paddle velocity
        paddleVelocity = (transform.position - previousPosition) / Time.fixedDeltaTime;

        // Clamp paddle velocity
        if (paddleVelocity.magnitude > maxPaddleSpeed)
        {
            paddleVelocity = paddleVelocity.normalized * maxPaddleSpeed;
        }

        previousPosition = transform.position; // Update for the next frame
    }

    private void PerformCapsuleSweep()
    {
        Vector3 start = previousPosition;                           // Starting point of the capsule
        Vector3 end = transform.position;                          // End point of the capsule
        float sweepDistance = Vector3.Distance(start, end);

        if (sweepDistance > maxSweepDistance) return;              // Skip if the distance exceeds the sweep range

        if (Physics.CapsuleCast(start, end, paddleRadius, paddleVelocity.normalized, out RaycastHit hit, sweepDistance))
        {
            if (hit.collider.CompareTag("Ball"))
            {
                Rigidbody ballRb = hit.collider.attachedRigidbody;
                if (ballRb != null)
                {
                    // Apply a small nudge force to the ball to correct tunneling
                    Vector3 reflection = Vector3.Reflect(ballRb.velocity.normalized, hit.normal).normalized;
                    ballRb.AddForce(reflection * nudgeForce, ForceMode.Impulse);

                    if (debugLogs)
                    {
                        Debug.Log($"Capsule sweep detected collision with ball at {hit.point}, applying nudge force.");
                    }
                }
            }
        }

        // Visualize the capsule sweep in the Scene view for debugging
        if (showDebugCapsule)
        {
            Debug.DrawLine(start, end, Color.green);
            Debug.DrawRay(hit.point, hit.normal * 0.2f, Color.red);
        }
    }
}
