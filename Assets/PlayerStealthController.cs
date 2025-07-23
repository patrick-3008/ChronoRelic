using UnityEngine;

public class PlayerSoundGenerator : MonoBehaviour
{
    [Header("Sound Settings")]
    public float walkingSoundIntensity = 1.0f;
    public float runningSoundIntensity = 2.5f;
    public float crouchingSoundIntensity = 0.3f;
    public float maxSoundRange = 25f;

    [Header("Movement Detection")]
    public float movementThreshold = 0.1f;
    public LayerMask obstacleLayerMask = -1;

    private Animator animator;
    private CharacterController characterController;
    private Vector3 lastPosition;
    private float currentSoundIntensity;

    // Animation parameter names
    private readonly string WALKING_PARAM = "Walking";
    private readonly string WALK_RUN_PARAM = "Walk_Run";
    private readonly string IS_CROUCHING_PARAM = "IsCrouching";

    void Start()
    {
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        lastPosition = transform.position;
    }

    void Update()
    {
        CalculateMovementSound();
        BroadcastSound();
    }

    void CalculateMovementSound()
    {
        bool isWalking = animator.GetBool(WALKING_PARAM);

        if (!isWalking)
        {
            currentSoundIntensity = 0f;
            lastPosition = transform.position;
            return;
        }

        float walkRunBlend = animator.GetFloat(WALK_RUN_PARAM);
        bool isCrouching = animator.GetBool(IS_CROUCHING_PARAM);

        if (isCrouching)
        {
            currentSoundIntensity = crouchingSoundIntensity;
        }
        else
        {
            currentSoundIntensity = Mathf.Lerp(walkingSoundIntensity, runningSoundIntensity, walkRunBlend);
        }

        lastPosition = transform.position;
    }

    void BroadcastSound()
    {
        if (currentSoundIntensity <= 0f) return;

        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, maxSoundRange);

        foreach (Collider col in nearbyColliders)
        {
            Transform enemyTransform = col.transform;

            // Try ArcherEnemy
            ArcherEnemy archer = col.GetComponent<ArcherEnemy>();
            if (archer != null)
            {
                float distance = Vector3.Distance(transform.position, archer.transform.position);
                float soundAtTarget = CalculateSoundWithOcclusion(archer.transform.position, distance);

                if (soundAtTarget > 0f)
                {
                    archer.OnSoundHeard(transform.position, soundAtTarget);
                }

                continue; // skip further checks if already handled
            }

            // Try GuardAI
            GuardAI guard = col.GetComponent<GuardAI>();
            if (guard != null)
            {
                float distance = Vector3.Distance(transform.position, guard.transform.position);
                float soundAtTarget = CalculateSoundWithOcclusion(guard.transform.position, distance);

                if (soundAtTarget > 0f)
                {
                    guard.OnSoundHeard(transform.position, soundAtTarget);
                }
            }
        }
    }

    float CalculateSoundWithOcclusion(Vector3 targetPosition, float distance)
    {
        float distanceAttenuation = Mathf.Exp(-distance / maxSoundRange * 2f);
        float baseSound = currentSoundIntensity * distanceAttenuation;

        Vector3 direction = (targetPosition - transform.position).normalized;
        RaycastHit hit;

        if (Physics.Raycast(transform.position, direction, out hit, distance, obstacleLayerMask))
        {
            baseSound *= 0.2f; // Reduce by 80% through walls
        }

        return baseSound;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, maxSoundRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, currentSoundIntensity * 3f);
    }

    /*void OnGUI()
    {
        if (Application.isPlaying && animator != null)
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 150));
            GUILayout.Label($"Walking: {animator.GetBool(WALKING_PARAM)}");
            GUILayout.Label($"Walk_Run Blend: {animator.GetFloat(WALK_RUN_PARAM):F2}");
            GUILayout.Label($"Is Crouching: {animator.GetBool(IS_CROUCHING_PARAM)}");
            GUILayout.Label($"Current Sound Intensity: {currentSoundIntensity:F2}");
            GUILayout.EndArea();
        }
    }*/
}
