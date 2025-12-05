using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;

/// <summary>
/// Singleton GameManager that persists across scenes.
/// Handles game state, progress saving, and global settings.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    [Tooltip("Total number of levels in the game")]
    public int totalLevels = 9; // 3 levels per area x 3 areas
    
    [Tooltip("Stars required to unlock next area")]
    public int starsPerArea = 5;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    // Player progress
    private Dictionary<int, int> levelStars = new Dictionary<int, int>(); // levelIndex -> stars (0-3)
    private HashSet<int> unlockedLevels = new HashSet<int>();
    private bool hasLiquidAbility = false;
    private bool hasGasAbility = false;
    private bool hasFrozenAbility = false;

    // Events
    public event Action<int, int> OnStarsCollected; // levelIndex, totalStars
    public event Action<int> OnLevelUnlocked;
    public event Action<string> OnAbilityUnlocked; // "Liquid", "Gas", "Frozen"

    // Save keys
    private const string SAVE_KEY_STARS = "Matter_LevelStars_";
    private const string SAVE_KEY_UNLOCKED = "Matter_UnlockedLevels";
    private const string SAVE_KEY_LIQUID = "Matter_HasLiquid";
    private const string SAVE_KEY_GAS = "Matter_HasGas";
    private const string SAVE_KEY_FROZEN = "Matter_HasFrozen";

    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadProgress();
    }

    #region Progress Management

    /// <summary>
    /// Get total stars collected across all levels
    /// </summary>
    public int GetTotalStars()
    {
        int total = 0;
        foreach (var kvp in levelStars)
        {
            total += kvp.Value;
        }
        return total;
    }

    /// <summary>
    /// Get stars collected for a specific level
    /// </summary>
    public int GetLevelStars(int levelIndex)
    {
        return levelStars.TryGetValue(levelIndex, out int stars) ? stars : 0;
    }

    /// <summary>
    /// Set stars for a level (only updates if new count is higher)
    /// </summary>
    public void SetLevelStars(int levelIndex, int stars)
    {
        stars = Mathf.Clamp(stars, 0, 3);
        
        int currentStars = GetLevelStars(levelIndex);
        if (stars > currentStars)
        {
            levelStars[levelIndex] = stars;
            SaveProgress();
            OnStarsCollected?.Invoke(levelIndex, stars);

            // Check for new area unlocks
            CheckAreaUnlocks();
        }
    }

    /// <summary>
    /// Check if a level is unlocked
    /// </summary>
    public bool IsLevelUnlocked(int levelIndex)
    {
        // Level 1 (index 0) is always unlocked
        if (levelIndex == 0) return true;
        return unlockedLevels.Contains(levelIndex);
    }

    /// <summary>
    /// Unlock a specific level
    /// </summary>
    public void UnlockLevel(int levelIndex)
    {
        if (!unlockedLevels.Contains(levelIndex))
        {
            unlockedLevels.Add(levelIndex);
            SaveProgress();
            OnLevelUnlocked?.Invoke(levelIndex);
            
            if (debugMode)
                Debug.Log($"[GameManager] Level {levelIndex} unlocked!");
        }
    }

    /// <summary>
    /// Complete a level and unlock the next one
    /// </summary>
    public void CompleteLevel(int levelIndex, int starsCollected)
    {
        SetLevelStars(levelIndex, starsCollected);
        
        // Unlock next level
        int nextLevel = levelIndex + 1;
        if (nextLevel < totalLevels)
        {
            UnlockLevel(nextLevel);
        }
    }

    private void CheckAreaUnlocks()
    {
        int totalStars = GetTotalStars();

        // Area 2 (Forest) unlocks at 5 stars
        if (totalStars >= starsPerArea && !IsLevelUnlocked(3))
        {
            UnlockLevel(3); // First forest level
        }

        // Area 3 (Cavern) unlocks at 10 stars
        if (totalStars >= starsPerArea * 2 && !IsLevelUnlocked(6))
        {
            UnlockLevel(6); // First cavern level
        }
    }

    #endregion

    #region Ability Management

    public bool HasLiquidAbility => hasLiquidAbility;
    public bool HasGasAbility => hasGasAbility;
    public bool HasFrozenAbility => hasFrozenAbility;

    public void UnlockLiquidAbility()
    {
        if (!hasLiquidAbility)
        {
            hasLiquidAbility = true;
            SaveProgress();
            OnAbilityUnlocked?.Invoke("Liquid");
            
            if (debugMode)
                Debug.Log("[GameManager] Liquid ability unlocked!");
        }
    }

    public void UnlockGasAbility()
    {
        if (!hasGasAbility)
        {
            hasGasAbility = true;
            SaveProgress();
            OnAbilityUnlocked?.Invoke("Gas");
            
            if (debugMode)
                Debug.Log("[GameManager] Gas ability unlocked!");
        }
    }

    public void UnlockFrozenAbility()
    {
        if (!hasFrozenAbility)
        {
            hasFrozenAbility = true;
            SaveProgress();
            OnAbilityUnlocked?.Invoke("Frozen");
            
            if (debugMode)
                Debug.Log("[GameManager] Frozen ability unlocked!");
        }
    }

    #endregion

    #region Scene Management

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void LoadLevelSelect()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("LevelSelect");
    }

    public void LoadLevel(int levelIndex)
    {
        if (IsLevelUnlocked(levelIndex))
        {
            Time.timeScale = 1f;
            string levelName = GetLevelSceneName(levelIndex);
            SceneManager.LoadScene(levelName);
        }
        else
        {
            Debug.LogWarning($"[GameManager] Level {levelIndex} is locked!");
        }
    }

    public void LoadLevel(string levelName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(levelName);
    }

    public void RestartCurrentLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadNextLevel()
    {
        int currentIndex = GetCurrentLevelIndex();
        if (currentIndex >= 0 && currentIndex < totalLevels - 1)
        {
            LoadLevel(currentIndex + 1);
        }
        else
        {
            LoadLevelSelect();
        }
    }

    public int GetCurrentLevelIndex()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        
        // Parse level index from scene name (e.g., "Level_1" -> 0)
        if (currentScene.StartsWith("Level_"))
        {
            if (int.TryParse(currentScene.Substring(6), out int levelNum))
            {
                return levelNum - 1; // Convert to 0-based index
            }
        }
        
        // Alternative naming: "Cave_1", "Forest_1", "Cavern_1"
        string[] prefixes = { "Cave_", "Forest_", "Cavern_" };
        for (int area = 0; area < prefixes.Length; area++)
        {
            if (currentScene.StartsWith(prefixes[area]))
            {
                if (int.TryParse(currentScene.Substring(prefixes[area].Length), out int levelNum))
                {
                    return (area * 3) + (levelNum - 1);
                }
            }
        }

        return -1;
    }

    private string GetLevelSceneName(int levelIndex)
    {
        // You can customize this based on your scene naming convention
        int area = levelIndex / 3;
        int levelInArea = (levelIndex % 3) + 1;

        string[] areaNames = { "Cave", "Forest", "Cavern" };
        
        if (area < areaNames.Length)
        {
            return $"{areaNames[area]}_{levelInArea}";
        }

        // Fallback to generic naming
        return $"Level_{levelIndex + 1}";
    }

    #endregion

    #region Save/Load

    private void SaveProgress()
    {
        // Save stars for each level
        foreach (var kvp in levelStars)
        {
            PlayerPrefs.SetInt(SAVE_KEY_STARS + kvp.Key, kvp.Value);
        }

        // Save unlocked levels as comma-separated string
        string unlockedStr = string.Join(",", unlockedLevels);
        PlayerPrefs.SetString(SAVE_KEY_UNLOCKED, unlockedStr);

        // Save abilities
        PlayerPrefs.SetInt(SAVE_KEY_LIQUID, hasLiquidAbility ? 1 : 0);
        PlayerPrefs.SetInt(SAVE_KEY_GAS, hasGasAbility ? 1 : 0);
        PlayerPrefs.SetInt(SAVE_KEY_FROZEN, hasFrozenAbility ? 1 : 0);

        PlayerPrefs.Save();

        if (debugMode)
            Debug.Log("[GameManager] Progress saved!");
    }

    private void LoadProgress()
    {
        levelStars.Clear();
        unlockedLevels.Clear();

        // Load stars for each level
        for (int i = 0; i < totalLevels; i++)
        {
            int stars = PlayerPrefs.GetInt(SAVE_KEY_STARS + i, 0);
            if (stars > 0)
            {
                levelStars[i] = stars;
            }
        }

        // Load unlocked levels
        string unlockedStr = PlayerPrefs.GetString(SAVE_KEY_UNLOCKED, "");
        if (!string.IsNullOrEmpty(unlockedStr))
        {
            string[] parts = unlockedStr.Split(',');
            foreach (string part in parts)
            {
                if (int.TryParse(part, out int levelIndex))
                {
                    unlockedLevels.Add(levelIndex);
                }
            }
        }

        // Load abilities
        hasLiquidAbility = PlayerPrefs.GetInt(SAVE_KEY_LIQUID, 0) == 1;
        hasGasAbility = PlayerPrefs.GetInt(SAVE_KEY_GAS, 0) == 1;
        hasFrozenAbility = PlayerPrefs.GetInt(SAVE_KEY_FROZEN, 0) == 1;

        if (debugMode)
            Debug.Log($"[GameManager] Progress loaded! Total stars: {GetTotalStars()}");
    }

    /// <summary>
    /// Reset all progress (for debug or new game)
    /// </summary>
    public void ResetProgress()
    {
        levelStars.Clear();
        unlockedLevels.Clear();
        hasLiquidAbility = false;
        hasGasAbility = false;
        hasFrozenAbility = false;

        // Clear PlayerPrefs
        for (int i = 0; i < totalLevels; i++)
        {
            PlayerPrefs.DeleteKey(SAVE_KEY_STARS + i);
        }
        PlayerPrefs.DeleteKey(SAVE_KEY_UNLOCKED);
        PlayerPrefs.DeleteKey(SAVE_KEY_LIQUID);
        PlayerPrefs.DeleteKey(SAVE_KEY_GAS);
        PlayerPrefs.DeleteKey(SAVE_KEY_FROZEN);
        PlayerPrefs.Save();

        if (debugMode)
            Debug.Log("[GameManager] Progress reset!");
    }

    #endregion

    public void QuitGame()
    {
        SaveProgress();
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
