using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Controls the level select screen.
/// Simple setup - assign each button directly.
/// </summary>
public class LevelSelectController : MonoBehaviour
{
    [Header("Level Buttons")]
    public Button level1Button;
    public Button level2Button;
    public Button level3Button;

    [Header("Navigation")]
    public Button backButton;

    void Start()
    {
        // Level 1
        if (level1Button != null)
        {
            level1Button.onClick.AddListener(() => SceneManager.LoadScene("Level 1"));
        }

        // Level 2
        if (level2Button != null)
        {
            level2Button.onClick.AddListener(() => SceneManager.LoadScene("Level 2"));
        }

        // Level 3
        if (level3Button != null)
        {
            level3Button.onClick.AddListener(() => SceneManager.LoadScene("Level 3"));
        }

        // Back to menu
        if (backButton != null)
        {
            backButton.onClick.AddListener(() => SceneManager.LoadScene("Menu"));
        }
    }
}
