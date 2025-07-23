using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GuardState
{
    Patrolling,
    Investigating,
    Chasing,
    Searching
}

public class GuardAI : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip investigationSound;

    [Header("Patrol Settings")]
    public Transform[] patrolPoints = new Transform[3];
    public float patrolRadius = 20f;
    public float waitTimeAtPoint = 2f;

    [Header("Movement")]
    public float moveSpeed = 2f;
    public float rotationSpeed = 5f;

    [Header("Detection")]
    public float hearingThreshold = 0.5f;
    public float sightRange = 20f;
    public float sightAngle = 60f;
    public float peripheralAngle = 160f;
    public LayerMask obstacleLayerMask = -1;

    [Header("Investigation Settings")]
    public float investigationTime = 3f;
    public float maxRotationTime = 2f;

    [Header("Alert Settings")]
    public float alertRadius = 30f;

    public GuardState currentState = GuardState.Patrolling;

    private Animator animator;
    private Transform player;
    private EnemyFollowAndEvadeWithAnimation enemyCombat;

    private int currentPatrolIndex = 0;
    private Vector3[] generatedPatrolPoints;
    private bool isWaiting = false;
    public Vector3 targetPosition;

    private Vector3 lastSoundPosition;
    private float investigationTimer;
    private bool isRotatingToSound = false;
    private float rotationTimer = 0f;

    private float lastPlayerSightTime;
    private bool isMoving = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        enemyCombat = GetComponent<EnemyFollowAndEvadeWithAnimation>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        InitializePatrol();
        SetState(GuardState.Patrolling);
    }

    void Update()
    {
        // ✅ Prevent all AI logic after death
        if (enemyCombat != null && enemyCombat.health <= 0)
        {
            isMoving = false;
            return;
        }

        switch (currentState)
        {
            case GuardState.Patrolling: HandlePatrolling(); break;
            case GuardState.Investigating: HandleInvestigation(); break;
            case GuardState.Chasing: HandleChasing(); break;
            case GuardState.Searching: HandleSearching(); break;
        }

        CheckPlayerInSight();
        UpdateAnimations();
    }

    void UpdateAnimations()
    {
        animator.SetBool("is_walking", isMoving);
    }

    void InitializePatrol()
    {
        List<Vector3> points = new List<Vector3>();
        foreach (Transform point in patrolPoints)
        {
            if (point != null)
                points.Add(point.position);
        }

        while (points.Count < 3)
        {
            Vector2 rand = Random.insideUnitCircle * patrolRadius;
            Vector3 pos = transform.position + new Vector3(rand.x, 0, rand.y);
            points.Add(pos);
        }

        generatedPatrolPoints = points.ToArray();
    }

    void HandlePatrolling()
    {
        if (isWaiting || generatedPatrolPoints.Length == 0) return;

        MoveTowards(targetPosition);

        if (Vector3.Distance(transform.position, targetPosition) < 5f)
        {
            StartCoroutine(WaitAtPatrolPoint());
        }
    }

    IEnumerator WaitAtPatrolPoint()
    {
        isWaiting = true;
        isMoving = false;
        yield return new WaitForSeconds(waitTimeAtPoint);

        currentPatrolIndex = (currentPatrolIndex + 1) % generatedPatrolPoints.Length;
        targetPosition = generatedPatrolPoints[currentPatrolIndex];
        isWaiting = false;
    }

    void HandleInvestigation()
    {
        if (isRotatingToSound)
        {
            rotationTimer += Time.deltaTime;
            Vector3 dir = (lastSoundPosition - transform.position).normalized;
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);

            if (Quaternion.Angle(transform.rotation, targetRot) < 5f || rotationTimer >= maxRotationTime)
            {
                isRotatingToSound = false;
                rotationTimer = 0f;
            }
        }
        else
        {
            MoveTowards(lastSoundPosition);

            if (Vector3.Distance(transform.position, lastSoundPosition) < 1f)
            {
                investigationTimer += Time.deltaTime;
                isMoving = false;

                if (investigationTimer >= investigationTime)
                {
                    SetState(GuardState.Patrolling);
                }
            }
        }
    }

    void HandleChasing()
    {
        if (player == null) return;

        targetPosition = player.position;
        MoveTowards(targetPosition);

        if (Time.time - lastPlayerSightTime > 3f)
        {
            SetState(GuardState.Searching);
        }
    }

    void HandleSearching()
    {
        if (Vector3.Distance(transform.position, targetPosition) < 1f)
        {
            targetPosition = transform.position + Random.insideUnitSphere * 5f;
            targetPosition.y = transform.position.y;
        }

        MoveTowards(targetPosition);

        if (Time.time - lastPlayerSightTime > 10f)
        {
            SetState(GuardState.Patrolling);
        }
    }

    void MoveTowards(Vector3 target)
    {
        Vector3 direction = (target - transform.position);
        direction.y = 0;

        float distance = direction.magnitude;
        if (distance > 0.05f)
        {
            direction.Normalize();
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * rotationSpeed);
            transform.position += direction * moveSpeed * Time.deltaTime;
            isMoving = true;
        }
        else
        {
            isMoving = false;
        }
    }

    void CheckPlayerInSight()
    {
        if (player == null) return;

        Vector3 dir = (player.position - transform.position).normalized;
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > sightRange) return;

        float angle = Vector3.Angle(transform.forward, dir);
        bool inCentral = angle <= sightAngle / 2f;
        bool inPeripheral = angle <= peripheralAngle;

        if (inPeripheral)
        {
            RaycastHit hit;
            if (!Physics.Raycast(transform.position + Vector3.up, dir, out hit, dist, obstacleLayerMask))
            {
                if (inCentral)
                    OnPlayerSpotted();
                else
                    OnSoundHeard(player.position, 2f);

                lastPlayerSightTime = Time.time;
            }
        }
    }

    public void OnSoundHeard(Vector3 position, float intensity)
    {
        if (intensity < hearingThreshold) return;

        if (currentState == GuardState.Patrolling || currentState == GuardState.Investigating)
        {
            lastSoundPosition = position;
            SetState(GuardState.Investigating);
            investigationTimer = 0f;
            isRotatingToSound = true;
        }
    }

    void OnPlayerSpotted()
    {
        SetState(GuardState.Chasing);
        AlertNearbyGuards();
    }

    void AlertNearbyGuards()
    {
        Collider[] nearby = Physics.OverlapSphere(transform.position, alertRadius);
        foreach (Collider col in nearby)
        {
            GuardAI guard = col.GetComponent<GuardAI>();
            if (guard != null && guard != this)
                guard.OnAlerted(player.position);
        }
    }

    public void OnAlerted(Vector3 playerPos)
    {
        if (currentState != GuardState.Chasing)
        {
            SetState(GuardState.Investigating);
            lastSoundPosition = playerPos;
            investigationTimer = 0f;
            isRotatingToSound = true;
        }
    }

    void SetState(GuardState newState)
    {
        currentState = newState;
        isMoving = false;

        if (enemyCombat != null)
            enemyCombat.enabled = (newState == GuardState.Chasing);

        if (newState == GuardState.Patrolling)
        {
            targetPosition = generatedPatrolPoints[currentPatrolIndex];
        }

        if (newState == GuardState.Investigating && audioSource != null && investigationSound != null)
        {
            audioSource.PlayOneShot(investigationSound);
        }
    }
}
