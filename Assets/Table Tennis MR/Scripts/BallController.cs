using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BallController : MonoBehaviour
{
    [Header("Max Speed Settings")]
    public float maxBallSpeed = 15f;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void FixedUpdate()
    {
        // Clamp the ball's velocity
        if (rb.velocity.magnitude > maxBallSpeed)
        {
            rb.velocity = rb.velocity.normalized * maxBallSpeed;
        }
    }
}