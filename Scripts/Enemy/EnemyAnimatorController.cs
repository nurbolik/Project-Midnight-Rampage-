using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteLibrary))]
public class EnemyAnimatorController : MonoBehaviour
{
    [Header("Animators")]
    [SerializeField] private Animator _animator;
    [SerializeField] private Animator _feetAnimator;

    [Header("Sprite Library (root)")]
    [SerializeField] private SpriteLibrary _spriteLibrary;

    [Header("Weapon -> SpriteLibraryAsset mapping")]
    [SerializeField] private List<WeaponSpriteLibrary> _weaponSpriteLibraries = new();

    private Dictionary<WeaponType, SpriteLibraryAsset> _weaponLibraryDictionary;
    private WeaponType _currentWeaponType = WeaponType.NoWeapon;

    private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");

    private bool _animatorHasIsWalking;
    private bool _feetAnimatorHasIsWalking;

    private void Awake()
    {
        InitializeReferences();
        InitializeWeaponLibraries();
        CacheAnimatorParameters();
    }

    // -------------------- Initialization --------------------

    private void InitializeReferences()
    {
        if (_animator == null)
            _animator = GetComponent<Animator>();

        if (_spriteLibrary == null)
            _spriteLibrary = GetComponent<SpriteLibrary>();
    }

    private void InitializeWeaponLibraries()
    {
        _weaponLibraryDictionary = new Dictionary<WeaponType, SpriteLibraryAsset>();

        foreach (var entry in _weaponSpriteLibraries)
        {
            if (entry == null || entry.libraryAsset == null) continue;
            _weaponLibraryDictionary[entry.weaponType] = entry.libraryAsset;
        }
    }

    private void CacheAnimatorParameters()
    {
        _animatorHasIsWalking = HasParameter(_animator, "IsWalking");
        _feetAnimatorHasIsWalking = _feetAnimator != null && HasParameter(_feetAnimator, "IsWalking");
    }

    // -------------------- Weapon Handling --------------------

    public void ChangeWeapon(WeaponType newWeaponType)
    {
        EnsureWeaponLibraryInitialized();

        if (_currentWeaponType == newWeaponType) return;
        _currentWeaponType = newWeaponType;

        if (_weaponLibraryDictionary.TryGetValue(newWeaponType, out var libAsset))
        {
            ApplySpriteLibrary(libAsset);
        }
        else
        {
            Debug.LogWarning($"[EnemyAnimator] No SpriteLibraryAsset for {newWeaponType}");
        }
    }

    private void EnsureWeaponLibraryInitialized()
    {
        if (_weaponLibraryDictionary == null || _weaponLibraryDictionary.Count == 0)
        {
            InitializeWeaponLibraries();
        }
    }

    private void ApplySpriteLibrary(SpriteLibraryAsset libAsset)
    {
        if (_spriteLibrary == null)
            _spriteLibrary = GetComponent<SpriteLibrary>();

        _spriteLibrary.spriteLibraryAsset = libAsset;

        RefreshAnimator();
    }

    private void RefreshAnimator()
    {
        if (_animator == null) return;

        _animator.Rebind();
        _animator.Update(0f);
    }

    // -------------------- Movement --------------------

    public void SetMoveSpeed(float speed)
    {
        bool isMoving = speed > 0.01f;

        if (_animatorHasIsWalking)
            _animator.SetBool(IsWalkingHash, isMoving);
        else
            _animator.SetBool(IsMovingHash, isMoving);

        if (_feetAnimatorHasIsWalking)
            _feetAnimator.SetBool(IsWalkingHash, isMoving);
    }

    // -------------------- Combat --------------------

    public void TriggerAttack()
    {
        if (_animator == null) return;

        _animator.ResetTrigger(AttackTriggerHash);
        _animator.SetTrigger(AttackTriggerHash);
    }

    public void StopAttack()
    {
        if (_animator == null) return;

        _animator.ResetTrigger(AttackTriggerHash);
    }

    // -------------------- Utilities --------------------

    private bool HasParameter(Animator animator, string paramName)
    {
        if (animator == null) return false;

        foreach (var p in animator.parameters)
        {
            if (p.name == paramName)
                return true;
        }

        return false;
    }
}

[Serializable]
public class WeaponSpriteLibrary
{
    public WeaponType weaponType;
    public SpriteLibraryAsset libraryAsset;
}