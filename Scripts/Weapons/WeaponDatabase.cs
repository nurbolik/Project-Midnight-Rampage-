using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "WeaponDatabase", menuName = "Game/Weapon Database", order = 1)]
public class WeaponDatabase : ScriptableObject
{
    // -------------------- SINGLETON --------------------
    private static WeaponDatabase _instance;
    public static WeaponDatabase Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<WeaponDatabase>("WeaponDatabase");

                if (_instance == null)
                {
                    Debug.LogError(
                        "[WeaponDatabase] No WeaponDatabase found in Resources folder! " +
                        "Create one at Assets/Resources/WeaponDatabase.asset"
                    );
                }
            }
            return _instance;
        }
    }

    // -------------------- WEAPON ENTRY --------------------
    [System.Serializable]
    public class WeaponEntry
    {
        public WeaponType weaponType;
        public GameObject weaponPickupPrefab;

        [Tooltip("Fire rate in seconds between shots")]
        public float fireRate = 0.2f;

        public bool isAutomatic;
        public bool isMelee;
    }

    // -------------------- DATABASE --------------------
    [Header("Weapon Pickup Prefabs")]
    public List<WeaponEntry> weapons = new List<WeaponEntry>();
    private Dictionary<WeaponType, GameObject> weaponDictionary;

    private void OnEnable()
    {
        _instance = this;
        BuildDictionary();
    }

    // -------------------- DICTIONARY MANAGEMENT --------------------
    private void BuildDictionary()
    {
        weaponDictionary = new Dictionary<WeaponType, GameObject>();
        foreach (var entry in weapons)
        {
            if (entry.weaponPickupPrefab != null)
            {
                weaponDictionary[entry.weaponType] = entry.weaponPickupPrefab;
            }
        }
    }

    // -------------------- API METHODS --------------------
    public bool TryGetWeaponEntry(WeaponType weaponType, out WeaponEntry entry)
    {
        foreach (var e in weapons)
        {
            if (e.weaponType == weaponType)
            {
                entry = e;
                return true;
            }
        }

        Debug.LogWarning($"[WeaponDatabase] No data found for {weaponType}");
        entry = null;
        return false;
    }

    public GameObject GetWeaponPrefab(WeaponType weaponType)
    {
        if (weaponDictionary == null || weaponDictionary.Count == 0)
        {
            BuildDictionary();
        }

        if (weaponDictionary.TryGetValue(weaponType, out GameObject prefab))
        {
            return prefab;
        }

        Debug.LogWarning(
            $"[WeaponDatabase] No weapon pickup prefab found for {weaponType}. " +
            "Add it to the WeaponDatabase asset."
        );
        return null;
    }

    public bool HasWeapon(WeaponType weaponType)
    {
        if (weaponDictionary == null || weaponDictionary.Count == 0)
        {
            BuildDictionary();
        }
        return weaponDictionary.ContainsKey(weaponType);
    }
}