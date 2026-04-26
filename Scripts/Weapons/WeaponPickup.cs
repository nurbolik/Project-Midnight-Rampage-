using UnityEngine;
using System.Collections;

public class WeaponPickup : MonoBehaviour
{
    [Header("Weapon Info")]
    [SerializeField] private WeaponType _weaponType = WeaponType.Pistol;
    public WeaponType weaponType => _weaponType;

    [Header("Stats")]
    [SerializeField] private int _ammoCount = 30;
    public int ammoCount => _ammoCount;

    [Header("Physics")]
    [SerializeField] private Rigidbody2D _rigidBody;
    [SerializeField] private Collider2D _collider;
	private float _enableTriggerDelay = 0.7f;
	private float _linearDrag = 5f;
    private float _angularDrag = 3f;

    private EnemyCombat _reservedBy;
    private float _reservationTime;
    private const float RESERVATION_TIMEOUT = 10f;

    public bool IsReserved => _reservedBy != null && Time.time - _reservationTime < RESERVATION_TIMEOUT;
    public EnemyCombat ReservedBy => _reservedBy;

    private void Awake()
    {
        CacheComponents();
        SetupPhysics();
        gameObject.tag = "Weapon";
    }

    private void Start()
    {
        IgnorePlayerCollisionTemporarily();
    }

    private void Update()
    {
        HandleReservationTimeout();
    }

 

    private void CacheComponents()
    {
        if (_rigidBody == null) _rigidBody = GetComponent<Rigidbody2D>();
        if (_collider == null) _collider = GetComponent<Collider2D>();
    }

    private void SetupPhysics()
    {
        if (_rigidBody == null) return;

        _rigidBody.linearDamping = _linearDrag;
        _rigidBody.angularDamping = _angularDrag;
        _rigidBody.gravityScale = 0f;
    }

    private void IgnorePlayerCollisionTemporarily()
    {
        var player = FindFirstObjectByType<PlayerController>();
        if (player == null) return;

        Collider2D playerCol = player.GetComponent<Collider2D>();
        if (playerCol == null) return;

        Physics2D.IgnoreCollision(_collider, playerCol, true);
        StartCoroutine(ReenableCollisionAfterDelay(playerCol));
    }


    private void HandleReservationTimeout()
    {
        if (_reservedBy != null && Time.time - _reservationTime >= RESERVATION_TIMEOUT)
        {
            ClearReservation();
        }
    }

    public void ReserveFor(EnemyCombat enemy)
    {
        _reservedBy = enemy;
        _reservationTime = Time.time;
    }

    public void ClearReservation()
    {
        _reservedBy = null;
    }

    

    public void Throw(Vector2 direction, float force = 8f)
    {
        if (_rigidBody == null) return;

        EnablePhysicsForThrow();

        _rigidBody.linearVelocity = direction * force;
        _rigidBody.angularVelocity = Random.Range(360f, 1080f);

        StartCoroutine(WaitUntilStoppedThenBecomePickup());
    }

    private void EnablePhysicsForThrow()
    {
        _collider.isTrigger = false;
        _rigidBody.isKinematic = false;

        _rigidBody.linearDamping = _linearDrag;
        _rigidBody.angularDamping = _angularDrag;
    }

    private IEnumerator WaitUntilStoppedThenBecomePickup()
    {
        yield return new WaitUntil(() => _rigidBody.linearVelocity.magnitude < 0.3f);
        yield return new WaitForSeconds(0.1f);

        StopMovement();
        SetAsPickup();
    }

    private void StopMovement()
    {
        _rigidBody.linearVelocity = Vector2.zero;
        _rigidBody.angularVelocity = 0f;
    }

    private void SetAsPickup()
    {
        _rigidBody.isKinematic = true;
        _collider.isTrigger = true;
    }

    private IEnumerator ReenableCollisionAfterDelay(Collider2D playerCollider)
    {
        yield return new WaitForSeconds(_enableTriggerDelay);

        if (playerCollider != null && _collider != null)
        {
            Physics2D.IgnoreCollision(_collider, playerCollider, false);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!_collider.isTrigger)
        {
            StartCoroutine(WaitUntilStoppedThenBecomePickup());
        }
    }


    public void SetAmmo(int ammo) => _ammoCount = ammo;
    public void SetWeaponType(WeaponType type) => _weaponType = type;

   

    private void Reset()
    {
        gameObject.tag = "Weapon";

        if (TryGetComponent<Rigidbody2D>(out var rb)) _rigidBody = rb;
        if (TryGetComponent<Collider2D>(out var col)) _collider = col;
    }
}