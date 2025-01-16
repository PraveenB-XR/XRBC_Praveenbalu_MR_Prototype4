using UnityEngine;

public class RaycastPaddle : MonoBehaviour
{
    [Header("Raycast Grid Settings")]
    [Tooltip("Number of rays along the 'width' (Y).")]
    public int raysAlongWidth = 5;
    
    [Tooltip("Number of rays along the 'height' (Z).")]
    public int raysAlongHeight = 5;
    
    [Tooltip("Distance forward/backward along local X axis to cast rays.")]
    public float rayDistance = 0.1f;

    [Header("Visual Dimensions")]
    [Tooltip("Width of the paddle along local Y axis.")]
    public float paddleWidth = 0.15f;
    
    [Tooltip("Height of the paddle along local Z axis.")]
    public float paddleHeight = 0.15f;

    [Header("Pivot Offset")]
    [Tooltip("Manually adjust the local offset of the ray grid to align with racket face.")]
    public Vector3 localOffset = Vector3.zero;

    [Header("Hit Settings")]
    [Tooltip("Minimum paddle speed required before a hit is registered.")]
    public float minHitSpeed = 1f;

    [Tooltip("Base force added to the ball's velocity.")]
    public float baseHitForce = 10f;

    [Tooltip("Maximum possible force if you swing the paddle fast.")]
    public float maxHitForce = 25f;

    [Tooltip("Energy loss factor on bounce (1.0 = no energy loss).")]
    public float bounceFactor = 1.0f;

    [Header("Debug")]
    public bool showDebugRays = true;

    private Vector3 previousPosition;
    private Vector3 paddleVelocity;

    private void Start()
    {
        previousPosition = transform.position;
    }

    private void FixedUpdate()
    {
        // Compute how fast the paddle is moving
        paddleVelocity = (transform.position - previousPosition) / Time.fixedDeltaTime;
        previousPosition = transform.position;

        CastRayGrid();
    }

    private void CastRayGrid()
    {
        // We’ll iterate over a grid in local Y-Z space (width/height).
        for (int w = 0; w < raysAlongWidth; w++)
        {
            for (int h = 0; h < raysAlongHeight; h++)
            {
                float yOffset = Mathf.Lerp(-paddleWidth * 0.5f, paddleWidth * 0.5f, 
                                           raysAlongWidth <= 1 ? 0.5f : (float)w / (raysAlongWidth - 1));
                float zOffset = Mathf.Lerp(-paddleHeight * 0.5f, paddleHeight * 0.5f, 
                                           raysAlongHeight <= 1 ? 0.5f : (float)h / (raysAlongHeight - 1));

                // Transform this local grid point into world space
                Vector3 localPoint = localOffset 
                                     + (Vector3.up * yOffset) 
                                     + (Vector3.forward * zOffset);

                Vector3 worldStart = transform.TransformPoint(localPoint);

                // Cast in both directions along local X (to catch front/back hits)
                CastRayInDirection(worldStart, transform.right);
                CastRayInDirection(worldStart, -transform.right);
            }
        }
    }

    private void CastRayInDirection(Vector3 start, Vector3 dir)
    {
        if (Physics.Raycast(start, dir, out RaycastHit hit, rayDistance))
        {
            // Only on the ball
            if (hit.collider.CompareTag("Ball"))
            {
                TryHitBall(hit);
            }

            if (showDebugRays)
            {
                Debug.DrawRay(start, dir * rayDistance, Color.red);
            }
        }
        else if (showDebugRays)
        {
            Debug.DrawRay(start, dir * rayDistance, Color.blue);
        }
    }

    private void TryHitBall(RaycastHit hit)
    {
        Rigidbody ballRb = hit.collider.GetComponent<Rigidbody>();
        if (ballRb == null) return;

        // Only hit if we’re moving above a certain speed (i.e., actually swinging)
        float currentSpeed = paddleVelocity.magnitude;
        if (currentSpeed < minHitSpeed)
            return; // Do nothing if moving too slowly

        // Calculate total force based on how fast the paddle is moving
        float totalForce = Mathf.Clamp(baseHitForce + currentSpeed, baseHitForce, maxHitForce);

        // Reflect the ball’s velocity about the hit normal
        Vector3 reflection = Vector3.Reflect(ballRb.velocity, hit.normal);

        // Update ball velocity
        ballRb.velocity = reflection.normalized * totalForce * bounceFactor;
    }
}