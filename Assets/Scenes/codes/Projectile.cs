using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 40f; // Speed of the projectile
    public float lifetime = 5f; // Time before the projectile is destroyed
    public int damage = 1; // Damage dealt by the projectile

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Destroy the projectile after its lifetime expires
        Destroy(gameObject, lifetime);
    }

    public void Initialize(Vector3 direction, Collider shooter)
    {
        // Set the projectile's velocity based on the given direction
        rb.linearVelocity = direction.normalized * speed;

        // Ignore collision with the shooter's collider
        Physics.IgnoreCollision(GetComponent<Collider>(), shooter);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
            Destroy(gameObject);
        }
        else if (other.CompareTag("Obstacle"))
        {
            Destroy(gameObject);
        }
    }
}
