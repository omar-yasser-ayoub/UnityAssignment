using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the level select screen.
/// Shows all levels with their star counts and lock status.
/// </summary>
public class LevelSelectController : MonoBehaviour
{
    [Header("Level Button Prefab")]
    [Tooltip("Prefab for level button (should have LevelButton component)")]
    public GameObject levelButtonPrefab;

    [Header("Level Grid")]
    [Tooltip("Parent container for level buttons")]
    public Transform levelGrid;

    [Header("Area Tabs")]
    public Button caveTabButton;
    public Button forestTabButton;
    public Button cavernTabButton;
    
    [Header("Area Info")]
    public Text areaNameText;
    public Text areaStarsText;

    [Header("Navigation")]
    public Button backButton;

    [Header("Visual")]
    public Color selectedTabColor = Color.white;
    public Color unselectedTabColor = new Color(0.7f, 0.7f, 0.7f);

    // Level data
    private string[] areaNames = { "The Cave", "The Forest", "The Cavern" };
    private int currentArea = 0;

    void Start()
    {
        SetupButtons();
        ShowArea(0); // Start with cave
    }

    private void SetupButtons()
    {
        if (caveTabButton != null)
            caveTabButton.onClick.AddListener(() => ShowArea(0));

        if (forestTabButton != null)
            forestTabButton.onClick.AddListener(() => ShowArea(1));

        if (cavernTabButton != null)
            cavernTabButton.onClick.AddListener(() => ShowArea(2));

        if (backButton != null)
            backButton.onClick.AddListener(GoBack);
    }

    public void ShowArea(int areaIndex)
    {
        currentArea = areaIndex;

        // Update tab colors
        UpdateTabColors();

        // Update area info
        if (areaNameText != null)
            areaNameText.text = areaNames[areaIndex];

        // Calculate stars for this area
        int areaStars = GetAreaStars(areaIndex);
        int maxAreaStars = 9; // 3 levels x 3 stars
        if (areaStarsText != null)
            areaStarsText.text = $"? {areaStars}/{maxAreaStars}";

        // Generate level buttons
        GenerateLevelButtons(areaIndex);
    }

    private void UpdateTabColors()
    {
        if (caveTabButton != null)
        {
            var img = caveTabButton.GetComponent<Image>();
            if (img != null) img.color = currentArea == 0 ? selectedTabColor : unselectedTabColor;
        }

        if (forestTabButton != null)
        {
            var img = forestTabButton.GetComponent<Image>();
            if (img != null) img.color = currentArea == 1 ? selectedTabColor : unselectedTabColor;
        }

        if (cavernTabButton != null)
        {
            var img = cavernTabButton.GetComponent<Image>();
            if (img != null) img.color = currentArea == 2 ? selectedTabColor : unselectedTabColor;
        }
    }

    private void GenerateLevelButtons(int areaIndex)
    {
        // Clear existing buttons
        if (levelGrid != null)
        {
            foreach (Transform child in levelGrid)
            {
                Destroy(child.gameObject);
            }
        }

        // Generate new buttons
        int startLevel = areaIndex * 3;
        for (int i = 0; i < 3; i++)
        {
            int levelIndex = startLevel + i;
            CreateLevelButton(levelIndex, i + 1);
        }
    }

    private void CreateLevelButton(int levelIndex, int displayNumber)
    {
        if (levelButtonPrefab == null || levelGrid == null) return;

        GameObject buttonObj = Instantiate(levelButtonPrefab, levelGrid);
        
        var levelButton = buttonObj.GetComponent<LevelSelectButton>();
        if (levelButton != null)
        {
            levelButton.Setup(levelIndex, displayNumber);
        }
        else
        {
            // Fallback: setup manually
            SetupButtonManually(buttonObj, levelIndex, displayNumber);
        }
    }

    private void SetupButtonManually(GameObject buttonObj, int levelIndex, int displayNumber)
    {
        bool isUnlocked = GameManager.Instance != null && GameManager.Instance.IsLevelUnlocked(levelIndex);
        int stars = GameManager.Instance != null ? GameManager.Instance.GetLevelStars(levelIndex) : 0;

        // Get components
        var button = buttonObj.GetComponent<Button>();
        var numberText = buttonObj.GetComponentInChildren<Text>();

        if (numberText != null)
            numberText.text = displayNumber.ToString();

        if (button != null)
        {
            button.interactable = isUnlocked;
            
            int capturedIndex = levelIndex; // Capture for lambda
            button.onClick.AddListener(() => LoadLevel(capturedIndex));
        }

        // Update visual based on locked/unlocked
        var image = buttonObj.GetComponent<Image>();
        if (image != null)
        {
            image.color = isUnlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        }
    }

    private int GetAreaStars(int areaIndex)
    {
        if (GameManager.Instance == null) return 0;

        int total = 0;
        int startLevel = areaIndex * 3;
        for (int i = 0; i < 3; i++)
        {
            total += GameManager.Instance.GetLevelStars(startLevel + i);
        }
        return total;
    }

    public void LoadLevel(int levelIndex)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadLevel(levelIndex);
        }
    }

    public void GoBack()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadMainMenu();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }
}
