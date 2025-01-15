using UnityEngine;

public class SimpleBallShooter : MonoBehaviour
{
    [Header("Settings")]
    public GameObject ballPrefab;
    public Transform targetPosition;
    public float shootSpeed = 10f;
    public float shootInterval = 3f;
    
    private float nextShootTime;

    void Start()
    {
        nextShootTime = Time.time + shootInterval;
    }

    void Update()
    {
        if (Time.time >= nextShootTime)
        {
            ShootBall();
            nextShootTime = Time.time + shootInterval;
        }
    }

    void ShootBall()
    {
        // Create ball at shooter position
        GameObject ball = Instantiate(ballPrefab, transform.position, Quaternion.identity);
        
        // Get direction to target
        Vector3 direction = (targetPosition.position - transform.position).normalized;
        
        // Add velocity towards target
        Rigidbody rb = ball.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = direction * shootSpeed;
        }
        
        // Cleanup ball after 10 seconds
        Destroy(ball, 10f);
    }
}