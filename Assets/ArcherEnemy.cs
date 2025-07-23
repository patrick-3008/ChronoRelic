using System.Collections;
using UnityEngine;

public class ArcherEnemy : MonoBehaviour
{

    public AudioClip alertClip;
    public AudioClip arrowStab;
    public AudioSource audioSource;
    // Player detection
    public Transform Player;
    public Transform detectionOrigin;
    public float detectionRadius = 10f;
    public float viewAngle = 120f;
    public string playerTag = "Player";
    public LayerMask layerToIgnore;

    // Movement / Rotation
    public float rotationSpeed = 2f;
    private bool playerInSight = false;

    // Sound detection
    private Vector3 lastHeardSoundPosition;
    private float lastHeardSoundIntensity = 0f;
    public float soundDetectionThreshold = 6f;
    private float pauseAfterSoundDuration = 5f;
    private float pauseTimer = 0f;
    private bool waitingAfterSound = false;

    // Patrol rotation pattern
    private readonly float[] patrolAngles = { 0f, 90f, 180f, 90f, 0f };
    private int currentPatrolIndex = 0;
    private float patrolTimer = 0f;
    private float patrolDelay = 5f;
    private bool isPatrolling = true;
    private Quaternion baseRotation;

    // Shooting
    public GameObject arrowPrefab;
    public Transform arrowSpawnPoint;
    public float arrowSpeed = 1f;

    // Visuals
    public bool showDebugVisuals = true;
    private Light visionLight;

    // Components
    private Animator animator;
    private Transform playerTransform;

    void Start()
    {
        animator = GetComponent<Animator>();
        baseRotation = transform.rotation;

        if (detectionOrigin == null)
            detectionOrigin = transform;

        if (arrowSpawnPoint == null)
            arrowSpawnPoint = transform;

        if (Player != null)
            playerTransform = Player;
    }

    void Update()
    {
        DetectPlayer();

        if (playerTransform != null && lastHeardSoundIntensity >= soundDetectionThreshold)
        {
            RotateTowardsPlayer();
        }

        if (showDebugVisuals)
        {
            DrawViewCone();
        }

        if (visionLight != null)
        {
            visionLight.transform.position = detectionOrigin.position;
            visionLight.transform.rotation = Quaternion.LookRotation(detectionOrigin.forward);
        }

        // Handle sound fade and pause timer
        if (lastHeardSoundIntensity > 0f)
        {
            lastHeardSoundIntensity -= Time.deltaTime * 2f;

            if (lastHeardSoundIntensity <= 0f)
            {
                lastHeardSoundIntensity = 0f;
                waitingAfterSound = true;
                pauseTimer = 0f;
                isPatrolling = false;
            }
        }

        if (waitingAfterSound && !playerInSight)
        {
            pauseTimer += Time.deltaTime;
            if (pauseTimer >= pauseAfterSoundDuration)
            {
                waitingAfterSound = false;
                isPatrolling = true;
                patrolTimer = 0f;
            }
        }

        // Only patrol if not seeing the player
        if (isPatrolling && !playerInSight)
        {
            PatrolRotate();
        }

    }

    private void PatrolRotate()
    {
        patrolTimer += Time.deltaTime;

        float targetY = patrolAngles[currentPatrolIndex];
        Quaternion targetRotation = baseRotation * Quaternion.Euler(0f, targetY, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        if (Quaternion.Angle(transform.rotation, targetRotation) < 1f && patrolTimer >= patrolDelay && !playerInSight)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolAngles.Length;
            patrolTimer = 0f;
        }
    }

    private void DetectPlayer()
    {
        if (Player != null && playerTransform == null)
            playerTransform = Player;

        if (playerTransform == null) return;

        Vector3 eyePos = detectionOrigin.position;
        Vector3 chestPos = playerTransform.position + new Vector3(0, 1.0f, 0);
        Vector3 dirToPlayer = (chestPos - eyePos).normalized;
        float angle = Vector3.Angle(detectionOrigin.forward, dirToPlayer);
        float distance = Vector3.Distance(eyePos, chestPos);

        playerInSight = false;

        if (distance <= detectionRadius && angle < viewAngle / 2f)
        {
            RaycastHit hit;
            if (Physics.Raycast(eyePos, dirToPlayer, out hit, detectionRadius, ~layerToIgnore))
            {
                if (hit.collider.CompareTag(playerTag))
                {
                    playerInSight = true;

                    if (animator != null)
                    {
                        animator.SetBool("is_aiming", true);
                        animator.SetBool("is_walking", false);
                    }

                    Debug.DrawLine(eyePos, chestPos, Color.green);
                }
                else
                {
                    Debug.DrawLine(eyePos, hit.point, Color.red);
                }
            }
        }

        if (!playerInSight)
        {
            StopAiming();
        }
    }

