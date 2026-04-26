using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    public enum IdleBehavior { Patrol, Roamer, Static }

    // -------------------- GENERAL SETTINGS --------------------
    [Header("General Settings")]
    public float moveSpeed = 3.5f;
    public float rotationSpeed = 10f;

    // -------------------- IDLE BEHAVIOR --------------------
    [Header("Idle Behavior")]
    public IdleBehavior idleBehavior = IdleBehavior.Patrol;

    // -------------------- STATIC SETTINGS --------------------
    [Header("Static Behavior")]
    public bool returnToInitialPositionAfterInspect = false;

    // -------------------- PATROL SETTINGS --------------------
    [Header("Patrol Settings")]
    [Header("Checkpoints:")]
    public Transform[] patrolNodes;

    [Range(0.1f, 1f)] public float stoppingDistance = 0.3f;
    [Range(0.5f, 3f)] public float decelerationDistance = 1.5f;
    [Range(0f, 3f)] public float waitTimeAtNode = 0.5f;

    // -------------------- ROAMER SETTINGS --------------------
    [Header("Roamer Settings")]
    [Range(0.5f, 5f)] public float waitTimeMin = 1f;
    [Range(0.5f, 5f)] public float waitTimeMax = 3f;

    [Range(1f, 10f)] public float roamDistanceMin = 2f;
    [Range(1f, 10f)] public float roamDistanceMax = 5f;

    [Tooltip("Maximum attempts to find a valid roam position before giving up")]
    public int maxRoamAttempts = 10;

    // -------------------- SEARCH SETTINGS --------------------
    [Header("Search Settings")]
    public float searchRadius = 2f;
    public int searchPoints = 4;

    public IdleBehavior currentIdleBehavior => idleBehavior;

    // -------------------- REFERENCES --------------------
    private Enemy enemy;
    private NavMeshAgent agent => enemy.agent;
    private Rigidbody2D rb => enemy.rb;
    private EnemyStateMachine sm => enemy.stateMachine;

    // -------------------- PATROL STATE --------------------
    private int currentPatrolIndex = 0;
    private bool isWaitingAtNode = false;

    // -------------------- ROAMER STATE --------------------
    private enum RoamerState { Waiting, Moving }
    private RoamerState roamerState = RoamerState.Waiting;

    private float roamerTimer;
    private float currentWaitTime;
    private Vector2 roamTarget;

    // -------------------- INVESTIGATION / SEARCH --------------------
    private Vector2 investigationTarget;
    private bool hasReachedInvestigationPoint = false;

    private Vector2[] searchPositions;
    private int currentSearchPoint;
    private bool isInSearchMode = false;

    private Vector3 initialPosition;

    // -------------------- UNITY EVENTS --------------------
    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        initialPosition = transform.position;
    }

    private void Start()
    {
        if (idleBehavior == IdleBehavior.Patrol && patrolNodes.Length > 0)
        {
            currentPatrolIndex = 0;
            MoveToNextPatrolNode();
        }
    }

    private void Update()
    {
        if (sm.currentState == EnemyStateMachine.State.Dead) return;

        // -------------------- ANIMATION SPEED --------------------
        float speed;

        if (idleBehavior == IdleBehavior.Roamer && sm.currentState == EnemyStateMachine.State.Idle)
            speed = rb.linearVelocity.magnitude;
        else
            speed = agent.velocity.magnitude;

        enemy.animatorController?.SetMoveSpeed(speed);

        // -------------------- STATE HANDLING --------------------
        switch (sm.currentState)
        {
            case EnemyStateMachine.State.Idle:
                HandleIdleMovement();
                break;

            case EnemyStateMachine.State.Inspect:
                HandleInspectMovement();
                break;

            case EnemyStateMachine.State.Chase:
                UpdateNavigation();
                break;

            case EnemyStateMachine.State.Attack:
                agent.ResetPath();
                agent.isStopped = true;
                break;
        }
    }

    // -------------------- IDLE MOVEMENT --------------------
    private void HandleIdleMovement()
    {
        switch (idleBehavior)
        {
            case IdleBehavior.Patrol:
                UpdatePatrol();
                break;

            case IdleBehavior.Roamer:
                UpdateRoamer();
                break;

            case IdleBehavior.Static:
                agent.ResetPath();
                agent.isStopped = true;
                rb.linearVelocity = Vector2.zero;
                break;
        }
    }

    // -------------------- INSPECT MOVEMENT --------------------
    private void HandleInspectMovement()
    {
        if (agent.pathPending) return;

        if (!hasReachedInvestigationPoint)
        {
            if (agent.remainingDistance <= stoppingDistance)
            {
                hasReachedInvestigationPoint = true;
                StartSearchPattern(investigationTarget);
            }
            else
            {
                UpdateNavigation();
            }
        }
        else if (isInSearchMode)
        {
            if (agent.remainingDistance <= stoppingDistance)
                MoveToNextSearchPoint();
            else
                UpdateNavigation();
        }
    }

    // -------------------- NAVIGATION --------------------
    private void UpdateNavigation()
    {
        if (agent.pathPending) return;

        agent.isStopped = false;

        if (agent.remainingDistance <= decelerationDistance)
        {
            float slowFactor = agent.remainingDistance / decelerationDistance;
            agent.speed = Mathf.Lerp(0.5f, moveSpeed, slowFactor);
        }
        else
        {
            agent.speed = moveSpeed;
        }
    }

    // -------------------- INVESTIGATION --------------------
    public void GoToLastKnownPosition(Vector2 position)
    {
        investigationTarget = position;
        hasReachedInvestigationPoint = false;
        isInSearchMode = false;

        agent.isStopped = false;
        agent.ResetPath();
        agent.SetDestination(position);
    }

    public void Investigate(Vector3 position)
    {
        agent.isStopped = false;
        agent.ResetPath();
        agent.SetDestination(position);
    }

    // -------------------- SEARCH PATTERN --------------------
    private void StartSearchPattern(Vector2 center)
    {
        searchPositions = new Vector2[searchPoints];

        for (int i = 0; i < searchPoints; i++)
        {
            float angle = i * (360f / searchPoints) + Random.Range(-30f, 30f);
            Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            searchPositions[i] = center + dir * searchRadius;
        }

        currentSearchPoint = 0;
        isInSearchMode = true;
        agent.SetDestination(searchPositions[currentSearchPoint]);
    }

    private void MoveToNextSearchPoint()
    {
        currentSearchPoint++;

        if (currentSearchPoint >= searchPositions.Length)
        {
            isInSearchMode = false;
            hasReachedInvestigationPoint = false;
            sm.ChangeState(EnemyStateMachine.State.Idle);
        }
        else
        {
            agent.SetDestination(searchPositions[currentSearchPoint]);
        }
    }

    // -------------------- MOVEMENT CONTROL --------------------
    public void StopMoving()
    {
        isWaitingAtNode = false;
        isInSearchMode = false;
        hasReachedInvestigationPoint = false;

        CancelInvoke();

        agent.ResetPath();
        agent.isStopped = true;
        rb.linearVelocity = Vector2.zero;
    }

    public void ResumeIdleBehavior()
    {
        isInSearchMode = false;
        hasReachedInvestigationPoint = false;

        agent.isStopped = false;
        agent.ResetPath();

        if (idleBehavior == IdleBehavior.Patrol && patrolNodes.Length > 0)
        {
            MoveToNextPatrolNode();
        }
        else if (idleBehavior == IdleBehavior.Roamer)
        {
            roamerState = RoamerState.Waiting;
            roamerTimer = 0f;
            currentWaitTime = Random.Range(waitTimeMin, waitTimeMax);
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void ReturnToInitialPositionIfNeeded()
    {
        if (idleBehavior == IdleBehavior.Static && returnToInitialPositionAfterInspect)
        {
            Investigate(initialPosition);
        }
    }

    // -------------------- PATROL --------------------
    private void MoveToNextPatrolNode()
    {
        if (patrolNodes.Length == 0) return;

        Transform targetNode = patrolNodes[currentPatrolIndex];

        agent.stoppingDistance = stoppingDistance;
        agent.SetDestination(targetNode.position);

        currentPatrolIndex = (currentPatrolIndex + 1) % patrolNodes.Length;
        isWaitingAtNode = false;
    }

    private void UpdatePatrol()
    {
        if (agent.pathPending) return;

        if (!isWaitingAtNode && agent.remainingDistance <= stoppingDistance)
        {
            isWaitingAtNode = true;
            agent.ResetPath();

            if (waitTimeAtNode > 0f)
                Invoke(nameof(MoveToNextPatrolNode), waitTimeAtNode);
            else
                MoveToNextPatrolNode();
        }
        else if (!isWaitingAtNode)
        {
            UpdateNavigation();
        }
    }

    // -------------------- ROAMER --------------------
    private void UpdateRoamer()
    {
        roamerTimer += Time.deltaTime;

        switch (roamerState)
        {
            case RoamerState.Waiting:
                rb.linearVelocity = Vector2.zero;
                agent.ResetPath();
                agent.isStopped = true;

                if (roamerTimer >= currentWaitTime)
                {
                    if (TryFindRoamTarget(out Vector2 target))
                    {
                        roamTarget = target;
                        roamerState = RoamerState.Moving;
                        roamerTimer = 0f;

                        agent.isStopped = false;
                        agent.SetDestination(roamTarget);
                    }
                    else
                    {
                        roamerTimer = 0f;
                        currentWaitTime = Random.Range(waitTimeMin, waitTimeMax);
                    }
                }
                break;

            case RoamerState.Moving:
                if (!agent.pathPending && agent.remainingDistance <= stoppingDistance)
                {
                    roamerState = RoamerState.Waiting;
                    roamerTimer = 0f;
                    currentWaitTime = Random.Range(waitTimeMin, waitTimeMax);

                    agent.ResetPath();
                    agent.isStopped = true;
                    rb.linearVelocity = Vector2.zero;
                }
                else
                {
                    UpdateNavigation();
                }
                break;
        }
    }

    // -------------------- ROAM TARGET --------------------
    private bool TryFindRoamTarget(out Vector2 target)
    {
        target = Vector2.zero;

        Vector2[] directions =
        {
            Vector2.up, Vector2.down, Vector2.left, Vector2.right,
            new Vector2(1,1).normalized,
            new Vector2(-1,1).normalized,
            new Vector2(1,-1).normalized,
            new Vector2(-1,-1).normalized
        };

        for (int i = 0; i < maxRoamAttempts; i++)
        {
            Vector2 dir = directions[Random.Range(0, directions.Length)];

            float angle = Random.Range(-15f, 15f);
            dir = Quaternion.Euler(0, 0, angle) * dir;

            float distance = Random.Range(roamDistanceMin, roamDistanceMax);
            Vector2 potential = (Vector2)transform.position + dir * distance;

            if (IsPathClear(transform.position, potential))
            {
                target = potential;
                return true;
            }
        }

        return false;
    }

    private bool IsPathClear(Vector2 from, Vector2 to)
    {
        Vector2 direction = to - from;
        float distance = direction.magnitude;

        RaycastHit2D hit = Physics2D.Raycast(
            from,
            direction.normalized,
            distance,
            enemy.perception.obstacleLayer
        );

        return hit.collider == null;
    }

    // -------------------- GIZMOS --------------------
    private void OnDrawGizmosSelected()
    {
        if (patrolNodes != null && patrolNodes.Length > 0)
        {
            Gizmos.color = Color.cyan;

            for (int i = 0; i < patrolNodes.Length; i++)
            {
                if (patrolNodes[i] == null) continue;

                Gizmos.DrawSphere(patrolNodes[i].position, 0.3f);

                int next = (i + 1) % patrolNodes.Length;
                if (patrolNodes[next] != null)
                    Gizmos.DrawLine(patrolNodes[i].position, patrolNodes[next].position);
            }
        }

        if (Application.isPlaying && isInSearchMode)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(investigationTarget, searchRadius);
        }

        if (idleBehavior == IdleBehavior.Roamer && Application.isPlaying)
        {
            Gizmos.color = new Color(0, 1, 0, 0.2f);
            Gizmos.DrawWireSphere(transform.position, roamDistanceMax);

            if (roamerState == RoamerState.Moving)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, roamTarget);
                Gizmos.DrawSphere(roamTarget, 0.3f);
            }
        }
    }
}