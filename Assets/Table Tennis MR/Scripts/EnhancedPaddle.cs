using UnityEngine;
using Meta.XR;

public class EnhancedPaddle : MonoBehaviour
{
    [Header("Paddle Properties")]
    public float paddleMass = 0.17f; // Standard paddle mass in kg
    public float sweetSpotMultiplier = 1.2f;
    public float edgeHitMultiplier = 0.7f;
    public float maxHitForce = 50f;
    
    [Header("Controller Settings")]
    public OVRInput.Controller controller = OVRInput.Controller.RTouch;
    public float velocitySmoothing = 0.1f;
    
    private Rigidbody paddleRb;
    private Vector3[] velocityHistory = new Vector3[5];
    private int velocityHistoryIndex = 0;
    private Vector3 previousPosition;
    private float timeSinceLastHit = 0f;
    
    void Start()
    {
        SetupPaddle();
        previousPosition = transform.position;
    }
    
    private void SetupPaddle()
    {
        paddleRb = GetComponent<Rigidbody>();
        paddleRb.mass = paddleMass;
        paddleRb.interpolation = RigidbodyInterpolation.Interpolate;
        paddleRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        
        // Setup paddle's physic material
        PhysicMaterial paddlePhysicsMaterial = new PhysicMaterial("PaddlePhysicsMaterial");
        paddlePhysicsMaterial.bounciness = 0.5f;
        paddlePhysicsMaterial.frictionCombine = PhysicMaterialCombine.Average;
        paddlePhysicsMaterial.dynamicFriction = 0.6f;
        
        Collider paddleCollider = GetComponent<Collider>();
        if (paddleCollider != null)
        {
            paddleCollider.material = paddlePhysicsMaterial;
        }
    }
    
    void Update()
    {
        UpdatePaddlePosition();
        timeSinceLastHit += Time.deltaTime;
    }
    
    private void UpdatePaddlePosition()
    {
        transform.position = OVRInput.GetLocalControllerPosition(controller);
        transform.rotation = OVRInput.GetLocalControllerRotation(controller);
        
        // Update velocity history
        Vector3 currentVelocity = (transform.position - previousPosition) / Time.deltaTime;
        velocityHistory[velocityHistoryIndex] = currentVelocity;
        velocityHistoryIndex = (velocityHistoryIndex + 1) % velocityHistory.Length;
        
        previousPosition = transform.position;
    }
    
    private Vector3 GetSmoothedVelocity()
    {
        Vector3 averageVelocity = Vector3.zero;
        foreach (Vector3 velocity in velocityHistory)
        {
            averageVelocity += velocity;
        }
        return averageVelocity / velocityHistory.Length;
    }
    
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ball") && timeSinceLastHit > 0.1f)
        {
            HandleBallHit(collision);
            timeSinceLastHit = 0f;
        }
    }
    
    private void HandleBallHit(Collision collision)
    {
        Vector3 smoothedVelocity = GetSmoothedVelocity();
        Vector3 hitPoint = collision.contacts[0].point;
        float hitFactor = CalculateHitFactor(hitPoint);
        
        Rigidbody ballRb = collision.gameObject.GetComponent<Rigidbody>();
        if (ballRb != null)
        {
            // Calculate hit direction based on paddle face normal
            Vector3 paddleNormal = transform.forward;
            Vector3 hitDirection = Vector3.Reflect(ballRb.velocity.normalized, paddleNormal);
            
            // Calculate final force
            float hitForce = Mathf.Min(smoothedVelocity.magnitude * hitFactor, maxHitForce);
            Vector3 finalVelocity = hitDirection * hitForce;
            
            // Apply the force to the ball
            ballRb.velocity = finalVelocity;
            
            // Add some spin based on hit angle and velocity
            Vector3 spinAxis = Vector3.Cross(paddleNormal, hitDirection);
            ballRb.angularVelocity = spinAxis * hitForce * 5f;
        }
    }
    
    private float CalculateHitFactor(Vector3 hitPoint)
    {
        // Calculate distance from paddle center to hit point
        float distanceFromCenter = Vector3.Distance(hitPoint, transform.position);
        float paddleRadius = GetComponent<Collider>().bounds.extents.magnitude;
        
        // Calculate hit factor based on distance from center
        if (distanceFromCenter < paddleRadius * 0.3f) // Sweet spot
        {
            return sweetSpotMultiplier;
        }
        else if (distanceFromCenter > paddleRadius * 0.7f) // Edge hit
        {
            return edgeHitMultiplier;
        }
        else // Normal hit
        {
            return 1.0f;
        }
    }
}