    private void RotateTowardsPlayer()
    {
        if (playerTransform == null) return;

        Vector3 directionToPlayer = playerTransform.position - transform.position;
        directionToPlayer.y = 0;

        if (directionToPlayer.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            isPatrolling = false;
            waitingAfterSound = false;

            if (animator != null)
            {
                animator.SetBool("is_walking", false);
                animator.SetBool("is_aiming", false);
            }
        }
    }

    public void ShootArrow()
    {
        if (!playerInSight || arrowPrefab == null || playerTransform == null) return;

        Vector3 targetPosition = playerTransform.position + new Vector3(
            Random.Range(-1.5f, 1.5f),
            1.5f + Random.Range(-1f, 1f),
            Random.Range(-1.5f, 1.5f)
        );

        Vector3 arrowPos = arrowSpawnPoint.position;
        Vector3 direction = (targetPosition - arrowPos).normalized;

        GameObject arrow = Instantiate(arrowPrefab, arrowPos, Quaternion.LookRotation(direction));
        arrow.transform.eulerAngles = new Vector3(90, arrow.transform.eulerAngles.y, arrow.transform.eulerAngles.z);

        StartCoroutine(MoveArrow(arrow, targetPosition));
    }

    private IEnumerator MoveArrow(GameObject arrow, Vector3 targetPosition)
    {
        Vector3 startPosition = arrow.transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < 1f)
        {
            if (playerTransform != null)
                targetPosition = playerTransform.position + new Vector3(0, 1.5f, 0);

            elapsedTime += Time.deltaTime * arrowSpeed;
            arrow.transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime);

            if (Vector3.Distance(arrow.transform.position, targetPosition) < 0.5f)
            {
                PlayerCombatAndHealth playerCombat = playerTransform.GetComponent<PlayerCombatAndHealth>();
                if (playerCombat != null)
                {
                    playerCombat.health -= 100;
                    audioSource.PlayOneShot(arrowStab);
                    Debug.Log("Player took 30 damage. Remaining health: " + playerCombat.health);
                }

                arrow.transform.SetParent(playerTransform);
                break;
            }

            yield return null;
        }

        Destroy(arrow, 1.5f);
    }

    private void StopAiming()
    {
        if (animator != null)
        {
            animator.SetBool("is_aiming", false);
            animator.SetBool("is_walking", false);
        }
    }

    private void DrawViewCone()
    {
        float halfFOV = viewAngle / 2.0f;
        Quaternion leftRayRotation = Quaternion.AngleAxis(-halfFOV, Vector3.up);
        Quaternion rightRayRotation = Quaternion.AngleAxis(halfFOV, Vector3.up);
        Vector3 leftRayDirection = leftRayRotation * detectionOrigin.forward;
        Vector3 rightRayDirection = rightRayRotation * detectionOrigin.forward;

        Debug.DrawRay(detectionOrigin.position, leftRayDirection * detectionRadius, Color.blue);
        Debug.DrawRay(detectionOrigin.position, rightRayDirection * detectionRadius, Color.blue);
    }

    void OnDrawGizmosSelected()
    {
        if (detectionOrigin != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(detectionOrigin.position, detectionRadius);

            float halfFOV = viewAngle / 2.0f;
            Quaternion leftRayRotation = Quaternion.AngleAxis(-halfFOV, Vector3.up);
            Quaternion rightRayRotation = Quaternion.AngleAxis(halfFOV, Vector3.up);
            Vector3 leftRayDirection = leftRayRotation * detectionOrigin.forward;
            Vector3 rightRayDirection = rightRayRotation * detectionOrigin.forward;

            Gizmos.color = Color.blue;
            Gizmos.DrawRay(detectionOrigin.position, leftRayDirection * detectionRadius);
            Gizmos.DrawRay(detectionOrigin.position, rightRayDirection * detectionRadius);
        }
    }

    public void OnSoundHeard(Vector3 soundPosition, float intensity)
    {
        if (intensity >= soundDetectionThreshold)
        {
            lastHeardSoundPosition = soundPosition;
            lastHeardSoundIntensity = intensity;

            isPatrolling = false;
            waitingAfterSound = false;
            pauseTimer = 0f;
        }
    }

    public void PlayAlertSound()
    {
        if (alertClip != null && audioSource != null)
            audioSource.PlayOneShot(alertClip);
    }
}
