using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PaddleImpactHandler : MonoBehaviour
{
    [Header("Impact Settings")]
    [Tooltip("Base impulse applied on collision if relative speed is at the minimum threshold.")]
    public float baseImpulse = 4f;

    [Tooltip("Maximum impulse that can be applied even at high relative speeds.")]
    public float maxImpulse = 12f;

    [Tooltip("Relative velocity that, when exceeded, will saturate the impulse (before clamping to maxImpulse).")]
    public float referenceRelativeSpeed = 5f;

    [Tooltip("A curve to scale the impulse based on the relative speed between paddle and ball.")]
    public AnimationCurve impulseCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 1f);
    
    [Header("Optional Fallback Raycast Settings")]
    public bool useRaycastFallback = true;
    public int raysAlongWidth = 3;
    public int raysAlongHeight = 3;
    public float rayDistance = 0.1f;

    public float raycastMinPaddleSpeed = 1f;

    [Header("Debug Settings")]
    public bool showDebugRays = false;

    private Rigidbody paddleRb;
    private Vector3 previousPaddlePos;
    private Vector3 paddleVelocity;

    private void Awake()
    {
        paddleRb = GetComponent<Rigidbody>();
        previousPaddlePos = transform.position;
    }

    private void FixedUpdate()
    {
        // Update paddle velocity.
        paddleVelocity = (transform.position - previousPaddlePos) / Time.fixedDeltaTime;
        previousPaddlePos = transform.position;

        // Optionally run a fallback raycast to nudge the ball if needed.
        if (useRaycastFallback)
        {
            PerformFallbackRaycasts();
        }
    }

    /// <summary>
    /// This collision callback is intended to fine-tune the hit impulse.
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Ball"))
        {
            Rigidbody ballRb = collision.collider.attachedRigidbody;
            if (ballRb == null)
                return;

            // Calculate the relative velocity between the paddle and ball.
            Vector3 relativeVelocity = paddleVelocity - ballRb.velocity;
            float relativeSpeed = relativeVelocity.magnitude;

            // Get a scaling factor from our curve. With a reference speed, speeds lower than this
            // yield a lower multiplier and speeds above yield higher multipliers – capped by maxImpulse.
            float impulseFactor = impulseCurve.Evaluate(Mathf.Clamp01(relativeSpeed / referenceRelativeSpeed));

            // Calculate a tentative impulse based on the base value.
            float calculatedImpulse = Mathf.Lerp(baseImpulse, maxImpulse, impulseFactor);

            // Determine a direction.
            // Use the collision contact normal (which is roughly the hit direction) blended with the paddle’s velocity direction.
            ContactPoint contact = collision.contacts[0];
            Vector3 contactNormal = contact.normal;
            Vector3 paddleDir = paddleVelocity.normalized;
            // Blend between the contact normal and the paddle’s movement direction.
            Vector3 finalImpulseDir = Vector3.Lerp(contactNormal, paddleDir, 0.3f).normalized;

            // Optionally, you might want to dampen the impulse further if the paddle is moving extremely fast.
            // For example: finalImpulseDir = Vector3.Lerp(finalImpulseDir, contactNormal, 0.5f).normalized;

            // Apply the impulse to the ball.
            ballRb.AddForce(finalImpulseDir * calculatedImpulse, ForceMode.Impulse);
        }
    }

    /// <summary>
    /// Optional fallback: Uses a grid of raycasts to nudge the ball if it passes very near the paddle.
    /// This compensates for extremely fast hand movement that might otherwise “skip” collisions.
    /// </summary>
    private void PerformFallbackRaycasts()
    {
        // The grid will scan the face of the paddle. (Assumes that the paddle’s local X-axis is the impact axis.)
        for (int w = 0; w < raysAlongWidth; w++)
        {
            for (int h = 0; h < raysAlongHeight; h++)
            {
                float yPos = Mathf.Lerp(-0.5f, 0.5f, raysAlongWidth <= 1 ? 0.5f : (float)w / (raysAlongWidth - 1));
                float zPos = Mathf.Lerp(-0.5f, 0.5f, raysAlongHeight <= 1 ? 0.5f : (float)h / (raysAlongHeight - 1));

                // Origin is on the paddle surface. You might adjust the local X to be at the “front” of the paddle.
                Vector3 localOrigin = new Vector3(0f, yPos, zPos);
                Vector3 origin = transform.TransformPoint(localOrigin);
                
                // Cast forward and backward along the paddle’s local right.
                RaycastInDirection(origin, transform.right);
                RaycastInDirection(origin, -transform.right);
            }
        }
    }

    private void RaycastInDirection(Vector3 origin, Vector3 direction)
    {
        if (Physics.Raycast(origin, direction, out RaycastHit hit, rayDistance))
        {
            if (hit.collider.CompareTag("Ball"))
            {
                // Only perform fallback if the paddle is moving at least minimally.
                if (paddleVelocity.magnitude >= raycastMinPaddleSpeed)
                {
                    Rigidbody ballRb = hit.collider.attachedRigidbody;
                    if (ballRb == null)
                        return;

                    // Get a relative velocity based impulse, similar to the collision method.
                    Vector3 relativeVelocity = paddleVelocity - ballRb.velocity;
                    float relativeSpeed = relativeVelocity.magnitude;
                    float impulseFactor = impulseCurve.Evaluate(Mathf.Clamp01(relativeSpeed / referenceRelativeSpeed));
                    float calculatedImpulse = Mathf.Lerp(baseImpulse, maxImpulse, impulseFactor);

                    // Direction is the reflection of the ball’s velocity.
                    Vector3 reflectionDir = Vector3.Reflect(ballRb.velocity.normalized, hit.normal).normalized;
                    Vector3 finalDir = Vector3.Lerp(reflectionDir, paddleVelocity.normalized, 0.3f).normalized;

                    ballRb.AddForce(finalDir * calculatedImpulse, ForceMode.Impulse);
                }
            }
        }

        if (showDebugRays)
        {
            Debug.DrawRay(origin, direction * rayDistance, Color.cyan);
        }
    }
}
