using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles pause menu functionality.
/// Listens for Escape key to toggle pause.
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    [Header("Pause Panel")]
    public GameObject pausePanel;

    [Header("Buttons")]
    public Button resumeButton;
    public Button restartButton;
    public Button settingsButton;
    public Button mainMenuButton;
    public Button quitButton;

    [Header("Settings Panel (Optional)")]
    public GameObject settingsPanel;
    public Button settingsBackButton;
    public Slider musicSlider;
    public Slider sfxSlider;

    private bool isPaused = false;

    void Start()
    {
        // Hide panels initially
        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        // Setup buttons
        SetupButtons();

        // Load settings values
        LoadSettings();
    }

    void Update()
    {
        // Check for Escape key using legacy Input
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    private void SetupButtons()
    {
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(Resume);
        }

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(Restart);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(ShowSettings);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
        }

        if (settingsBackButton != null)
        {
            settingsBackButton.onClick.AddListener(HideSettings);
        }

        // Settings sliders
        if (musicSlider != null)
        {
            musicSlider.onValueChanged.AddListener(OnMusicChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.AddListener(OnSFXChanged);
        }
    }

    public void TogglePause()
    {
        if (isPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;

        if (pausePanel != null)
            pausePanel.SetActive(true);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        Debug.Log("[PauseMenu] Game paused");
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        Debug.Log("[PauseMenu] Game resumed");
    }

    public void Restart()
    {
        Resume();
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartCurrentLevel();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            );
        }
    }

    public void ReturnToMainMenu()
    {
        Resume();
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadMainMenu();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }

    public void QuitGame()
    {
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

    #region Settings

    private void ShowSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    private void HideSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    private void LoadSettings()
    {
        if (musicSlider != null)
            musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);

        if (sfxSlider != null)
            sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);
    }

    private void OnMusicChanged(float value)
    {
        PlayerPrefs.SetFloat("MusicVolume", value);
        PlayerPrefs.Save();
    }

    private void OnSFXChanged(float value)
    {
        PlayerPrefs.SetFloat("SFXVolume", value);
        PlayerPrefs.Save();
    }

    #endregion

    public bool IsPaused => isPaused;
}
