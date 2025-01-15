using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TableTennisBall : MonoBehaviour
{
    [Header("Ball Properties")]
    public float mass = 0.0027f; // Standard table tennis ball mass in kg
    public float drag = 0.1f;
    public float angularDrag = 0.05f;
    public float maxVelocity = 30f;
    
    [Header("Bounce Properties")]
    public float bounceEnergyLoss = 0.05f;
    public float minBounceVelocity = 0.1f;
    public float surfaceFriction = 0.5f;
    
    private Rigidbody rb;
    private PhysicMaterial ballPhysicsMaterial;

    void Start()
    {
        SetupRigidbody();
        SetupPhysicsMaterial();
    }

    private void SetupRigidbody()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = mass;
        rb.drag = drag;
        rb.angularDrag = angularDrag;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.maxAngularVelocity = 1000; // Allow for high spin rates
    }

    private void SetupPhysicsMaterial()
    {
        ballPhysicsMaterial = new PhysicMaterial("BallPhysicsMaterial");
        ballPhysicsMaterial.bounciness = 0.9f;
        ballPhysicsMaterial.frictionCombine = PhysicMaterialCombine.Average;
        ballPhysicsMaterial.bounceCombine = PhysicMaterialCombine.Average;
        ballPhysicsMaterial.dynamicFriction = surfaceFriction;
        ballPhysicsMaterial.staticFriction = surfaceFriction * 1.5f;

        Collider ballCollider = GetComponent<Collider>();
        if (ballCollider != null)
        {
            ballCollider.material = ballPhysicsMaterial;
        }
    }

    void FixedUpdate()
    {
        // Clamp velocity to prevent unrealistic speeds
        if (rb.velocity.magnitude > maxVelocity)
        {
            rb.velocity = rb.velocity.normalized * maxVelocity;
        }

        // Apply air resistance (more realistic than linear drag)
        ApplyAirResistance();
    }

    private void ApplyAirResistance()
    {
        float airResistance = rb.velocity.magnitude * rb.velocity.magnitude * 0.0005f;
        Vector3 airResistanceForce = -rb.velocity.normalized * airResistance;
        rb.AddForce(airResistanceForce, ForceMode.Force);
    }

    void OnCollisionEnter(Collision collision)
    {
        HandleCollision(collision);
    }

    private void HandleCollision(Collision collision)
    {
        if (collision.relativeVelocity.magnitude < minBounceVelocity)
        {
            rb.velocity = Vector3.zero;
            return;
        }

        // Calculate energy loss
        Vector3 velocityAfterBounce = rb.velocity;
        velocityAfterBounce *= (1f - bounceEnergyLoss);
        rb.velocity = velocityAfterBounce;
    }
}
