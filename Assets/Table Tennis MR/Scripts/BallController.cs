using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BallController : MonoBehaviour
{
    [Header("Ball Speed Settings")]
    [Tooltip("Maximum speed the ball is allowed to reach.")]
    public float maxBallSpeed = 15f;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        // Ensure the ball uses continuous collision detection to reduce tunneling.
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        
        // Interpolation can smooth out its motion visually.
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void FixedUpdate()
    {
        // Clamp the ball's speed so it doesn't become a "bullet."
        if (rb.velocity.magnitude > maxBallSpeed)
        {
            rb.velocity = rb.velocity.normalized * maxBallSpeed;
        }
    }

    // OPTIONAL: If you want to debug collisions or add extra logic upon collisions, 
    // you can handle OnCollisionEnter here too.
    private void OnCollisionEnter(Collision collision)
    {
        // Example: Just for debugging
        // if (collision.collider.CompareTag("Paddle"))
        // {
        //     Debug.Log("Ball collided with paddle.");
        // }
    }
}