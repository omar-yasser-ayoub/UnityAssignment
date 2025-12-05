using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Individual level button in the level select screen.
/// Shows level number, star count, and lock status.
/// </summary>
public class LevelSelectButton : MonoBehaviour
{
    [Header("UI Elements")]
    public Text levelNumberText;
    public Image[] starImages;
    public GameObject lockIcon;
    public Image backgroundImage;

    [Header("Colors")]
    public Color unlockedColor = Color.white;
    public Color lockedColor = new Color(0.4f, 0.4f, 0.4f);
    public Color completedColor = new Color(0.8f, 1f, 0.8f);

    private Button button;
    private int levelIndex;

    void Awake()
    {
        button = GetComponent<Button>();
    }

    public void Setup(int levelIndex, int displayNumber)
    {
        this.levelIndex = levelIndex;

        bool isUnlocked = GameManager.Instance != null && GameManager.Instance.IsLevelUnlocked(levelIndex);
        int stars = GameManager.Instance != null ? GameManager.Instance.GetLevelStars(levelIndex) : 0;

        // Set level number
        if (levelNumberText != null)
        {
            levelNumberText.text = displayNumber.ToString();
            levelNumberText.color = isUnlocked ? Color.white : Color.gray;
        }

        // Set lock icon
        if (lockIcon != null)
        {
            lockIcon.SetActive(!isUnlocked);
        }

        // Set stars
        if (starImages != null)
        {
            for (int i = 0; i < starImages.Length; i++)
            {
                if (starImages[i] != null)
                {
                    starImages[i].enabled = isUnlocked; // Hide stars on locked levels
                    starImages[i].color = i < stars ? Color.yellow : new Color(1, 1, 1, 0.3f);
                }
            }
        }

        // Set background color
        if (backgroundImage != null)
        {
            if (!isUnlocked)
                backgroundImage.color = lockedColor;
            else if (stars == 3)
                backgroundImage.color = completedColor;
            else
                backgroundImage.color = unlockedColor;
        }

        // Set button interactable
        if (button != null)
        {
            button.interactable = isUnlocked;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClicked);
        }
    }

    private void OnClicked()
    {
        Debug.Log($"[LevelSelectButton] Level {levelIndex} clicked");
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadLevel(levelIndex);
        }
    }
}
