using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyStateMachine))]
[RequireComponent(typeof(EnemyPerception))]
[RequireComponent(typeof(EnemyMovement))]
[RequireComponent(typeof(EnemyCombat))]
[RequireComponent(typeof(EnemyHealth))]
public class Enemy : MonoBehaviour
{
    [HideInInspector] public EnemyAnimatorController animatorController;

    [HideInInspector] public NavMeshAgent agent;
    [HideInInspector] public Rigidbody2D rb;
    [HideInInspector] public EnemyStateMachine stateMachine;
    [HideInInspector] public EnemyPerception perception;
    [HideInInspector] public EnemyMovement movement;
    [HideInInspector] public EnemyCombat combat;
    [HideInInspector] public EnemyHealth health;

    private void Awake()
    {
        CacheComponents();
        SetupAgent();
    }

    private void FixedUpdate()
    {
        if (IsDead()) return;

        Vector2 desiredDirection = GetDesiredDirection();

        if (desiredDirection.sqrMagnitude > 0.01f)
        {
            RotateTowards(desiredDirection);
        }
    }

    // -------------------- Setup --------------------

    private void CacheComponents()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody2D>();
        stateMachine = GetComponent<EnemyStateMachine>();
        perception = GetComponent<EnemyPerception>();
        movement = GetComponent<EnemyMovement>();
        combat = GetComponent<EnemyCombat>();
        health = GetComponent<EnemyHealth>();

        if (animatorController == null)
            animatorController = GetComponent<EnemyAnimatorController>();
    }

    private void SetupAgent()
    {
        if (agent == null) return;

        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.speed = movement.moveSpeed;
        agent.angularSpeed = 360f;
        agent.acceleration = 20f;
    }

    // -------------------- State Checks --------------------

    private bool IsDead()
    {
        return stateMachine.currentState == EnemyStateMachine.State.Dead;
    }

    private bool IsChasingOrAttacking()
    {
        return stateMachine.currentState == EnemyStateMachine.State.Attack ||
               stateMachine.currentState == EnemyStateMachine.State.Chase;
    }

    // -------------------- Movement --------------------

    private Vector2 GetDesiredDirection()
    {
        if (IsChasingOrAttacking())
        {
            return GetDirectionToPlayer();
        }

        return GetMovementVelocity();
    }

    private Vector2 GetDirectionToPlayer()
    {
        if (perception.player == null) return Vector2.zero;

        return (Vector2)perception.player.position - (Vector2)transform.position;
    }

    private Vector2 GetMovementVelocity()
    {
        Vector2 velocity = agent.velocity.sqrMagnitude > 0.01f
            ? (Vector2)agent.velocity
            : rb.linearVelocity;

        return velocity.sqrMagnitude > 0.1f ? velocity : Vector2.zero;
    }

    // -------------------- Rotation --------------------

    private void RotateTowards(Vector2 direction)
    {
        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float currentAngle = rb.rotation;

        float newAngle = Mathf.MoveTowardsAngle(
            currentAngle,
            targetAngle,
            movement.rotationSpeed * Time.fixedDeltaTime
        );

        rb.MoveRotation(newAngle);
    }
}