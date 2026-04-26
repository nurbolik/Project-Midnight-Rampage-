using UnityEngine;
using System.Collections;

public class EnemyCombat : MonoBehaviour
{
    [Header("Current Weapon")]
    public WeaponType currentWeapon = WeaponType.NoWeapon;
    public int currentAmmo = 30;

    [HideInInspector] public bool isMeleeWeapon = false;
    [HideInInspector] public bool isAutomaticWeapon = false;

    [Header("Shooting")]
    public GameObject bulletPrefab;
    public Transform bulletSpawnPoint;
    public float bulletSpeed = 20f;

    [Header("Melee")]
    public float meleeRange = 0.5f;
    public float meleeDamage = 10f;
    public float meleeAttackCooldown = 1f;
    public float meleeAttackDuration = 0.5f;

    [Header("Weapon Pickup")]
    public float weaponCheckInterval = 2f;

    [Header("Last Known Position Settings")]
    public float lastKnownPositionUpdateThreshold = 2f;

    private Enemy enemy;
    private EnemyStateMachine sm => enemy.stateMachine;

    private float shootTimer;
    private float lastShotTime;
    private float lastMeleeAttackTime = -999f;
    private float weaponCheckTimer;

    private bool isPerformingMeleeAttack = false;

    private float _currentFireRate = 0.7f;

    private Vector2 lastKnownPlayerPos;
    private bool hasStoredLastKnownPosition = false;
    private bool wasInCombat = false;

    private WeaponType _lastInspectorWeapon = WeaponType.NoWeapon;
    private bool _hasInitializedWeapon = false;
	private SoundManager _soundManager;

