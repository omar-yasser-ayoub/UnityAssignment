using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Manages all in-game UI elements.
/// Connect UI elements in the inspector.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("HUD Elements")]
    [Tooltip("Star counter text (e.g., '2/3')")]
    public Text starCountText;
    
    [Tooltip("Individual star icons (3 total)")]
    public Image[] starIcons;
    
    [Tooltip("Sprite for collected star")]
    public Sprite starFilledSprite;
    
    [Tooltip("Sprite for uncollected star")]
    public Sprite starEmptySprite;

    [Header("Energy Bar")]
    public Slider energySlider;
    public Image energyFillImage;
    public Color energyFullColor = Color.cyan;
    public Color energyLowColor = Color.red;

    [Header("State Indicator")]
    public Image stateIndicatorImage;
    public Sprite solidStateSprite;
    public Sprite liquidStateSprite;
    public Sprite gasStateSprite;
    public Sprite frozenStateSprite;

    [Header("Notification")]
    public GameObject notificationPanel;
    public Text notificationText;
    public float notificationDuration = 3f;

    [Header("Level Complete Panel")]
    public GameObject levelCompletePanel;
    public Text levelCompleteTitle;
    public Image[] levelCompleteStars;
    public Button nextLevelButton;
    public Button restartButton;
    public Button menuButton;

    [Header("Pause Menu")]
    public GameObject pauseMenuPanel;

    private Coroutine notificationCoroutine;
    private PlayerStateMachine player;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // Find player
        player = FindFirstObjectByType<PlayerStateMachine>();

        // Subscribe to state changes
        if (player != null)
        {
            player.OnStateChanged += HandleStateChanged;
            UpdateStateIndicator(player.CurrentStateType);
        }

        // Hide panels initially
        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(false);

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        if (notificationPanel != null)
            notificationPanel.SetActive(false);

        // Initial UI update
        UpdateStarCount(0, 3);
        UpdateEnergy(1f);
    }

    void OnDestroy()
    {
        if (player != null)
        {
            player.OnStateChanged -= HandleStateChanged;
        }
    }

    #region Star Display

    public void UpdateStarCount(int collected, int total)
    {
        if (starCountText != null)
        {
            starCountText.text = $"{collected}/{total}";
        }

        // Update star icons
        if (starIcons != null)
        {
            for (int i = 0; i < starIcons.Length; i++)
            {
                if (starIcons[i] != null)
                {
                    if (i < collected)
                    {
                        starIcons[i].sprite = starFilledSprite;
                        starIcons[i].color = Color.yellow;
                    }
                    else
                    {
                        starIcons[i].sprite = starEmptySprite;
                        starIcons[i].color = new Color(1, 1, 1, 0.3f);
                    }
                }
            }
        }
    }

    #endregion

    #region Energy Bar

    public void UpdateEnergy(float normalizedValue)
    {
        if (energySlider != null)
        {
            energySlider.value = normalizedValue;
        }

        if (energyFillImage != null)
        {
            energyFillImage.color = Color.Lerp(energyLowColor, energyFullColor, normalizedValue);
        }
    }

    public void SetEnergyUnlimited(bool unlimited)
    {
        if (energySlider != null)
        {
            // Visual indicator for unlimited energy (cave levels)
            if (unlimited)
            {
                energySlider.value = 1f;
                if (energyFillImage != null)
                    energyFillImage.color = new Color(0.5f, 1f, 1f, 1f); // Bright cyan
            }
        }
    }

    #endregion

    #region State Indicator

    private void HandleStateChanged(MatterState oldState, MatterState newState)
    {
        UpdateStateIndicator(newState);
    }

    public void UpdateStateIndicator(MatterState state)
    {
        if (stateIndicatorImage == null) return;

        stateIndicatorImage.sprite = state switch
        {
            MatterState.Solid => solidStateSprite,
            MatterState.Liquid => liquidStateSprite,
            MatterState.Gas => gasStateSprite,
            MatterState.Frozen => frozenStateSprite,
            _ => solidStateSprite
        };

        // Color tint based on state
        stateIndicatorImage.color = state switch
        {
            MatterState.Solid => Color.white,
            MatterState.Liquid => new Color(0.3f, 0.6f, 1f),
            MatterState.Gas => new Color(0.8f, 0.8f, 0.9f),
            MatterState.Frozen => new Color(0.7f, 0.9f, 1f),
            _ => Color.white
        };
    }

    #endregion

    #region Notifications

    public void ShowNotification(string message)
    {
        if (notificationPanel == null || notificationText == null) return;

        if (notificationCoroutine != null)
        {
            StopCoroutine(notificationCoroutine);
        }

        notificationText.text = message;
        notificationPanel.SetActive(true);
        notificationCoroutine = StartCoroutine(HideNotificationAfterDelay());
    }

    private IEnumerator HideNotificationAfterDelay()
    {
        yield return new WaitForSeconds(notificationDuration);
        
        if (notificationPanel != null)
        {
            notificationPanel.SetActive(false);
        }
    }

    #endregion

    #region Level Complete

    public void ShowLevelComplete(int starsCollected, int totalStars)
    {
        if (levelCompletePanel == null) return;

        levelCompletePanel.SetActive(true);

        // Pause game
        Time.timeScale = 0f;

        // Update title
        if (levelCompleteTitle != null)
        {
            levelCompleteTitle.text = starsCollected >= totalStars ? "Perfect!" : "Level Complete!";
        }

        // Update stars
        if (levelCompleteStars != null)
        {
            for (int i = 0; i < levelCompleteStars.Length; i++)
            {
                if (levelCompleteStars[i] != null)
                {
                    levelCompleteStars[i].enabled = i < starsCollected;
                }
            }
        }

        // Setup buttons
        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.RemoveAllListeners();
            nextLevelButton.onClick.AddListener(() => {
                Time.timeScale = 1f;
                GameManager.Instance?.LoadNextLevel();
            });
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(() => {
                Time.timeScale = 1f;
                GameManager.Instance?.RestartCurrentLevel();
            });
        }

        if (menuButton != null)
        {
            menuButton.onClick.RemoveAllListeners();
            menuButton.onClick.AddListener(() => {
                Time.timeScale = 1f;
                GameManager.Instance?.LoadLevelSelect();
            });
        }
    }

    #endregion

    #region Pause Menu

    public void TogglePause()
    {
        if (pauseMenuPanel == null) return;

        bool isPaused = pauseMenuPanel.activeSelf;
        SetPaused(!isPaused);
    }

    public void SetPaused(bool paused)
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(paused);
        }

        Time.timeScale = paused ? 0f : 1f;
    }

    public void ResumeGame()
    {
        SetPaused(false);
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        GameManager.Instance?.RestartCurrentLevel();
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        GameManager.Instance?.LoadLevelSelect();
    }

    #endregion
}
