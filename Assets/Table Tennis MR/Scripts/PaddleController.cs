using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PaddleController : MonoBehaviour
{
    [Header("VR / Hand Follow")]
    public Transform vrHand;                 // The transform of the VR controller or hand
    public float followSpeed = 20f;          // How quickly the paddle follows
    public float maxPaddleSpeed = 8f;        // Limits extremely fast paddle movement

    [Header("Hybrid Fallback Settings")]
    public bool useFallbackRaycast = true;
    public int rayCountWidth = 3;
    public int rayCountHeight = 3;
    public float rayDistance = 0.1f;
    public float nudgeImpulse = 2f;          // A small impulse to correct tunneling
    public float minPaddleSpeedForNudge = 1f;
    [Range(0f, 1f)] public float directionBlend = 0.3f;

    [Header("Debug")]
    public bool showDebugRays = false;
    public bool debugLogs = false;

    private Rigidbody rb;
    private Vector3 previousPos;
    private Vector3 paddleVelocity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;  // We'll control it manually
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        rb.interpolation = RigidbodyInterpolation.Interpolate; // Smooth movement

        previousPos = transform.position;
    }

    private void FixedUpdate()
    {
        FollowHand();
        if (useFallbackRaycast)
        {
            RaycastFallback();
        }
    }

    /// <summary>
    /// Moves paddle to vrHand with velocity clamping to avoid super-fast hits.
    /// </summary>
    private void FollowHand()
    {
        if (vrHand == null) return;

        // Smoothly move and rotate the paddle
        Vector3 targetPos = Vector3.Lerp(transform.position, vrHand.position, followSpeed * Time.fixedDeltaTime);
        Quaternion targetRot = Quaternion.Lerp(transform.rotation, vrHand.rotation, followSpeed * Time.fixedDeltaTime);

        rb.MovePosition(targetPos);
        rb.MoveRotation(targetRot);

        // Calculate instantaneous velocity
        paddleVelocity = (transform.position - previousPos) / Time.fixedDeltaTime;
        previousPos = transform.position;

        // Clamp the paddle velocity
        if (paddleVelocity.magnitude > maxPaddleSpeed)
        {
            paddleVelocity = paddleVelocity.normalized * maxPaddleSpeed;
        }
    }

    /// <summary>
    /// We rely on Unity’s built-in bounce. 
    /// If needed, we can do small tweaks on collision. 
    /// Currently, we do minimal manual force to avoid bullet hits.
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Ball"))
        {
            // Optionally add a small effect if you want a bit of "push" 
            // beyond the PhysicMaterial bounce, but keep it minimal:

            // Example minimal push:
            // Rigidbody ballRb = collision.collider.attachedRigidbody;
            // if (ballRb != null)
            // {
            //     // Very small impulse to add a bit of "oomph"
            //     Vector3 collisionNormal = collision.contacts[0].normal;
            //     ballRb.AddForce(collisionNormal * 0.5f, ForceMode.Impulse);
            // }

            if (debugLogs) Debug.Log("Paddle collided with ball, letting PhysicMaterial handle bounce.");
        }
    }

    /// <summary>
    /// Optional fallback to correct missed collisions/tunneling.
    /// Only applies small impulses if near the paddle.
    /// </summary>
    private void RaycastFallback()
    {
        // Only nudge if paddle is moving at least a certain speed
        if (paddleVelocity.magnitude < minPaddleSpeedForNudge) return;

        // We'll create a grid on the paddle's local YZ plane (assuming X is "thickness" axis).
        for (int w = 0; w < rayCountWidth; w++)
        {
            for (int h = 0; h < rayCountHeight; h++)
            {
                float y = (rayCountWidth <= 1) 
                    ? 0.0f 
                    : Mathf.Lerp(-0.5f, 0.5f, (float)w / (rayCountWidth - 1));

                float z = (rayCountHeight <= 1)
                    ? 0.0f
                    : Mathf.Lerp(-0.5f, 0.5f, (float)h / (rayCountHeight - 1));

                // We place the origin in local coords at (x=0) - center thickness, y, z
                Vector3 localOrigin = new Vector3(0f, y, z);
                Vector3 worldOrigin = transform.TransformPoint(localOrigin);

                // We'll check forward (transform.right) and backward (-transform.right).
                CastInDirection(worldOrigin, transform.right);
                CastInDirection(worldOrigin, -transform.right);

                if (showDebugRays)
                {
                    Debug.DrawRay(worldOrigin, transform.right * rayDistance, Color.red);
                    Debug.DrawRay(worldOrigin, -transform.right * rayDistance, Color.blue);
                }
            }
        }
    }

    private void CastInDirection(Vector3 origin, Vector3 dir)
    {
        if (Physics.Raycast(origin, dir, out RaycastHit hit, rayDistance))
        {
            if (hit.collider.CompareTag("Ball"))
            {
                Rigidbody ballRb = hit.collider.attachedRigidbody;
                if (ballRb == null) return;

                // We'll apply a small "nudge" impulse to push the ball out of tunneling.
                // Blend between reflection of ball velocity and paddle velocity:
                Vector3 ballVelocity = ballRb.velocity;
                Vector3 reflectDir = Vector3.Reflect(ballVelocity, hit.normal).normalized;
                Vector3 finalDir = Vector3.Lerp(reflectDir, paddleVelocity.normalized, directionBlend).normalized;

                // A small impulse to correct any partial tunneling
                ballRb.AddForce(finalDir * nudgeImpulse, ForceMode.Impulse);

                if (debugLogs) 
                    Debug.Log("Fallback nudge applied to ball. Force: " + nudgeImpulse);
            }
        }
    }
}
