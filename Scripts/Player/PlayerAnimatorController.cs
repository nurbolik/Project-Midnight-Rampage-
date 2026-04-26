using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimatorController : MonoBehaviour
{
    #region Inspector Fields

    [Header("Animator References")]
    [SerializeField] private Animator _feetAnimator;
    [SerializeField] private List<AnimatorOverrideController> _weaponOverrideControllers;
    [SerializeField] private RuntimeAnimatorController _baseAnimatorController; // Player_NoWeapon

    #endregion

    #region Private Fields

    private Animator _animator;
    private Dictionary<WeaponType, AnimatorOverrideController> _overrideControllerDictionary;
    private WeaponType _currentWeaponType = WeaponType.NoWeapon;

    
    private static readonly int WeaponTypeHash = Animator.StringToHash("WeaponType");
    private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");

    
    private bool _animatorHasIsMoving;
    private bool _feetAnimatorHasIsWalking;

    #endregion

    #region Unity Callbacks

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        InitializeOverrideControllers();

        
        _animatorHasIsMoving = HasParameter(_animator, "IsMoving");
        _feetAnimatorHasIsWalking = _feetAnimator != null && HasParameter(_feetAnimator, "IsWalking");
    }

    #endregion

    #region Weapon Handling

    private void InitializeOverrideControllers()
    {
        _overrideControllerDictionary = new Dictionary<WeaponType, AnimatorOverrideController>();

        foreach (var overrideController in _weaponOverrideControllers)
        {
            WeaponType weaponType = DetermineWeaponTypeFromOverrideController(overrideController);
            if (!_overrideControllerDictionary.ContainsKey(weaponType))
                _overrideControllerDictionary[weaponType] = overrideController;
        }
    }

    private WeaponType DetermineWeaponTypeFromOverrideController(AnimatorOverrideController controller)
    {
        string controllerName = controller.name;

        foreach (WeaponType weaponType in Enum.GetValues(typeof(WeaponType)))
        {
            if (controllerName.Contains(weaponType.ToString()))
                return weaponType;
        }

        Debug.LogWarning($"Could not determine weapon type for controller: {controllerName}");
        return WeaponType.NoWeapon;
    }

    public void ChangeWeapon(WeaponType newWeaponType)
    {
        if (_currentWeaponType == newWeaponType) return;

        _currentWeaponType = newWeaponType;
        UpdateAnimatorController();
    }

    private void UpdateAnimatorController()
    {
        if (_currentWeaponType == WeaponType.NoWeapon)
        {
            _animator.runtimeAnimatorController = _baseAnimatorController;
        }
        else if (_overrideControllerDictionary.TryGetValue(_currentWeaponType, out var overrideController))
        {
            _animator.runtimeAnimatorController = overrideController;
        }
        else
        {
            Debug.LogError($"Animator override controller not found for weapon type: {_currentWeaponType}");
            return;
        }

        _animator.SetInteger(WeaponTypeHash, (int)_currentWeaponType);
    }

    #endregion

    #region Movement Handling

    public void SetMoveSpeed(float speed)
    {
        bool isMoving = speed > 0.01f;

        if (_animatorHasIsMoving)
            _animator.SetBool("IsMoving", isMoving);

        if (_feetAnimatorHasIsWalking)
            _feetAnimator.SetBool("IsWalking", isMoving);
    }

    private bool HasParameter(Animator animator, string paramName)
    {
        foreach (var param in animator.parameters)
        {
            if (param.name == paramName)
                return true;
        }

        return false;
    }

    #endregion

    #region Combat Handling

    public void TriggerAttack()
    {
        _animator.SetTrigger(AttackTriggerHash);
    }

    public void StopAttack()
    {
        _animator.ResetTrigger(AttackTriggerHash);
    }

    #endregion
}
