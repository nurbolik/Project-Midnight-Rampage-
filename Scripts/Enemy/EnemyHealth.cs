using UnityEngine;
using System.Collections.Generic;

public class EnemyHealth : MonoBehaviour
{
    [Header("Death Sprites")]
    public List<Sprite> deathSprites;

    [Header("Blood Splatter")]
    [Tooltip("List of blood splatter sprites to spawn on death")]
    public List<Sprite> bloodSplatterSprites;

    [Tooltip("Offset range for blood splatter position randomness")]
    public float bloodSplatterOffsetRange = 0.3f;

    [Header("Object References")]
    [Tooltip("The child GameObject with SpriteRenderer that shows the death sprite")]
    public GameObject deathSpriteObject;

    [Tooltip("The child GameObject for legs - will be disabled on death")]
    public GameObject legsObject;

    [Tooltip("The main body sprite object - will be disabled on death")]
    public GameObject bodySpriteObject;

    [Header("Weapon Drop")]
    public Transform weaponDropPoint;

    private Enemy enemy;
    private bool isDead = false;
    private SpriteRenderer deathSpriteRenderer;

    // -------------------- INIT --------------------

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        SetupDeathSprite();
    }

    private void SetupDeathSprite()
    {
        if (deathSpriteObject == null) return;

        deathSpriteRenderer = deathSpriteObject.GetComponent<SpriteRenderer>();

        if (deathSpriteRenderer == null)
            deathSpriteRenderer = deathSpriteObject.AddComponent<SpriteRenderer>();

        deathSpriteObject.SetActive(false);
    }

    // -------------------- DEATH --------------------

    public void Die(Vector2 hitDirection)
    {
        if (isDead) return;
        isDead = true;

        enemy.stateMachine.ChangeState(EnemyStateMachine.State.Dead);

        DropWeaponAsPickup();
        StopMovement();
        DisableVisuals();
        SetupDeathVisual(hitDirection);
        SpawnBloodSplatter();
        DisableEnemyComponents();

        if (GameManager.Instance != null)
            GameManager.Instance.EnemyKilled();

        RemoveCollider();
		FindObjectOfType<SoundManager>().Play("Kill");
    }

    private void StopMovement()
    {
        enemy.movement.StopMoving();

        if (enemy.rb != null)
        {
            enemy.rb.linearVelocity = Vector2.zero;
            enemy.rb.angularVelocity = 0f;
        }
    }

    private void DisableVisuals()
    {
        if (bodySpriteObject != null)
            bodySpriteObject.SetActive(false);

        if (legsObject != null)
            legsObject.SetActive(false);
    }

    private void SetupDeathVisual(Vector2 hitDirection)
    {
        if (deathSpriteObject == null || deathSpriteRenderer == null || deathSprites.Count == 0)
            return;

        deathSpriteObject.transform.SetParent(null);
        deathSpriteObject.transform.position = transform.position;
        deathSpriteObject.SetActive(true);

        deathSpriteRenderer.sprite = deathSprites[Random.Range(0, deathSprites.Count)];

        float angle = Mathf.Atan2(hitDirection.y, hitDirection.x) * Mathf.Rad2Deg;
        float randomRotation = Random.Range(-15f, 15f);

        deathSpriteObject.transform.rotation =
            Quaternion.Euler(0, 0, angle - 90f + randomRotation);

        deathSpriteRenderer.sortingOrder = 1;
    }

    private void RemoveCollider()
    {
        if (TryGetComponent<Collider2D>(out var col))
            Destroy(col);
    }

    // -------------------- WEAPON DROP --------------------

    private void DropWeaponAsPickup()
    {
        if (enemy.combat.currentWeapon == WeaponType.NoWeapon)
            return;

        GameObject weaponPickupPrefab =
            WeaponDatabase.Instance?.GetWeaponPrefab(enemy.combat.currentWeapon);

        if (weaponPickupPrefab == null)
        {
            Debug.LogWarning($"[EnemyHealth] No prefab for {enemy.combat.currentWeapon}");
            return;
        }

        Vector2 dropPosition =
            weaponDropPoint != null ? weaponDropPoint.position : transform.position;

        dropPosition += Random.insideUnitCircle * 0.5f;

        GameObject droppedWeapon =
            Instantiate(weaponPickupPrefab, dropPosition, Quaternion.identity);

        if (droppedWeapon.TryGetComponent<WeaponPickup>(out var pickup))
        {
            pickup.SetAmmo(enemy.combat.currentAmmo);

            Vector2 throwDir = Random.insideUnitCircle.normalized;
            pickup.Throw(throwDir, 3f);
        }

        enemy.combat.currentWeapon = WeaponType.NoWeapon;
        enemy.combat.currentAmmo = 0;
    }

    // -------------------- BLOOD --------------------

    private void SpawnBloodSplatter()
    {
        if (bloodSplatterSprites == null || bloodSplatterSprites.Count == 0)
            return;

        GameObject bloodSplatter = new GameObject("BloodSplatter");

        Vector2 offset = Random.insideUnitCircle * bloodSplatterOffsetRange;
        bloodSplatter.transform.position = (Vector2)transform.position + offset;

        bloodSplatter.transform.rotation =
            Quaternion.Euler(0, 0, Random.Range(0f, 360f));

        SpriteRenderer bloodRenderer = bloodSplatter.AddComponent<SpriteRenderer>();

        bloodRenderer.sprite =
            bloodSplatterSprites[Random.Range(0, bloodSplatterSprites.Count)];

        bloodRenderer.sortingOrder = 1;
    }

    // -------------------- DISABLE --------------------

    private void DisableEnemyComponents()
    {
        if (enemy.stateMachine != null) enemy.stateMachine.enabled = false;
        if (enemy.perception != null) enemy.perception.enabled = false;
        if (enemy.movement != null) enemy.movement.enabled = false;
        if (enemy.combat != null) enemy.combat.enabled = false;
        if (enemy.animatorController != null) enemy.animatorController.enabled = false;

        if (enemy.agent != null) enemy.agent.enabled = false;

        if (enemy.rb != null)
            enemy.rb.bodyType = RigidbodyType2D.Static;

        DisableAnimators();
        DisableDebug();
    }

    private void DisableAnimators()
    {
        Animator[] animators = GetComponentsInChildren<Animator>();

        foreach (var anim in animators)
            anim.enabled = false;
    }

    private void DisableDebug()
    {
        if (TryGetComponent<EnemyDebug>(out var debug))
            debug.ToggleDebugDisplay();
    }
}