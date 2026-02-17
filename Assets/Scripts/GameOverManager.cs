using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI gameOverText;
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button quitButton;

    private bool isGameOver = false;

    public bool IsGameOver => isGameOver;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        Debug.Log("[GameOverManager] Start");
        
        // Find GameOverPanel if not assigned
        if (gameOverPanel == null)
            gameOverPanel = GameObject.Find("GameOverPanel");
        
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
            
            // Find text components if not assigned
            if (gameOverText == null)
                gameOverText = gameOverPanel.GetComponentInChildren<TextMeshProUGUI>();
            if (finalScoreText == null)
            {
                var texts = gameOverPanel.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length > 1)
                    finalScoreText = texts[1];
            }
            
            // Find buttons
            if (restartButton == null || quitButton == null)
            {
                var buttons = gameOverPanel.GetComponentsInChildren<Button>();
                if (buttons.Length >= 2)
                {
                    restartButton = buttons[0];
                    quitButton = buttons[1];
                }
            }
        }

        // Setup button listeners
        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);

        // Check if score was saved in PlayerPrefs (when coming from GamePlay scene)
        int savedScore = PlayerPrefs.GetInt("GameOverScore", -1);
        if (savedScore >= 0)
        {
            Debug.Log($"[GameOverManager] Retrieved saved score from PlayerPrefs: {savedScore}");
            ShowGameOverScreenWithScore(savedScore);
        }
        else
        {
            // Subscribe to player death event (fallback for same-scene deaths)
            if (Player.Instance != null)
                Player.Instance.OnDeath += ShowGameOverScreen;
        }
    }

    private void OnDestroy()
    {
        if (Player.Instance != null)
            Player.Instance.OnDeath -= ShowGameOverScreen;
    }

    /// <summary>
    /// Show game over screen when player dies (gets score from ScoreManager)
    /// </summary>
    public void ShowGameOverScreen()
    {
        if (isGameOver) return;
        isGameOver = true;

        Debug.Log("[GameOverManager] ShowGameOverScreen");

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            Time.timeScale = 0f; // Pause game
        }

        // Update score display from ScoreManager
        if (finalScoreText != null && ScoreManager.Instance != null)
            finalScoreText.text = $"Итоговый счет: {ScoreManager.Instance.CurrentScore}";

        if (gameOverText != null)
            gameOverText.text = "YOU DIED!";
    }

    /// <summary>
    /// Show game over screen with score from PlayerPrefs (when loading GameOver scene)
    /// </summary>
    private void ShowGameOverScreenWithScore(int score)
    {
        if (isGameOver) return;
        isGameOver = true;

        Debug.Log($"[GameOverManager] ShowGameOverScreenWithScore: {score}");

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            Time.timeScale = 0f; // Pause game
        }

        // Update score display with saved score from PlayerPrefs
        if (finalScoreText != null)
            finalScoreText.text = $"Итоговый счет: {score}";

        if (gameOverText != null)
            gameOverText.text = "YOU DIED!";
    }

    /// <summary>
    /// Restart the game (reload current scene)
    /// </summary>
    private void OnRestartClicked()
    {
        Debug.Log("[GameOverManager] Restart clicked");
        Time.timeScale = 1f; // Resume time
        
        // Clear the saved score from PlayerPrefs for new game
        PlayerPrefs.DeleteKey("GameOverScore");
        PlayerPrefs.Save();
        
        UnityEngine.SceneManagement.SceneManager.LoadScene(3); // Load main gameplay scene
    }

    /// <summary>
    /// Quit the game
    /// </summary>
    private void OnQuitClicked()
    {
        Debug.Log("[GameOverManager] Quit clicked");
        Time.timeScale = 1f; // Resume time
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
