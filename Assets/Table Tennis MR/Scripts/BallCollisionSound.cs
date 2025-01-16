using UnityEngine;

public class BallCollisionSound : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioClip paddleHitSound;
    public float volume = 1f;

    [Header("Layer Settings")]
    public LayerMask paddleLayer;

    void OnCollisionEnter(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & paddleLayer) != 0)
        {
            if (paddleHitSound != null)
            {
                AudioSource.PlayClipAtPoint(paddleHitSound, collision.contacts[0].point, volume);
            }
        }
    }
}