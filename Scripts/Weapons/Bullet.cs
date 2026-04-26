using UnityEngine;

public class Bullet : MonoBehaviour
{
    #region Settings

    [Header("Bullet Settings")]
    [SerializeField] private float _moveSpeed = 40f;
    [SerializeField] private float _maxLifetime = 3f;
    [SerializeField] private LayerMask _hitMask;
    [SerializeField] private float _spreadAngleDegrees = 5f;

    [Header("Visual Effects")]
    [SerializeField] private GameObject _impactEffectPrefab;

    #endregion

    #region State

    private Vector2 _travelDirection;
    private float _remainingLifetime;
    private bool _hasHitTarget;

    [HideInInspector] public bool IsFromEnemy = false;

    #endregion

    #region Initialization

    public void Initialize(Vector2 direction)
    {
        _travelDirection = ApplySpread(direction.normalized);
        _remainingLifetime = _maxLifetime;

        AlignRotationToDirection();
    }

    private Vector2 ApplySpread(Vector2 baseDirection)
    {
        float randomAngle = Random.Range(-_spreadAngleDegrees, _spreadAngleDegrees);
        return Quaternion.Euler(0f, 0f, randomAngle) * baseDirection;
    }

    private void AlignRotationToDirection()
    {
        float angle = Mathf.Atan2(_travelDirection.y, _travelDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle + 90f);
    }

    #endregion

    #region Update Loop

    private void Update()
    {
        if (HasExpired())
        {
            DestroySelf();
            return;
        }

        PerformMovementAndCollisionCheck();
        DecreaseLifetime();
    }

    private bool HasExpired() => _remainingLifetime <= 0f;

    private void DecreaseLifetime() => _remainingLifetime -= Time.deltaTime;

    #endregion

    #region Movement & Collision

    private void PerformMovementAndCollisionCheck()
    {
        float distance = _moveSpeed * Time.deltaTime;
        Vector2 origin = transform.position;

        if (DetectObstacleHit(origin, distance, out RaycastHit2D hit))
        {
            HandleEnvironmentHit(hit.point);
            return;
        }

        MoveForward(distance);
    }

    private bool DetectObstacleHit(Vector2 origin, float distance, out RaycastHit2D hit)
    {
        bool original = Physics2D.queriesHitTriggers;
        Physics2D.queriesHitTriggers = false;

        hit = Physics2D.Raycast(origin, _travelDirection, distance, _hitMask);

        Physics2D.queriesHitTriggers = original;

        return hit.collider != null;
    }

    private void MoveForward(float distance)
    {
        transform.position += (Vector3)(_travelDirection * distance);
    }

    private void HandleEnvironmentHit(Vector2 hitPoint)
    {
        SpawnImpactEffect(hitPoint);
        DestroySelf();
    }

    #endregion

    #region Trigger Collisions

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_hasHitTarget) return;

        if (IsPlayerBulletHittingEnemy(other))
        {
            HandleEnemyHit(other);
            return;
        }

        if (IsEnemyBulletHittingPlayer(other))
        {
            HandlePlayerHit(other);
            return;
        }
    }

    private bool IsPlayerBulletHittingEnemy(Collider2D other)
    {
        return !IsFromEnemy && other.CompareTag("Enemy");
    }

    private bool IsEnemyBulletHittingPlayer(Collider2D other)
    {
        return IsFromEnemy && other.CompareTag("Player");
    }

    private void HandleEnemyHit(Collider2D enemyCollider)
    {
        _hasHitTarget = true;

        if (enemyCollider.TryGetComponent<EnemyHealth>(out var enemyHealth))
        {
            enemyHealth.Die(_travelDirection);
        }

        SpawnImpactEffect(enemyCollider.bounds.center);
        DestroySelf();
    }

    private void HandlePlayerHit(Collider2D playerCollider)
    {
        _hasHitTarget = true;

        if (playerCollider.TryGetComponent<PlayerController>(out var player))
        {
            if (!player.IsDead)
                player.Die(_travelDirection);
        }

        SpawnImpactEffect(playerCollider.bounds.center);
        DestroySelf();
    }

    #endregion

    #region Effects & Cleanup

    private void SpawnImpactEffect(Vector2 position)
    {
        if (_impactEffectPrefab == null) return;

        GameObject effect = Instantiate(_impactEffectPrefab, position, Quaternion.identity);

        float destroyDelay = GetEffectDuration(effect);
        Destroy(effect, destroyDelay);
    }

    private float GetEffectDuration(GameObject effect)
    {
        Animator animator = effect.GetComponent<Animator>();

        if (animator == null)
            return 0.5f;

        return animator.GetCurrentAnimatorClipInfo(0)[0].clip.length;
    }

    private void DestroySelf()
    {
        Destroy(gameObject);
    }

    #endregion
}