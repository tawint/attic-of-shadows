using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }

    [Header("Health (7 HP total, 1 per hit)")]
    [SerializeField] private int maxHealth = 7;
    [SerializeField] private int currentHealth;
    [Tooltip("Sprite for one HP segment (titleSprite_16). Seven segments are created inside GameObject 'HpBar'.")]
    [SerializeField] private Sprite hpBarSegmentSprite;
    [SerializeField] private Transform hpBarRoot;
    [Tooltip("Шаг по X между сегментами.")]
    [SerializeField] private float hpSegmentSpacing = 14f;
    [Tooltip("Смещение полоски HP по X (отрицательное — влево, положительное — вправо).")]
    [SerializeField] private float hpBarOffsetX = 0f;

    [Header("Damage effect")]
    [SerializeField] private float invincibilityTime = 0.5f;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color damageTintColor = new Color(1f, 0.4f, 0.4f);
    [SerializeField] private float damageTintDuration = 0.2f;

    [Header("Icon in IconPlayer by HP: full=6, mid=17, low=29")]
    [SerializeField] private SpriteRenderer iconPlayerSprite;
    [SerializeField] private Sprite iconFullHp;   // titleSprite_6
    [SerializeField] private Sprite iconMidHp;   // titleSprite_17
    [SerializeField] private Sprite iconLowHp;   // titleSprite_29

    [Header("Animator (vertical / horizontal swing after spell)")]
    [SerializeField] private Animator animator;
    private static readonly int IsFertAttack = Animator.StringToHash("IsFertAttack");
    private static readonly int IsGorizAttack = Animator.StringToHash("IsGorizAttack");
    private bool nextSwingIsVertical = true;

    [Header("Symbol Display (over head)")]
    [SerializeField] private SpriteRenderer playerSymbolDisplay;
    [SerializeField] private float symbolDisplayDuration = 1f;
    [SerializeField] private Sprite verticalLineSprite;
    [SerializeField] private Sprite horizontalLineSprite;
    [SerializeField] private Sprite checkmarkUpSprite;     // ^
    [SerializeField] private Sprite checkmarkDownSprite;   // V
    [SerializeField] private Sprite angleLeftSprite;       // <
    [SerializeField] private Sprite angleRightSprite;      // >
    [SerializeField] private Sprite starSprite;
    [SerializeField] private Sprite circleSprite;
    [SerializeField] private Sprite spiralSprite;

    private bool isInvincible = false;
    private Image[] hpBarSegments;

    public System.Action<int, int> OnHealthChanged;
    /// <summary>Вызывается, когда HP опустились до 0 (смерть игрока).</summary>
    public System.Action OnPlayerDied;
    /// <summary>То же, что OnPlayerDied — событие смерти при HP = 0. Удобно для подписки на факт смерти.</summary>
    public System.Action OnDeath;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsAlive => currentHealth > 0;

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
        Debug.Log("[Player] Start begin");
        currentHealth = maxHealth;
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        SetupHpBar();
        SetupIconPlayer();
        SetupSymbolDisplay();
        UpdateHealthUI();
        Debug.Log("[Player] Start end");
    }

    private void Update()
    {
    }

    private void SetupHpBar()
    {
        Transform hpBarTransform = hpBarRoot != null ? hpBarRoot : FindObjectByName("HpBar");
        if (hpBarTransform == null)
        {
            Debug.LogWarning("[Player] HpBar not found. Assign hpBarRoot or create GameObject named 'HpBar'.");
            return;
        }

        if (hpBarSegmentSprite == null)
        {
            Debug.LogWarning("[Player] hpBarSegmentSprite (titleSprite_16) not assigned. Assign in Inspector.");
            return;
        }

        hpBarSegments = new Image[maxHealth];
        for (int i = 0; i < maxHealth; i++)
        {
            var go = new GameObject($"HpSegment_{i}");
            go.transform.SetParent(hpBarTransform, false);
            var img = go.AddComponent<Image>();
            img.sprite = hpBarSegmentSprite;
            img.color = Color.white;
            var rt = img.rectTransform;
            rt.anchorMin = new Vector2(0, 0.5f);
            rt.anchorMax = new Vector2(0, 0.5f);
            rt.pivot = new Vector2(0, 0.5f);
            rt.anchoredPosition = new Vector2(hpBarOffsetX + i * hpSegmentSpacing, 0f);
            rt.sizeDelta = new Vector2(0.2f, 0.4f);
            hpBarSegments[i] = img;
        }
    }

    private void SetupIconPlayer()
    {
        if (iconPlayerSprite == null)
        {
            var iconGo = GameObject.Find("IconPlayer");
            if (iconGo != null)
            {
                iconPlayerSprite = iconGo.GetComponent<SpriteRenderer>();
                Debug.Log($"[Player] IconPlayer found: {iconGo.name}, has SpriteRenderer={iconPlayerSprite != null}");
            }
            else
                Debug.Log("[Player] IconPlayer not found (GameObject.Find returned null)");
        }
        else
            Debug.Log($"[Player] IconPlayer sprite assigned: {iconPlayerSprite.gameObject.name}");
        UpdateIconByHp();
    }

    private void SetupSymbolDisplay()
    {
        if (playerSymbolDisplay == null)
        {
            var symbolGo = GameObject.Find("PlayerSymbolDisplay");
            if (symbolGo != null)
            {
                playerSymbolDisplay = symbolGo.GetComponent<SpriteRenderer>();
                Debug.Log($"[Player] PlayerSymbolDisplay found: {symbolGo.name}");
            }
            else
                Debug.Log("[Player] PlayerSymbolDisplay not found (GameObject.Find returned null)");
        }
        
        if (playerSymbolDisplay != null)
        {
            playerSymbolDisplay.gameObject.SetActive(false);
        }
    }

    private static string GetPath(Transform t)
    {
        if (t == null) return "";
        var p = t.parent != null ? GetPath(t.parent) + "/" : "";
        return p + t.name;
    }

    private Transform FindObjectByName(string name)
    {
        var go = GameObject.Find(name);
        return go != null ? go.transform : null;
    }

    /// <summary>
    /// Показывает спрайт символа над головой игрока на указанное время
    /// </summary>
    public void ShowSymbol(string symbolName)
    {
        if (playerSymbolDisplay == null)
        {
            Debug.LogWarning("[Player] PlayerSymbolDisplay not assigned!");
            return;
        }

        Sprite symbolSprite = GetSpriteForSymbol(symbolName);
        if (symbolSprite != null)
        {
            playerSymbolDisplay.sprite = symbolSprite;
            playerSymbolDisplay.gameObject.SetActive(true);
            StartCoroutine(HideSymbolDisplay());
        }
    }

    private Sprite GetSpriteForSymbol(string symbol)
    {
        switch (symbol)
        {
            case "VerticalLine": return verticalLineSprite;
            case "HorizontalLine": return horizontalLineSprite;
            case "^": return checkmarkUpSprite;
            case "V": return checkmarkDownSprite;
            case "<": return angleLeftSprite;
            case ">": return angleRightSprite;
            case "Star": return starSprite;
            case "Circle": return circleSprite;
            case "Spiral": return spiralSprite;
            default: return null;
        }
    }

    private System.Collections.IEnumerator HideSymbolDisplay()
    {
        yield return new WaitForSeconds(symbolDisplayDuration);
        if (playerSymbolDisplay != null)
            playerSymbolDisplay.gameObject.SetActive(false);
    }

    public void TakeDamage(float damage)
    {
        // Проверяем неуязвимость от способности R
        if (AbilitySystem.Instance != null && AbilitySystem.Instance.IsInvincibilityActive)
        {
            Debug.Log("[Player] Damage blocked by Ability R invincibility!");
            return;
        }

        if (isInvincible || !IsAlive) return;

        currentHealth -= Mathf.RoundToInt(damage);
        currentHealth = Mathf.Max(0, currentHealth);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        UpdateHealthUI();
        ApplyDamageTint();

        if (currentHealth <= 0)
            Die();
        else
            StartCoroutine(InvincibilityFrames());
    }

    private void ApplyDamageTint()
    {
        if (spriteRenderer == null) return;
        StartCoroutine(DamageTintRoutine());
    }

    private IEnumerator DamageTintRoutine()
    {
        var orig = spriteRenderer.color;
        spriteRenderer.color = damageTintColor;
        yield return new WaitForSeconds(damageTintDuration);
        if (spriteRenderer != null)
            spriteRenderer.color = orig;
    }

    private IEnumerator InvincibilityFrames()
    {
        isInvincible = true;
        float elapsed = 0f;
        while (elapsed < invincibilityTime && spriteRenderer != null)
        {
            spriteRenderer.enabled = !spriteRenderer.enabled;
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }
        if (spriteRenderer != null)
            spriteRenderer.enabled = true;
        isInvincible = false;
    }

    public void Heal(int amount)
    {
        if (!IsAlive) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        UpdateHealthUI();
    }

    private void UpdateHealthUI()
    {
        if (hpBarSegments != null)
        {
            for (int i = 0; i < hpBarSegments.Length; i++)
            {
                if (hpBarSegments[i] != null)
                    hpBarSegments[i].color = i < currentHealth ? Color.white : new Color(0.4f, 0.4f, 0.4f, 0.6f);
            }
        }
        UpdateIconByHp();
    }

    private void UpdateIconByHp()
    {
        if (iconPlayerSprite == null) return;
        if (currentHealth >= maxHealth && iconFullHp != null)
            iconPlayerSprite.sprite = iconFullHp;
        else if (currentHealth >= 5 && iconMidHp != null)
            iconPlayerSprite.sprite = iconMidHp;
        else if (currentHealth <= 2 && iconLowHp != null)
            iconPlayerSprite.sprite = iconLowHp;
        else if (iconMidHp != null)
            iconPlayerSprite.sprite = iconMidHp;
    }

    /// <summary>
    /// Вызывать после каждого распознанного жеста: по очереди вертикальная атака -> горизонтальная атака -> вертикальная...
    /// </summary>
    public void PlayNextSpellAnimation()
    {
        if (animator == null) return;
        bool useVertical = nextSwingIsVertical;
        nextSwingIsVertical = !nextSwingIsVertical;
        StartCoroutine(SetAttackTriggerNextFrame(useVertical));
    }

    private IEnumerator SetAttackTriggerNextFrame(bool vertical)
    {
        animator.ResetTrigger(IsFertAttack);
        animator.ResetTrigger(IsGorizAttack);
        yield return null;
        if (animator == null) yield break;
        if (vertical)
            animator.SetTrigger(IsFertAttack);
        else
            animator.SetTrigger(IsGorizAttack);
    }

    private void Die()
    {
        Debug.Log("[Player] PLAYER DIED! HP = 0");
        
        // Save current score before switching scene
        if (ScoreManager.Instance != null)
        {
            PlayerPrefs.SetInt("GameOverScore", ScoreManager.Instance.CurrentScore);
            PlayerPrefs.Save();
            Debug.Log($"[Player] Saved score {ScoreManager.Instance.CurrentScore} for GameOver scene");
            ScoreManager.Instance.ResetCombo();
        }
        
        OnDeath?.Invoke();
        OnPlayerDied?.Invoke();
        
        SceneManager.LoadScene(3);
        StartCoroutine(Respawn());
    }

    private IEnumerator Respawn()
    {
        yield return new WaitForSeconds(2f);
        currentHealth = maxHealth;
        UpdateHealthUI();
    }

    public void ResetPlayer()
    {
        currentHealth = maxHealth;
        isInvincible = false;
        nextSwingIsVertical = true;
        UpdateHealthUI();
    }
}
