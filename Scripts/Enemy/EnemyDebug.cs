using UnityEngine;
using TMPro;

public class EnemyDebug : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private Vector3 offset = new Vector3(0, 1.5f, 0);
    [SerializeField] private float updateInterval = 0.1f;

    private Enemy enemy;
    private TextMeshPro debugText;
    private GameObject debugTextObject;
    private float updateTimer;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        CreateDebugText();
    }

    private void Update()
    {
        if (!showDebugInfo || debugText == null) return;

        HandleUpdateTimer();
    }

    // -------------------- Setup --------------------

    private void CreateDebugText()
    {
        debugTextObject = new GameObject("DebugText");
        debugTextObject.transform.SetParent(null);
        debugTextObject.transform.position = transform.position + offset;

        debugText = debugTextObject.AddComponent<TextMeshPro>();
        debugText.fontSize = 3;
        debugText.alignment = TextAlignmentOptions.Center;
        debugText.color = Color.white;

        debugText.outlineWidth = 0.2f;
        debugText.outlineColor = Color.black;

        debugTextObject.SetActive(showDebugInfo);
    }

    // -------------------- Update Logic --------------------

    private void HandleUpdateTimer()
    {
        updateTimer += Time.deltaTime;
        if (updateTimer < updateInterval) return;

        updateTimer = 0f;

        UpdatePosition();
        FaceCamera();
        UpdateDebugText();
    }

    private void UpdatePosition()
    {
        if (debugTextObject != null)
        {
            debugTextObject.transform.position = transform.position + offset;
        }
    }

    private void FaceCamera()
    {
        if (Camera.main != null && debugTextObject != null)
        {
            debugTextObject.transform.rotation = Camera.main.transform.rotation;
        }
    }

    private void UpdateDebugText()
    {
        debugText.text = BuildDebugString();
        debugText.color = GetStateColor();
    }

    // -------------------- Debug Info --------------------

    private string BuildDebugString()
    {
        if (enemy == null) return "No Enemy";

        string info = "";

        info += $"<b>State:</b> {enemy.stateMachine.currentState}\n";

        bool canSeePlayer = enemy.perception.CanSeePlayer();
        info += $"<b>Sees Player:</b> {(canSeePlayer ? "<color=green>YES</color>" : "<color=red>NO</color>")}\n";

        float speed = GetCurrentSpeed();
        info += $"<b>Speed:</b> {speed:F2} m/s\n";

        info += GetWeaponInfo();
        info += GetDistanceToPlayer();
        info += GetNavigationInfo();

        return info;
    }

    private float GetCurrentSpeed()
    {
        if (enemy.movement.currentIdleBehavior == EnemyMovement.IdleBehavior.Roamer)
        {
            return enemy.rb.linearVelocity.magnitude;
        }

        return enemy.agent.velocity.magnitude;
    }

    private string GetWeaponInfo()
    {
        if (enemy.combat.currentWeapon != WeaponType.NoWeapon)
        {
            string info = $"<b>Weapon:</b> {enemy.combat.currentWeapon}\n";

            if (!enemy.combat.isMeleeWeapon)
            {
                info += $"<b>Ammo:</b> {enemy.combat.currentAmmo}\n";
            }

            return info;
        }

        return "<b>Weapon:</b> None\n";
    }

    private string GetDistanceToPlayer()
    {
        if (enemy.perception.player == null) return "";

        float dist = Vector2.Distance(transform.position, enemy.perception.player.position);
        return $"<b>Dist to Player:</b> {dist:F1}m\n";
    }

    private string GetNavigationInfo()
    {
        if (enemy.agent.hasPath)
        {
            return $"<b>Dist to Target:</b> {enemy.agent.remainingDistance:F1}m";
        }

        return "";
    }

    private Color GetStateColor()
    {
        switch (enemy.stateMachine.currentState)
        {
            case EnemyStateMachine.State.Idle:
                return Color.white;
            case EnemyStateMachine.State.Inspect:
                return Color.yellow;
            case EnemyStateMachine.State.Attack:
                return Color.red;
            case EnemyStateMachine.State.Chase:
                return new Color(1f, 0.5f, 0f);
            case EnemyStateMachine.State.Dead:
                return Color.gray;
            default:
                return Color.white;
        }
    }

    // -------------------- Public --------------------

    public void ToggleDebugDisplay()
    {
        showDebugInfo = !showDebugInfo;

        if (debugTextObject != null)
        {
            debugTextObject.SetActive(showDebugInfo);
        }
    }

    // -------------------- Cleanup --------------------

    private void OnDestroy()
    {
        if (debugTextObject != null)
        {
            Destroy(debugTextObject);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!showDebugInfo || enemy == null || enemy.perception == null) return;

        Vector3 pos = transform.position;
        float visionRange = enemy.perception.visionRange;

        Gizmos.color = enemy.perception.CanSeePlayer() ? Color.green : Color.blue;
        UnityEditor.Handles.color = Gizmos.color;
        UnityEditor.Handles.DrawWireDisc(pos, Vector3.forward, visionRange);

        if (enemy.perception.player != null && enemy.perception.CanSeePlayer())
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(pos, enemy.perception.player.position);
        }
    }
#endif
}