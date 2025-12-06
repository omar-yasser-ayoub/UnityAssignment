using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the main menu screen.
/// Attach to a Canvas in the MainMenu scene.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Buttons")]
    public Button playButton;
    public Button levelSelectButton;
    public Button quitButton;

    [Header("Optional Buttons")]
    public Button continueButton;
    public Button settingsButton;
    public Button creditsButton;

    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject settingsPanel;
    public GameObject creditsPanel;

    [Header("Settings (Optional)")]
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;
    public Toggle fullscreenToggle;
    public Button settingsBackButton;

    [Header("Credits (Optional)")]
    public Button creditsBackButton;

    [Header("Display (Optional)")]
    public Text totalStarsText;
    public Text versionText;

    void Start()
    {
        // Setup button listeners
        SetupButtons();

        // Show main panel
        ShowMainPanel();

        // Update display
        UpdateStarsDisplay();

        if (versionText != null)
        {
            versionText.text = $"v{Application.version}";
        }

        // Load settings
        LoadSettings();

        // Ensure time is running (in case we came from paused game)
        Time.timeScale = 1f;
    }

    private void SetupButtons()
    {
        // Play - Start new game from level 1
        if (playButton != null)
        {
            playButton.onClick.AddListener(OnPlayClicked);
        }

        // Continue - Load last unlocked level (optional)
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueClicked);
            
            // Only show if there's progress
            bool hasProgress = GameManager.Instance != null && GameManager.Instance.GetTotalStars() > 0;
            continueButton.gameObject.SetActive(hasProgress);
        }

        // Level Select
        if (levelSelectButton != null)
        {
            levelSelectButton.onClick.AddListener(OnLevelSelectClicked);
        }

        // Settings (optional)
        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(ShowSettingsPanel);
        }

        // Credits (optional)
        if (creditsButton != null)
        {
            creditsButton.onClick.AddListener(ShowCreditsPanel);
        }

        // Quit
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitClicked);
        }

        // Settings back
        if (settingsBackButton != null)
        {
            settingsBackButton.onClick.AddListener(ShowMainPanel);
        }

        // Credits back
        if (creditsBackButton != null)
        {
            creditsBackButton.onClick.AddListener(ShowMainPanel);
        }

        // Settings sliders
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        }
    }

    #region Button Handlers

    private void OnPlayClicked()
    {
        Debug.Log("[MainMenu] Play clicked - Loading Level 1");
        
        // Load first level
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadLevel(0);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Level 1");
        }
    }

    private void OnContinueClicked()
    {
        Debug.Log("[MainMenu] Continue clicked");
        
        if (GameManager.Instance != null)
        {
            // Find the last unlocked level
            int lastUnlocked = 0;
            for (int i = GameManager.Instance.totalLevels - 1; i >= 0; i--)
            {
                if (GameManager.Instance.IsLevelUnlocked(i))
                {
                    lastUnlocked = i;
                    break;
                }
            }
            
            GameManager.Instance.LoadLevel(lastUnlocked);
        }
    }

    private void OnLevelSelectClicked()
    {
        Debug.Log("[MainMenu] Level Select clicked");
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadLevelSelect();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Level Select");
        }
    }

    private void OnQuitClicked()
    {
        Debug.Log("[MainMenu] Quit clicked");
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.QuitGame();
        }
        else
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
    }

    #endregion

    #region Panels

    private void ShowMainPanel()
    {
        if (mainPanel != null) mainPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }

    private void ShowSettingsPanel()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }

    private void ShowCreditsPanel()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(true);
    }

    #endregion

    #region Settings

    private void LoadSettings()
    {
        float musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        bool fullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;

        if (musicVolumeSlider != null) musicVolumeSlider.value = musicVolume;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = sfxVolume;
        if (fullscreenToggle != null) fullscreenToggle.isOn = fullscreen;
    }

    private void OnMusicVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat("MusicVolume", value);
    }

    private void OnSFXVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat("SFXVolume", value);
    }

    private void OnFullscreenChanged(bool value)
    {
        PlayerPrefs.SetInt("Fullscreen", value ? 1 : 0);
        Screen.fullScreen = value;
    }

    #endregion

    #region Display

    private void UpdateStarsDisplay()
    {
        if (totalStarsText != null && GameManager.Instance != null)
        {
            int totalStars = GameManager.Instance.GetTotalStars();
            int maxStars = GameManager.Instance.totalLevels * 3;
            totalStarsText.text = $"Stars: {totalStars}/{maxStars}";
        }
    }

    #endregion
}
