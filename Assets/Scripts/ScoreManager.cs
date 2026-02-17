using System.Collections;
using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI comboText;
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private TextMeshProUGUI highScoreText;

    [Header("Combo Settings")]
    [SerializeField] private float comboTimeWindow = 2f;      // Time to continue combo
    [SerializeField] private int maxComboMultiplier = 10;     // Maximum combo multiplier

    [Header("Score Animation")]
    [SerializeField] private float scoreAnimationSpeed = 5f;

    // Current state
    private int currentScore = 0;
    private int displayedScore = 0;
    private int highScore = 0;
    private int comboCount = 0;
    private int comboMultiplier = 1;
    private float lastKillTime = 0f;
    private Coroutine comboDecayCoroutine;

    // Properties
    public int CurrentScore => currentScore;
    public int ComboMultiplier => comboMultiplier;
    public int ComboCount => comboCount;

    // Events
    public System.Action<int> OnScoreChanged;
    public System.Action<int, int> OnComboChanged;  // (count, multiplier)
    public System.Action OnComboLost;

    private void Awake()
    {
        Debug.Log($"[ScoreManager] Awake: {gameObject.name}");
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Load high score
        highScore = PlayerPrefs.GetInt("HighScore", 0);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        Debug.Log("[ScoreManager] Start");
        UpdateUI();

        PlayerPrefs.DeleteKey("GameOverScore");
        PlayerPrefs.DeleteKey("HighScore");

        // Subscribe to wave events
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveStarted += OnWaveStarted;
            WaveManager.Instance.OnWaveCompleted += OnWaveCompleted;
        }
    }

    private void Update()
    {
        // Animate score display
        if (displayedScore != currentScore)
        {
            displayedScore = (int)Mathf.MoveTowards(displayedScore, currentScore, scoreAnimationSpeed * Time.deltaTime * Mathf.Max(100, Mathf.Abs(currentScore - displayedScore)));
        }

        // Always refresh score text so it never "disappears" (e.g. if reference was lost and restored, or canvas re-enabled)
        UpdateScoreDisplay();

        // Check combo timeout
        if (comboCount > 0 && Time.time - lastKillTime > comboTimeWindow)
        {
            ResetCombo();
        }
    }

    /// <summary>
    /// Add score with combo multiplier
    /// </summary>
    public void AddScore(int baseScore)
    {
        // Apply combo multiplier
        int finalScore = baseScore * comboMultiplier;
        currentScore += finalScore;

        // Update combo
        IncrementCombo();

        // Update high score
        if (currentScore > highScore)
        {
            highScore = currentScore;
            PlayerPrefs.SetInt("HighScore", highScore);
        }

        OnScoreChanged?.Invoke(currentScore);
        UpdateUI();

        Debug.Log($"+{finalScore} points (x{comboMultiplier} combo) | Total: {currentScore}");
    }

    /// <summary>
    /// Add score without combo (for bonuses)
    /// </summary>
    public void AddBonusScore(int score)
    {
        currentScore += score;
        OnScoreChanged?.Invoke(currentScore);
        UpdateUI();
    }

    /// <summary>
    /// Increment combo counter
    /// </summary>
    private void IncrementCombo()
    {
        float timeSinceLastKill = Time.time - lastKillTime;
        lastKillTime = Time.time;

        // If kill was fast enough, increase combo
        if (timeSinceLastKill <= comboTimeWindow || comboCount == 0)
        {
            comboCount++;
            
            // Update multiplier based on combo count
            // 1-2 kills: x1, 3-4: x2, 5-6: x3, etc.
            comboMultiplier = Mathf.Min(maxComboMultiplier, 1 + (comboCount - 1) / 2);

            OnComboChanged?.Invoke(comboCount, comboMultiplier);
            UpdateComboDisplay();

            // Restart decay timer
            if (comboDecayCoroutine != null)
                StopCoroutine(comboDecayCoroutine);
            comboDecayCoroutine = StartCoroutine(ComboDecayTimer());
        }
    }

    /// <summary>
    /// Timer for combo decay
    /// </summary>
    private IEnumerator ComboDecayTimer()
    {
        yield return new WaitForSeconds(comboTimeWindow);
        ResetCombo();
    }

    /// <summary>
    /// Reset combo to zero
    /// </summary>
    public void ResetCombo()
    {
        if (comboCount > 0)
        {
            Debug.Log($"Combo lost! Final combo: {comboCount}x");
            OnComboLost?.Invoke();
        }

        comboCount = 0;
        comboMultiplier = 1;
        OnComboChanged?.Invoke(comboCount, comboMultiplier);
        UpdateComboDisplay();

        if (comboDecayCoroutine != null)
        {
            StopCoroutine(comboDecayCoroutine);
            comboDecayCoroutine = null;
        }
    }

    /// <summary>
    /// Update all UI elements
    /// </summary>
    private void UpdateUI()
    {
        UpdateScoreDisplay();
        UpdateComboDisplay();
        UpdateWaveDisplay();
        UpdateHighScoreDisplay();
    }

    private void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Счет: {displayedScore}";
        }
    }

    private void UpdateComboDisplay()
    {
        if (comboText != null)
        {
            if (comboCount > 1)
            {
                comboText.text = $"Комбо x{comboMultiplier}\n{comboCount} hits!";
                comboText.gameObject.SetActive(true);
            }
            else
            {
                comboText.gameObject.SetActive(false);
            }
        }
    }

    private void UpdateWaveDisplay()
    {
        if (waveText != null && WaveManager.Instance != null)
        {
            waveText.text = $"Волна {WaveManager.Instance.CurrentWave}";
        }
    }

    private void UpdateHighScoreDisplay()
    {
        if (highScoreText != null)
        {
            highScoreText.text = $"Лучший: {highScore}";
        }
    }

    private void OnWaveStarted(int wave)
    {
        UpdateWaveDisplay();
    }

    private void OnWaveCompleted(int wave)
    {
        // Bonus score for completing wave
        int waveBonus = wave * 500;
        AddBonusScore(waveBonus);
        Debug.Log($"Wave {wave} completed! Bonus: +{waveBonus}");
    }

    /// <summary>
    /// Reset score (for new game)
    /// </summary>
    public void ResetScore()
    {
        currentScore = 0;
        displayedScore = 0;
        ResetCombo();
        UpdateUI();
    }
}
