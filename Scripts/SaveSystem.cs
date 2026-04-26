using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class SaveData
{
    public int sceneIndex;
    public float playerX;
    public float playerY;
    public float playerZ;
}

public static class SaveSystem
{
    private static string path => Application.persistentDataPath + "/save.json";

    public static void SaveGame(GameObject player)
    {
        SaveData data = new SaveData
        {
            sceneIndex = SceneManager.GetActiveScene().buildIndex,
            playerX = player.transform.position.x,
            playerY = player.transform.position.y,
            playerZ = player.transform.position.z
        };

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(path, json);

        Debug.Log("Game saved");
    }

    public static SaveData LoadGame()
    {
        if (!File.Exists(path)) return null;

        string json = File.ReadAllText(path);
        return JsonUtility.FromJson<SaveData>(json);
    }
}