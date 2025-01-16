using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RaycastBall : MonoBehaviour
{
    public float maxSpeed = 30f;
    
    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        // Limit ball speed
        if (rb.velocity.magnitude > maxSpeed)
        {
            rb.velocity = rb.velocity.normalized * maxSpeed;
        }
    }
}