using UnityEngine;

public class BallShooter : MonoBehaviour
{
    [Header("Ball & Target Settings")]
    public GameObject ballPrefab;
    public Transform[] targetPositions;

    [Header("Shooting Speed Settings")]
    public float minShootSpeed = 5f;
    public float maxShootSpeed = 15f;

    [Header("Shooter Movement Settings")]
    public float minZOffset = -1f;
    public float maxZOffset = 1f;

    [Header("Timing Settings")]
    public float shootInterval = 3f;
    public float ballLifetime = 10f;

    private float nextShootTime;
    private Vector3 initialPosition;

    void Start()
    {
        initialPosition = transform.position;
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
        if (targetPositions == null || targetPositions.Length == 0) return;

        // 1. Move shooter in global Z
        Vector3 newPosition = initialPosition;
        newPosition.z += Random.Range(minZOffset, maxZOffset);
        transform.position = newPosition;

        // 2. Spawn ball
        GameObject ball = Instantiate(ballPrefab, transform.position, Quaternion.identity);
        
        // 3. Pick random target and speed
        Transform target = targetPositions[Random.Range(0, targetPositions.Length)];
        float speed = Random.Range(minShootSpeed, maxShootSpeed);

        // 4. Shoot directly at target
        Rigidbody rb = ball.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 direction = (target.position - transform.position).normalized;
            rb.velocity = direction * speed;
        }

        // 5. Reset shooter position
        transform.position = initialPosition;

        // 6. Destroy ball after lifetime
        Destroy(ball, ballLifetime);
    }
}