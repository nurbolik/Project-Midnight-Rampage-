using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem; 

public class WeaponManager : MonoBehaviour
{
    [SerializeField] private float _pickupRadius = 1f;
    [SerializeField] private float _throwForce = 10f;
    [SerializeField] private PlayerAnimatorController _animatorController;
    [SerializeField] private PlayerController _playerController;
    [SerializeField] private TextMeshProUGUI _ammoText;

    [Header("Weapon Spawn Offsets (Scope Only)")]
    [SerializeField] private List<WeaponSpawnOffset> _weaponOffsets;

    [Header("Bullet Settings")]
    [SerializeField] private GameObject _bulletPrefab;
    [SerializeField] private Transform _bulletSpawnBase;
    [SerializeField] private UnityEngine.Rendering.Universal.Light2D _muzzleFlashLight;
    [SerializeField] private float _flashDuration = 0.05f;
    [SerializeField] private int _shotgunPelletCount = 5;
    [SerializeField] private float _spreadAngle = 15f;

    [Header("Sound Detection")]
    [SerializeField] private float gunNoiseRadius = 15f;

    private WeaponType _currentWeaponType = WeaponType.NoWeapon;
    public int _currentAmmo = 0;
    private bool _isCurrentWeaponMelee = false;
    private bool _isAutomatic = false;
    private float _currentFireRate = 0.2f;

    private bool _hasPendingShot = false;
    private Vector2 _pendingShootDirection;
	private SoundManager _soundManager;
    private Dictionary<WeaponType, Vector2> _offsetDict;

    [System.Serializable]
    public class WeaponSpawnOffset
    {
        public WeaponType type;
        [Tooltip("Where the bullet spawns relative to the weapon")]
        public Vector2 bulletSpawnOffset;
    }

    private void Awake()
    {
        if (_animatorController == null) _animatorController = GetComponent<PlayerAnimatorController>();
        if (_playerController == null) _playerController = GetComponent<PlayerController>();

		_soundManager = SoundManager.instance;

        InitializeOffsetDictionary();
        UpdateAmmoUI();
    }

    private void InitializeOffsetDictionary()
    {
        _offsetDict = new Dictionary<WeaponType, Vector2>();
        foreach (var offset in _weaponOffsets)
        {
            if (!_offsetDict.ContainsKey(offset.type))
                _offsetDict[offset.type] = offset.bulletSpawnOffset;
        }
    }

    private void FixedUpdate()
    {
        if (_hasPendingShot)
        {
            _hasPendingShot = false;
            if (_currentAmmo <= 0 || _currentWeaponType == WeaponType.NoWeapon || _isCurrentWeaponMelee)
                return;

            if (_currentWeaponType == WeaponType.Shotgun)
            {
                float angleStep = _spreadAngle / (_shotgunPelletCount - 1);
                float start = -_spreadAngle / 2f;
                for (int i = 0; i < _shotgunPelletCount; i++)
                {
                    float angle = start + i * angleStep;
                    Vector2 dir = Quaternion.Euler(0, 0, angle) * _pendingShootDirection;
                    FireBullet(dir);
                }
            }
            else
            {
                FireBullet(_pendingShootDirection);
            }

            _currentAmmo--;
            UpdateAmmoUI();

            if (_muzzleFlashLight != null)
                StartCoroutine(FlashMuzzleLight());
        }
    }

    public void TryPickUpWeapon()
    {
        foreach (Collider2D col in Physics2D.OverlapCircleAll(transform.position, _pickupRadius))
        {
            if (col.TryGetComponent<WeaponPickup>(out var pickup))
            {
                PickUpWeapon(pickup);
                return;
            }
        }
    }

    private void PickUpWeapon(WeaponPickup pickup)
    {
        _currentWeaponType = pickup.weaponType;
        _currentAmmo = pickup.ammoCount;

        if (WeaponDatabase.Instance != null &&
            WeaponDatabase.Instance.TryGetWeaponEntry(_currentWeaponType, out WeaponDatabase.WeaponEntry entry))
        {
            _currentFireRate = entry.fireRate;
            _isAutomatic = entry.isAutomatic;
            _isCurrentWeaponMelee = entry.isMelee;
        }
        else
        {
            _currentFireRate = 0.2f;
            _isAutomatic = false;
            _isCurrentWeaponMelee = false;
        }

        UpdateBulletSpawnOffset();

        _animatorController.ChangeWeapon(_currentWeaponType);
        UpdateAmmoUI();
        Destroy(pickup.gameObject);
		
		FindObjectOfType<SoundManager>().Play("Pickup");
    }

