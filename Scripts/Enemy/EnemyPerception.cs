using UnityEngine;

public class EnemyPerception : MonoBehaviour
{
    // -------------------- HEARING --------------------
    [SerializeField] private float hearingResponseChance = 0.6f;

    // -------------------- VISION & DETECTION --------------------
    [Header("Detection")]
    public float visionRange = 10f;
    public float weaponDetectionRadius = 5f;
    public LayerMask obstacleLayer;
    public LayerMask weaponLayer;

    [HideInInspector] public Transform player; 

    // -------------------- REFERENCES --------------------
    private Enemy enemy;
    private EnemyStateMachine sm => enemy.stateMachine;

    // -------------------- STATE TRACKING --------------------
    private bool wasSeeingPlayerLastFrame = false;
    public bool WasSeeingPlayerLastFrame => wasSeeingPlayerLastFrame;

    // -------------------- UNITY EVENTS --------------------
    private void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    private void Start()
    {
        UpdatePlayerReference();
    }

    // -------------------- PLAYER REFERENCE --------------------
    private void UpdatePlayerReference()
    {
        if (player != null) return;

       
        if (GameManager.Instance != null && GameManager.Instance.player != null)
        {
            player = GameManager.Instance.player.transform;
        }
        else
        {
            
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }
    }

    // -------------------- VISION CHECK --------------------
    public bool CanSeePlayer()
    {
        UpdatePlayerReference();
        if (player == null)
        {
            wasSeeingPlayerLastFrame = false;
            return false;
        }

        
        if (player.TryGetComponent<PlayerController>(out var playerController) && playerController.IsDead)
        {
            wasSeeingPlayerLastFrame = false;
            return false;
        }

        Vector2 enemyPos = (Vector2)transform.position;
        Vector2 playerPos = (Vector2)player.position;
        Vector2 dir = playerPos - enemyPos;
        float dist = dir.magnitude;

        if (dist > visionRange)
        {
            wasSeeingPlayerLastFrame = false;
            return false;
        }

        Vector2 normalizedDir = dir / dist;

        RaycastHit2D hit = Physics2D.Raycast(enemyPos, normalizedDir, dist, obstacleLayer);

#if UNITY_EDITOR
        Color rayColor = hit.collider ? Color.red : Color.green;
        Debug.DrawRay(enemyPos, normalizedDir * dist, rayColor, 0.1f);
#endif

        bool canSee = !hit.collider || hit.collider.CompareTag("Player");

       
        wasSeeingPlayerLastFrame = canSee;

        return canSee;
    }

    // -------------------- HEARING / INVESTIGATION --------------------
    public void HandleGunshotHeard(Vector3 position)
    {
        if (sm.currentState == EnemyStateMachine.State.Dead) return;

        float dist = Vector3.Distance(transform.position, position);
        float chance = hearingResponseChance * (1 - Mathf.Clamp01(dist / 15f));

        if (Random.value <= chance)
        {
            enemy.movement.Investigate(position);
            sm.ChangeState(EnemyStateMachine.State.Inspect);
        }
    }
}