    // -------------------- INIT --------------------

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
		
    }

    private void Start()
    {
		
        InitializeWeapon();
		_soundManager = SoundManager.instance;
    }

    private void InitializeWeapon()
    {
        if (_hasInitializedWeapon) return;

        ApplyWeaponStats(currentWeapon);

        if (enemy.animatorController != null)
        {
            enemy.animatorController.ChangeWeapon(currentWeapon);
            _hasInitializedWeapon = true;
        }

        _lastInspectorWeapon = currentWeapon;
    }

    // -------------------- UPDATE --------------------

    private void Update()
    {
        HandleInitializationFallback();
        HandleInspectorHotSwap();

        if (enemy.perception.player == null)
        {
            HandleNoPlayer();
            return;
        }

        bool canSeePlayer = enemy.perception.CanSeePlayer();
        Vector2 playerPos = enemy.perception.player.position;

        bool isInCombat = IsInCombatState();

        HandleWeaponSearch(isInCombat);
        UpdateLastKnownPosition(canSeePlayer, isInCombat, playerPos);

        if (HandleLostSight(canSeePlayer, isInCombat)) return;

        HandleEngagement(canSeePlayer);

        HandleCombat(canSeePlayer, playerPos);

        wasInCombat = isInCombat;
    }

    // -------------------- STATE HELPERS --------------------

    private void HandleInitializationFallback()
    {
        if (!_hasInitializedWeapon && enemy.animatorController != null)
            InitializeWeapon();
    }

    private void HandleInspectorHotSwap()
    {
        if (currentWeapon == _lastInspectorWeapon) return;

        _lastInspectorWeapon = currentWeapon;

        ApplyWeaponStats(currentWeapon);
        enemy.animatorController?.ChangeWeapon(currentWeapon);
    }

    private void HandleNoPlayer()
    {
        if (sm.currentState == EnemyStateMachine.State.Attack ||
            sm.currentState == EnemyStateMachine.State.Chase ||
            sm.currentState == EnemyStateMachine.State.Inspect)
        {
            hasStoredLastKnownPosition = false;
            wasInCombat = false;

            sm.ChangeState(EnemyStateMachine.State.Idle);
        }
    }

    private bool IsInCombatState()
    {
        return sm.currentState == EnemyStateMachine.State.Attack ||
               sm.currentState == EnemyStateMachine.State.Chase;
    }

    // -------------------- PERCEPTION --------------------

    private void HandleWeaponSearch(bool isInCombat)
    {
        if (isInCombat || currentWeapon != WeaponType.NoWeapon) return;

        weaponCheckTimer += Time.deltaTime;

        if (weaponCheckTimer >= weaponCheckInterval)
        {
            weaponCheckTimer = 0f;
            TryPickupWeapon();
        }
    }

    private void UpdateLastKnownPosition(bool canSeePlayer, bool isInCombat, Vector2 playerPos)
    {
        if (!canSeePlayer || !isInCombat) return;

        if (!hasStoredLastKnownPosition || !wasInCombat)
        {
            lastKnownPlayerPos = playerPos;
            hasStoredLastKnownPosition = true;
        }
        else if (Vector2.Distance(lastKnownPlayerPos, playerPos) >= lastKnownPositionUpdateThreshold)
        {
            lastKnownPlayerPos = playerPos;
        }
    }

    private bool HandleLostSight(bool canSeePlayer, bool isInCombat)
    {
        if (canSeePlayer || !hasStoredLastKnownPosition || !isInCombat)
            return false;

        sm.ChangeState(EnemyStateMachine.State.Inspect);
        enemy.movement.GoToLastKnownPosition(lastKnownPlayerPos);

        hasStoredLastKnownPosition = false;
        wasInCombat = false;

        return true;
    }

    // -------------------- COMBAT LOGIC --------------------

    private void HandleEngagement(bool canSeePlayer)
    {
        if (!canSeePlayer) return;

        if (currentWeapon != WeaponType.NoWeapon && !isMeleeWeapon)
            sm.ChangeState(EnemyStateMachine.State.Attack);
        else
            sm.ChangeState(EnemyStateMachine.State.Chase);
    }

    private void HandleCombat(bool canSeePlayer, Vector2 playerPos)
    {
        if (!canSeePlayer) return;

        if (sm.currentState == EnemyStateMachine.State.Attack)
            HandleRangedAttack();
        else if (sm.currentState == EnemyStateMachine.State.Chase)
            HandleMeleeChase(playerPos);
    }

    // -------------------- ATTACKS --------------------

    private void HandleRangedAttack()
    {
        shootTimer += Time.deltaTime;

        if (shootTimer < _currentFireRate ||
            Time.time < lastShotTime + _currentFireRate)
            return;

        Shoot();

        shootTimer = 0f;
        lastShotTime = Time.time;
    }

    private void HandleMeleeChase(Vector2 playerPos)
    {
        float dist = Vector2.Distance(enemy.rb.position, playerPos);

        if (dist > meleeRange)
        {
            enemy.movement.Investigate(playerPos);
            return;
        }

        enemy.movement.StopMoving();

        if (!isPerformingMeleeAttack &&
            Time.time >= lastMeleeAttackTime + meleeAttackCooldown)
        {
            StartCoroutine(PerformMeleeAttack());
        }
    }

    private IEnumerator PerformMeleeAttack()
    {
        isPerformingMeleeAttack = true;
        lastMeleeAttackTime = Time.time;

        enemy.animatorController?.TriggerAttack();
		FindObjectOfType<SoundManager>().Play("Melee");

        yield return new WaitForSeconds(meleeAttackDuration);

        if (enemy.perception.player != null &&
            Vector2.Distance(enemy.rb.position, enemy.perception.player.position) <= meleeRange)
        {
            if (enemy.perception.player.TryGetComponent(out PlayerController player))
                player.TakeDamage(meleeDamage);
        }

        isPerformingMeleeAttack = false;
    }

    private void Shoot()
    {
		PlayWeaponSound();
        if (bulletPrefab == null || bulletSpawnPoint == null) return;

        if (currentAmmo <= 0)
        {
            Debug.Log("[EnemyCombat] Out of ammo!");
            return;
        }

        Vector2 shootPos = bulletSpawnPoint.position;
        Vector2 playerPos = enemy.perception.player.position;

        Rigidbody2D rb = enemy.perception.player.GetComponent<Rigidbody2D>();
        Vector2 velocity = rb != null ? rb.linearVelocity : Vector2.zero;

        float distance = Vector2.Distance(shootPos, playerPos);
        float timeToImpact = distance / bulletSpeed;

        Vector2 dir = (playerPos + velocity * timeToImpact - shootPos).normalized;

        GameObject bullet = Instantiate(bulletPrefab, shootPos, Quaternion.identity);

        if (bullet.TryGetComponent(out Bullet b))
        {
            b.IsFromEnemy = true;
            b.Initialize(dir);
        }
		

        currentAmmo--;

        enemy.animatorController?.TriggerAttack();

        if (currentAmmo <= 0)
        {
            currentWeapon = WeaponType.NoWeapon;
            ApplyWeaponStats(currentWeapon);
            enemy.animatorController?.ChangeWeapon(currentWeapon);
        }
    }

    // -------------------- WEAPONS --------------------

    public void TryPickupWeapon()
    {
        if (currentWeapon != WeaponType.NoWeapon) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            enemy.perception.weaponDetectionRadius,
            enemy.perception.weaponLayer
        );

        WeaponPickup closest = null;
        float closestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent(out WeaponPickup pickup)) continue;
            if (pickup.IsReserved && pickup.ReservedBy != this) continue;

            Vector2 dir = hit.transform.position - transform.position;
            float dist = dir.magnitude;

            if (Physics2D.Raycast(transform.position, dir.normalized, dist, enemy.perception.obstacleLayer))
                continue;

            if (dist < closestDist)
            {
                closestDist = dist;
                closest = pickup;
            }
        }

        if (closest == null) return;

        closest.ReserveFor(this);
        enemy.movement.Investigate(closest.transform.position);
        sm.ChangeState(EnemyStateMachine.State.Inspect);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Weapon") || currentWeapon != WeaponType.NoWeapon)
            return;

        if (other.TryGetComponent(out WeaponPickup pickup))
            EquipWeapon(pickup);
    }

    private void EquipWeapon(WeaponPickup pickup)
    {
        currentWeapon = pickup.weaponType;
        currentAmmo = pickup.ammoCount;

        ApplyWeaponStats(currentWeapon);

        enemy.animatorController?.ChangeWeapon(currentWeapon);

        pickup.ClearReservation();
        Destroy(pickup.gameObject);
    }

    private void ApplyWeaponStats(WeaponType weapon)
    {
        if (weapon == WeaponType.NoWeapon)
        {
            isMeleeWeapon = false;
            isAutomaticWeapon = false;
            _currentFireRate = 0f;
            return;
        }

        if (WeaponDatabase.Instance != null &&
            WeaponDatabase.Instance.TryGetWeaponEntry(weapon, out var data))
        {
            isMeleeWeapon = data.isMelee;
            isAutomaticWeapon = data.isAutomatic;
            _currentFireRate = data.fireRate;
        }
        else
        {
            isMeleeWeapon = false;
            isAutomaticWeapon = false;
            _currentFireRate = 0.2f;
        }
    }
	
	// -------------------- SOUNDS --------------------
	private void PlayWeaponSound()
{
    if (_soundManager == null) return;

    switch (currentWeapon)
    {
        case WeaponType.Pistol:   _soundManager.Play("Pistol");  break;
        case WeaponType.Shotgun:  _soundManager.Play("Shotgun"); break;
        case WeaponType.Rifle:    _soundManager.Play("Rifle");   break;
        case WeaponType.Uzi:      _soundManager.Play("Uzi");     break;
        case WeaponType.Ballbat:  _soundManager.Play("Bat");     break;
        case WeaponType.Knife:    _soundManager.Play("Knife");   break;
        default: break; 
    }
}
private SoundManager SoundManagerInstance
{
    get
    {
        if (_soundManager == null)
            _soundManager = SoundManager.instance;
        return _soundManager;
    }
}
}