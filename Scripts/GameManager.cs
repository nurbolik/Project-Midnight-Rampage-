using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.InputSystem; 
using System.Collections;

public class GameManager : MonoBehaviour
{
    // -------------------- SINGLETON --------------------
    public static GameManager Instance { get; private set; }
    public bool IsPlayerDead { get; private set; }

    // -------------------- COMPONENT REFERENCES --------------------
    [Header("Component References")]
    [SerializeField] private CameraController cameraFollowing;
    [SerializeField] private TextMeshProUGUI levelStateText; 

    // -------------------- PLAYER --------------------
    [Header("Player Reference")]
    [SerializeField] public GameObject player; 

    // -------------------- LEVEL MANAGEMENT --------------------
    [Header("Level Management")]
    [SerializeField] private int nextLevelIndex = 1;

    // -------------------- ENEMY & SHADER TRACKING --------------------
    [Header("Enemy Tracking & Shader")]
    [SerializeField] private SpriteRenderer backgroundSpriteRenderer;
    private string colorPropertyName = "_SkyBlue";
    [SerializeField] private Color startColor = Color.black;
    [SerializeField] private Color endColor = Color.red;
    [SerializeField] private bool useColorProgression = true;

    private int totalEnemies = 0;
    private int enemiesKilled = 0;
    private bool isLevelComplete = false;
    private Material backgroundMaterial;

    private IEnumerator LoadSavedGame()
    {
        SaveData data = SaveSystem.LoadGame();

        if (data == null)
        {
            Debug.LogWarning("No save file found");
            yield break;
        }

        IsPlayerDead = false;

        SceneManager.LoadScene(data.sceneIndex);

        yield return null;

        GameObject loadedPlayer = GameObject.FindGameObjectWithTag("Player");

        if (loadedPlayer != null)
        {
            loadedPlayer.transform.position = new Vector3(
                data.playerX,
                data.playerY,
                data.playerZ
            );
        }

        if (levelStateText != null)
            levelStateText.text = "";

        Debug.Log("Game loaded after death");
    }

    // -------------------- UNITY EVENTS --------------------
    private void Awake()
    {
        
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

       
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
                Debug.LogWarning("GameManager: No player assigned or found with tag 'Player'!");
        }
    }

    private void Start()
    {
        
        Enemy[] allEnemies = FindObjectsOfType<Enemy>();
        totalEnemies = allEnemies.Length;
        enemiesKilled = 0;
        isLevelComplete = false;

        if (useColorProgression && backgroundSpriteRenderer != null)
        {
            backgroundMaterial = backgroundSpriteRenderer.material;
            backgroundMaterial.SetColor(colorPropertyName, startColor);
            Debug.Log($"[GameManager] Level started with {totalEnemies} enemies. Shader '{colorPropertyName}' initialized.");
        }
        else if (useColorProgression)
        {
            Debug.LogWarning("[GameManager] Color progression enabled but no sprite renderer assigned!");
        }
        else
        {
            Debug.Log($"[GameManager] Level started with {totalEnemies} enemies. Color progression disabled.");
        }

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
    }

    private void Update()
    {
       
        if (Keyboard.current == null) return;

     
        if (Keyboard.current.rKey.wasReleasedThisFrame)
            RestartScene();

       
        if (Keyboard.current.eKey.wasReleasedThisFrame && isLevelComplete)
            LoadNextLevel();

        if (Keyboard.current.kKey.wasReleasedThisFrame)
        {
            SaveSystem.SaveGame(player);
        }

        if (Keyboard.current.lKey.wasReleasedThisFrame)
        {
            SaveData data = SaveSystem.LoadGame();

            if (data != null && player != null)
            {
                Vector3 pos = new Vector3(data.playerX, data.playerY, data.playerZ);

                if (player.TryGetComponent<PlayerController>(out var pc))
                    pc.Respawn(pos);

                IsPlayerDead = false;

                if (levelStateText != null)
                    levelStateText.text = "";
            }
        }
    }

    // -------------------- SCENE MANAGEMENT --------------------
    private void RestartScene()
    {
        IsPlayerDead = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void LoadNextLevel()
    {
        Debug.Log($"[GameManager] Loading level {nextLevelIndex}");
        SceneManager.LoadScene(nextLevelIndex);
    }

    public void ChangeSceneToNumber(int sceneIndex)
    {
        SceneManager.LoadScene(sceneIndex);
    }

    public void ChangeSceneToName(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    // -------------------- PLAYER MANAGEMENT --------------------
    public void PlayerDied()
    {
        IsPlayerDead = true;

        if (levelStateText != null)
            levelStateText.text = "You Died!\nPress R to restart";
        else
            Debug.LogWarning("GameManager: Level state text not assigned in inspector!");
    }

    public void TeleportPlayer(Transform target)
    {
        if (player == null || target == null) return;

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.position = target.position;
            rb.linearVelocity = Vector2.zero;
        }
        else
        {
            player.transform.position = target.position;
        }
    }

    public void ChangeCameraFollowTarget(Transform newTarget)
    {
        if (cameraFollowing != null && newTarget != null)
            cameraFollowing._target = newTarget;
    }

    // -------------------- GAMEOBJECT / COMPONENT CONTROL --------------------
    public void DestroyGameObject(GameObject objectToDestroy)
    {
        if (objectToDestroy != null)
            objectToDestroy.SetActive(false);
    }

    public void ActivateGameObject(GameObject objectToActivate)
    {
        if (objectToActivate != null)
            objectToActivate.SetActive(true);
    }

    public void ActivateComponent(Behaviour component)
    {
        if (component != null)
            component.enabled = true;
    }

    public void DeactivateComponent(Behaviour component)
    {
        if (component != null)
            component.enabled = false;
    }

    // -------------------- ENEMY TRACKING --------------------
    public void EnemyKilled()
    {
        enemiesKilled++;
        Debug.Log($"[GameManager] Enemy killed! ({enemiesKilled}/{totalEnemies})");

        
        if (useColorProgression && backgroundMaterial != null)
            UpdateShaderColor();

       
        if (enemiesKilled >= totalEnemies && totalEnemies > 0 && !isLevelComplete)
            CompleteLevel();
    }

    private void UpdateShaderColor()
    {
        if (totalEnemies == 0) return;

        float progress = Mathf.Clamp01((float)enemiesKilled / totalEnemies);
        Color newColor = Color.Lerp(startColor, endColor, progress);
        backgroundMaterial.SetColor(colorPropertyName, newColor);

        Debug.Log($"[GameManager] Shader color updated: {progress:F2} ({enemiesKilled}/{totalEnemies})");
    }

    private void CompleteLevel()
    {
        isLevelComplete = true;
        Debug.Log("[GameManager] Level complete! All enemies defeated.");

        if (levelStateText != null)
            levelStateText.text = $"Level Complete!\nPress E to continue to level {nextLevelIndex}";
    }
}