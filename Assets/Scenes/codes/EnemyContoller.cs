using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public Transform player; // Reference to the player
    public float moveSpeed = 3f; // Speed of the enemy's movement
    public float minDistance = 2f; // Minimum distance from the player
    public float rotationSpeed = 5f; // Speed of rotation toward the player
    public float shootInterval = 2f; // Time between shots
    public GameObject projectilePrefab; // Prefab for the projectile
    public Transform firePoint; // Where the projectiles spawn
    [SerializeField] private float separationRadius = 2f; // Radius for detecting nearby enemies
    [SerializeField] private LayerMask enemyLayer; // Layer mask for identifying other enemies


    private Rigidbody rb;
    private float nextShotTime;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Lock unwanted rotation
        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationZ;
    }

    void Update()
    {
        if (player != null)
        {
            RotateTowardsPlayer();
            MoveTowardsPlayer();

            if (Time.time >= nextShotTime)
            {
                Shoot();
                nextShotTime = Time.time + shootInterval;
            }
        }
    }

    void RotateTowardsPlayer()
    {
        Vector3 direction = -(player.position - transform.position).normalized;
        direction.y = 0;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
    }

    void MoveTowardsPlayer()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Move towards the player if beyond the minimum distance
        if (distanceToPlayer > minDistance)
        {
            // Direction toward the player
            Vector3 directionToPlayer = (player.position - transform.position).normalized;

            // Separation force to avoid other enemies
            Vector3 separationForce = Vector3.zero;

            // Find all nearby enemies
            Collider[] nearbyEnemies = Physics.OverlapSphere(transform.position, separationRadius, enemyLayer);

            foreach (Collider otherEnemy in nearbyEnemies)
            {
                if (otherEnemy.gameObject != gameObject) // Avoid self
                {
                    Vector3 directionAway = transform.position - otherEnemy.transform.position;
                    float distance = directionAway.magnitude;

                    if (distance > 0)
                    {
                        separationForce += directionAway.normalized / distance; // Stronger force when closer
                    }
                }
            }

            // Combine forces: Move toward the player + Separation force
            Vector3 finalDirection = directionToPlayer + separationForce.normalized;
            finalDirection = finalDirection.normalized; // Normalize the combined direction

            // Apply force to Rigidbody
            rb.AddForce(finalDirection * moveSpeed, ForceMode.Acceleration);

            // Limit maximum speed
            if (rb.linearVelocity.magnitude > moveSpeed)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * moveSpeed;
            }
        }
        else
        {
            // Gradually slow down when within the minimum distance
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.deltaTime * moveSpeed);
        }
    }

    void Shoot()
    {
        if (firePoint != null && projectilePrefab != null)
        {
            // Instantiate the projectile
            GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);

            // Get the projectile's Rigidbody
            Rigidbody projectileRb = projectile.GetComponent<Rigidbody>();

            if (projectileRb != null)
            {
                projectileRb.useGravity = true;

                // Calculate the direction to the player
                Vector3 toPlayer = player.position - firePoint.position;
                float gravity = Mathf.Abs(Physics.gravity.y); // Absolute gravity value
                float heightDifference = toPlayer.y; // Vertical difference
                toPlayer.y = 0; // Flatten direction for horizontal velocity calculation

                float horizontalDistance = toPlayer.magnitude; // Horizontal distance
                float projectileSpeed = 400f; // Adjust based on desired speed

                if (horizontalDistance > 0.01f) // Avoid division by zero
                {
                    // Compute the required initial vertical velocity
                    float verticalVelocity = Mathf.Sqrt(2 * gravity * Mathf.Abs(heightDifference));

                    // Compute total flight time considering up-and-down motion
                    float totalFlightTime = (verticalVelocity / gravity) + Mathf.Sqrt((2 * Mathf.Abs(heightDifference)) / gravity);

                    // Compute horizontal velocity components
                    float horizontalSpeed = horizontalDistance / totalFlightTime;
                    Vector3 horizontalVelocity = toPlayer.normalized * horizontalSpeed;

                    // Combine horizontal and vertical velocities
                    Vector3 velocity = horizontalVelocity + Vector3.up * verticalVelocity;

                    // Assign the velocity to the projectile
                    projectileRb.linearVelocity = velocity;
                }
                else
                {
                    Debug.LogWarning("Horizontal distance is too small. Skipping projectile launch.");
                }
            }
        }
    }


}