    private void UpdateBulletSpawnOffset()
    {
        if (_bulletSpawnBase == null) return;

        if (_isCurrentWeaponMelee)
        {
            _bulletSpawnBase.localPosition = Vector2.zero;
        }
        else if (_offsetDict.TryGetValue(_currentWeaponType, out Vector2 offset))
        {
            _bulletSpawnBase.localPosition = offset;
        }
        else
        {
            _bulletSpawnBase.localPosition = Vector2.zero;
        }
    }

    public void DropWeapon()
    {
        if (_currentWeaponType == WeaponType.NoWeapon) return;

        GameObject weaponPrefab = WeaponDatabase.Instance.GetWeaponPrefab(_currentWeaponType);

        if (weaponPrefab != null)
        {
            Vector2 dir = GetMouseDirection();
            Vector2 pos = (Vector2)transform.position + dir;
            GameObject dropped = Instantiate(weaponPrefab, pos, Quaternion.identity);

            if (dropped.TryGetComponent<WeaponPickup>(out var pickup))
            {
                pickup.SetAmmo(_currentAmmo);
                pickup.Throw(dir, _throwForce);
            }
        }

        _currentWeaponType = WeaponType.NoWeapon;
        _currentAmmo = 0;
        _isCurrentWeaponMelee = false;

        if (_bulletSpawnBase != null)
            _bulletSpawnBase.localPosition = Vector2.zero;

        _animatorController.ChangeWeapon(WeaponType.NoWeapon);
        UpdateAmmoUI();
		FindObjectOfType<SoundManager>().Play("Drop");
    }

    public void Shoot()
    {
        if (_currentAmmo <= 0 || _currentWeaponType == WeaponType.NoWeapon || _isCurrentWeaponMelee) return;

        _pendingShootDirection = GetMouseDirection();
        _hasPendingShot = true;
    }

    private void FireBullet(Vector2 direction)
    {
        Vector3 pos = _bulletSpawnBase.position;
        GameObject bullet = Instantiate(_bulletPrefab, pos, Quaternion.identity);
        bullet.GetComponent<Bullet>()?.Initialize(direction);

		PlayWeaponSound();

        CheckForNearbyEnemies();
        CameraController.Instance?.TriggerShake(0.5f, 0.5f);
    }
	
	private void PlayWeaponSound()
{
    var sm = FindObjectOfType<SoundManager>();
    if (sm == null) return;

    switch (_currentWeaponType)
    {
        case WeaponType.Pistol:    sm.Play("Pistol");   break;
        case WeaponType.Shotgun:   sm.Play("Shotgun");  break;
        case WeaponType.Rifle:     sm.Play("Rifle");    break;
        case WeaponType.Uzi:     sm.Play("Uzi");    break;
        default:                   sm.Play("Pistol");   break;
    }
}

    void CheckForNearbyEnemies()
    {
        foreach (Collider2D col in Physics2D.OverlapCircleAll(transform.position, gunNoiseRadius))
        {
            if (col.CompareTag("Enemy") && col.TryGetComponent<EnemyPerception>(out var perception))
            {
                perception.HandleGunshotHeard(transform.position);
            }
        }
    }

    private Vector2 GetMouseDirection()
    {
        if (Mouse.current == null) return Vector2.zero;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector2 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
        return (worldPos - (Vector2)transform.position).normalized;
    }

    private IEnumerator FlashMuzzleLight()
    {
        _muzzleFlashLight.enabled = true;
        yield return new WaitForSeconds(_flashDuration);
        _muzzleFlashLight.enabled = false;
    }

    public void UpdateAmmoUI()
    {
        if (_ammoText == null) return;

        if (_currentWeaponType == WeaponType.NoWeapon || _isCurrentWeaponMelee)
            _ammoText.gameObject.SetActive(false);
        else
        {
            _ammoText.gameObject.SetActive(true);
            _ammoText.text = _currentAmmo.ToString();
        }
    }

    // Getters
    public bool IsCurrentWeaponMelee() => _isCurrentWeaponMelee;
    public WeaponType GetCurrentWeaponType() => _currentWeaponType;
    public int GetCurrentAmmo() => _currentAmmo;
    public bool IsCurrentWeaponAutomatic() => _isAutomatic;
    public float GetFireRate() => _currentFireRate;
}