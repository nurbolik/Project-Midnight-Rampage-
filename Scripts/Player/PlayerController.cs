using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(PlayerAnimatorController))]
public class PlayerController : MonoBehaviour
{
    #region Inspector Fields

    [Header("Movement")]
    [SerializeField] private float _maxSpeed = 5f;

    [Header("Melee Combat")]
    [SerializeField] private float _meleeRange = 1.5f;
    [SerializeField] private float _meleeAttackDuration = 0.3f;
    [SerializeField] private float _meleeCooldown = 0.4f;
    [SerializeField] private LayerMask _enemyLayer;
    [Tooltip("Angle spread for melee attack detection (degrees)")]
    [SerializeField] private float _meleeArcAngle = 90f;

    [Header("Death Sprites")]
    [SerializeField] private List<Sprite> deathSprites;

    [Header("Object References")]
    [Tooltip("Child GameObject showing death sprite")]
    [SerializeField] private GameObject deathSpriteObject;
    [Tooltip("Child GameObject for legs (disabled on death)")]
    [SerializeField] private GameObject legsObject;
    [Tooltip("Main body sprite object (disabled on death)")]
    [SerializeField] private GameObject bodySpriteObject;


    [Header("References")]
    [SerializeField] private PlayerAnimatorController _animatorController;

    #endregion

    #region Private Fields

    private Vector2 _moveInput;
    private Rigidbody2D _rigidbody;
    private WeaponManager _weaponManager;
    private Coroutine _shootCoroutine;
    private bool _isDead = false;
    private bool _canMove = true;
    private SpriteRenderer _deathSpriteRenderer;

    // Melee
    private bool _isPerformingMeleeAttack = false;
    private float _lastMeleeAttackTime = -999f;

    #endregion

    #region Public Properties

    public bool IsDead => _isDead;
    public bool IsMoving => _moveInput.magnitude > 0.01f;

    #endregion

    #region Unity Callbacks

    public void Respawn(Vector3 position)
    {
        _isDead = false;
        _canMove = true;
        _moveInput = Vector2.zero;

        transform.position = position;

        if (_rigidbody != null)
            _rigidbody.linearVelocity = Vector2.zero;

        bodySpriteObject?.SetActive(true);
        legsObject?.SetActive(true);

        if (deathSpriteObject != null)
            deathSpriteObject.SetActive(false);
        
        if (TryGetComponent<Collider2D>(out var collider))
            collider.enabled = true;

        if (CameraController.Instance != null)
        {
            CameraController.Instance._target = transform;
            CameraController.Instance.SetOrbitRotation(false);
        }
        foreach (var enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            if (enemy.perception != null)
                enemy.perception.player = transform;
        }

        _animatorController?.SetMoveSpeed(0);
    }

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _weaponManager = GetComponent<WeaponManager>();
        if (_animatorController == null)
            _animatorController = GetComponent<PlayerAnimatorController>();

        SetupDeathSprite();
    }

   
    private void FixedUpdate()
    {
        if (_isDead) return;
        Move();
        UpdateAnimation();
    }

    #endregion


    #region Movement

    private void Move()
    {
        if (!_canMove) return;
        _rigidbody.linearVelocity = _moveInput * _maxSpeed;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (_isDead || !_canMove) return;
        _moveInput = context.ReadValue<Vector2>();
    }

    private void UpdateAnimation()
    {
        if (_isDead || _animatorController == null) return;
        _animatorController.SetMoveSpeed(_rigidbody.linearVelocity.magnitude);
    }

    #endregion

    #region Attack

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (_isDead || !_canMove) return;

        if (_weaponManager.IsCurrentWeaponMelee())
        {
            if (context.performed && !_isPerformingMeleeAttack && Time.time >= _lastMeleeAttackTime + _meleeCooldown)
                StartCoroutine(PerformMeleeAttack());
        }
        else
        {
            HandleRangedAttack(context);
        }
    }

    private void HandleRangedAttack(InputAction.CallbackContext context)
    {
        if (_weaponManager.IsCurrentWeaponAutomatic())
        {
            if (context.started && _shootCoroutine == null)
                _shootCoroutine = StartCoroutine(ShootLoop());
            else if (context.canceled && _shootCoroutine != null)
                StopShootingCoroutine();
        }
        else
        {
            if (context.performed && _weaponManager.GetCurrentAmmo() > 0)
            {
                _animatorController.TriggerAttack();
                _weaponManager.Shoot();
                _weaponManager.UpdateAmmoUI();
            }
        }
    }

    private IEnumerator PerformMeleeAttack()
    {
        _isPerformingMeleeAttack = true;
        _lastMeleeAttackTime = Time.time;

        _animatorController.TriggerAttack();

        yield return new WaitForSeconds(_meleeAttackDuration * 0.3f);

        PerformMeleeHit();

        if (CameraController.Instance != null)
            CameraController.Instance.TriggerShake(0.2f, 0.3f);

        yield return new WaitForSeconds(_meleeAttackDuration * 0.7f);

        _isPerformingMeleeAttack = false;
    }

    private void PerformMeleeHit()
    {
        Vector2 playerPos = transform.position;
        Vector2 playerDir = transform.right;

        Collider2D[] hits = Physics2D.OverlapCircleAll(playerPos, _meleeRange, _enemyLayer);

        EnemyHealth closestEnemy = null;
        float closestDistance = float.MaxValue;
        Vector2 closestDirection = Vector2.zero;
		FindObjectOfType<SoundManager>().Play("Melee");
        foreach (Collider2D hit in hits)
        {
            Vector2 dirToEnemy = (hit.transform.position - transform.position).normalized;
            float angle = Vector2.Angle(playerDir, dirToEnemy);

            if (angle <= _meleeArcAngle / 2f && hit.TryGetComponent(out EnemyHealth enemyHealth))
            {
                float distance = Vector2.Distance(playerPos, hit.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemy = enemyHealth;
                    closestDirection = dirToEnemy;
                }
            }
        }

        if (closestEnemy != null)
            closestEnemy.Die(closestDirection);
    }

    private IEnumerator ShootLoop()
    {
        while (_weaponManager.GetCurrentAmmo() > 0 && !_isDead && _canMove)
        {
            _animatorController.TriggerAttack();
            _weaponManager.Shoot();
            _weaponManager.UpdateAmmoUI();
            yield return new WaitForSeconds(_weaponManager.GetFireRate());
        }

        _shootCoroutine = null;
    }

    private void StopShootingCoroutine()
    {
        if (_shootCoroutine != null)
        {
            StopCoroutine(_shootCoroutine);
            _shootCoroutine = null;
        }
    }

    #endregion

    #region Weapon

    public void OnDrop(InputAction.CallbackContext context)
    {
        if (_isDead || !_canMove || !context.performed) return;

        if (_weaponManager.GetCurrentWeaponType() != WeaponType.NoWeapon)
            _weaponManager.DropWeapon();
        else
            _weaponManager.TryPickUpWeapon();
    }

    public void ChangeWeapon(WeaponType newWeaponType)
    {
        if (_isDead) return;
        _animatorController?.ChangeWeapon(newWeaponType);
    }

    #endregion

    #region Damage & Death

    public void TakeDamage(float damage)
    {
        if (_isDead) return;

        Debug.Log("Player hit!");
        Die(Vector2.zero);
    }

    public void Die(Vector2 hitDirection)
    {
		FindObjectOfType<SoundManager>().Play("Kill");
        if (_isDead) return;
        _isDead = true;

        _rigidbody.linearVelocity = Vector2.zero;
        _animatorController?.SetMoveSpeed(0);
        StopAllAttackCoroutines();

        bodySpriteObject?.SetActive(false);
        legsObject?.SetActive(false);
		

        ShowDeathSprite(hitDirection);
		

        if (TryGetComponent<Collider2D>(out var collider))
            collider.enabled = false;

        GameManager.Instance?.PlayerDied();

        foreach (var enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            if (enemy.perception != null)
                enemy.perception.player = null;
        }
		
    }

    private void ShowDeathSprite(Vector2 hitDirection)
    {
        if (deathSpriteObject == null || _deathSpriteRenderer == null || deathSprites.Count == 0) return;

        deathSpriteObject.transform.SetParent(null);
        deathSpriteObject.transform.position = transform.position;
        deathSpriteObject.SetActive(true);

        _deathSpriteRenderer.sprite = deathSprites[Random.Range(0, deathSprites.Count)];

        float angle = Mathf.Atan2(hitDirection.y, hitDirection.x) * Mathf.Rad2Deg;
        float randomRotation = Random.Range(-15f, 15f);
        deathSpriteObject.transform.rotation = Quaternion.Euler(0, 0, angle - 90f + randomRotation);
        _deathSpriteRenderer.sortingOrder = 1;

        if (CameraController.Instance != null)
        {
            CameraController.Instance._target = deathSpriteObject.transform;
            CameraController.Instance.SetOrbitRotation(true);
        }
    }

    #endregion

    #region Utilities

    public void ToggleMovement(bool canMove)
    {
        _canMove = canMove;

        if (!canMove)
        {
            _moveInput = Vector2.zero;
            _rigidbody.linearVelocity = Vector2.zero;
            _animatorController?.SetMoveSpeed(0);
            StopAllAttackCoroutines();
            _animatorController?.StopAttack();
        }
    }

    private void StopAllAttackCoroutines()
    {
        _isPerformingMeleeAttack = false;
        StopShootingCoroutine();
    }

    private void SetupDeathSprite()
    {
        if (deathSpriteObject == null) return;

        _deathSpriteRenderer = deathSpriteObject.GetComponent<SpriteRenderer>();
        if (_deathSpriteRenderer == null)
            _deathSpriteRenderer = deathSpriteObject.AddComponent<SpriteRenderer>();

        deathSpriteObject.SetActive(false);
    }

    #endregion
}