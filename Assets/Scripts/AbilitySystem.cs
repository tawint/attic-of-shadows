using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Система способностей игрока (Q, W, E, R)
/// Способности разблокируются после каждой пятой волны (после убийства босса)
/// </summary>
public class AbilitySystem : MonoBehaviour
{
    public static AbilitySystem Instance { get; private set; }

    [Header("Ability Q - Heal")]
    [SerializeField] private GameObject ability_Q;
    [SerializeField] private float abilityQ_Cooldown = 10f;
    [SerializeField] private float abilityQ_DisabledAlpha = 0.4f;
    private bool abilityQ_OnCooldown = false;
    private CanvasGroup abilityQ_CanvasGroup;
    private Button abilityQ_Button;
    private Image abilityQ_Image;
    private TextMeshProUGUI abilityQ_CooldownTimer;

    [Header("Ability W - Auto destroy HorizontalLines")]
    [SerializeField] private GameObject ability_W;
    [SerializeField] private float abilityW_Duration = 5f;
    [SerializeField] private float abilityW_Cooldown = 10f;
    private bool abilityW_Unlocked = false;
    private bool abilityW_OnCooldown = false;
    private CanvasGroup abilityW_CanvasGroup;
    private Button abilityW_Button;
    private Image abilityW_Image;
    private TextMeshProUGUI abilityW_CooldownTimer;

    [Header("Ability E - Auto destroy VerticalLines")]
    [SerializeField] private GameObject ability_E;
    [SerializeField] private float abilityE_Duration = 5f;
    [SerializeField] private float abilityE_Cooldown = 10f;
    private bool abilityE_Unlocked = false;
    private bool abilityE_OnCooldown = false;
    private CanvasGroup abilityE_CanvasGroup;
    private Button abilityE_Button;
    private Image abilityE_Image;
    private TextMeshProUGUI abilityE_CooldownTimer;

    [Header("Ability R - Invincibility")]
    [SerializeField] private GameObject ability_R;
    [SerializeField] private float abilityR_Duration = 5f;
    [SerializeField] private float abilityR_Cooldown = 20f;
    private bool abilityR_Unlocked = false;
    private bool abilityR_OnCooldown = false;
    private CanvasGroup abilityR_CanvasGroup;
    private Button abilityR_Button;
    private Image abilityR_Image;
    private TextMeshProUGUI abilityR_CooldownTimer;

    [Header("UI Settings")]
    [SerializeField] private float disabledAlpha = 0.4f;
    
    [SerializeField] private GameObject animationAbility;


    // Способности в активном состоянии
    private bool horizontalLineAutoDestroy_Active = false;
    private bool verticalLineAutoDestroy_Active = false;
    private bool invincibility_Active = false;

    // Cooldown timers
    private float abilityQ_CooldownRemaining = 0f;
    private float abilityW_CooldownRemaining = 0f;
    private float abilityE_CooldownRemaining = 0f;
    private float abilityR_CooldownRemaining = 0f;

    public bool IsHorizontalLineAutoDestroyActive => horizontalLineAutoDestroy_Active;
    public bool IsVerticalLineAutoDestroyActive => verticalLineAutoDestroy_Active;
    public bool IsInvincibilityActive => invincibility_Active;
    public bool IsAbilityQOnCooldown => abilityQ_OnCooldown;

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
        Debug.Log("[AbilitySystem] Start");
        SetupAbilityQ();
        SetupAbilityW();
        SetupAbilityE();
        SetupAbilityR();

        // Подписываемся на событие волны
        if (WaveManager.Instance != null)
            WaveManager.Instance.OnWaveCompleted += OnWaveCompleted;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
            TryActivateAbilityQ();
        if (Input.GetKeyDown(KeyCode.W))
            TryActivateAbilityW();
        if (Input.GetKeyDown(KeyCode.E))
            TryActivateAbilityE();
        if (Input.GetKeyDown(KeyCode.R))
            TryActivateAbilityR();

        // Update cooldown timers
        UpdateCooldownTimers();
    }

    private void OnDestroy()
    {
        if (WaveManager.Instance != null)
            WaveManager.Instance.OnWaveCompleted -= OnWaveCompleted;
    }

    private void UpdateCooldownTimers()
    {
        if (abilityQ_OnCooldown)
        {
            abilityQ_CooldownRemaining = Mathf.Max(0f, abilityQ_CooldownRemaining - Time.deltaTime);
            if (abilityQ_CooldownTimer != null)
                abilityQ_CooldownTimer.text = abilityQ_CooldownRemaining > 0 ? abilityQ_CooldownRemaining.ToString("F1") : "";
        }

        if (abilityW_OnCooldown)
        {
            abilityW_CooldownRemaining = Mathf.Max(0f, abilityW_CooldownRemaining - Time.deltaTime);
            if (abilityW_CooldownTimer != null)
                abilityW_CooldownTimer.text = abilityW_CooldownRemaining > 0 ? abilityW_CooldownRemaining.ToString("F1") : "";
        }

        if (abilityE_OnCooldown)
        {
            abilityE_CooldownRemaining = Mathf.Max(0f, abilityE_CooldownRemaining - Time.deltaTime);
            if (abilityE_CooldownTimer != null)
                abilityE_CooldownTimer.text = abilityE_CooldownRemaining > 0 ? abilityE_CooldownRemaining.ToString("F1") : "";
        }

        if (abilityR_OnCooldown)
        {
            abilityR_CooldownRemaining = Mathf.Max(0f, abilityR_CooldownRemaining - Time.deltaTime);
            if (abilityR_CooldownTimer != null)
                abilityR_CooldownTimer.text = abilityR_CooldownRemaining > 0 ? abilityR_CooldownRemaining.ToString("F1") : "";
        }
    }

    /// <summary>
    /// Вызывается после завершения каждой волны
    /// Разблокирует новую способность после каждой волны, кратной 5 (после босса)
    /// </summary>
    private void OnWaveCompleted(int waveNumber)
    {
        if (waveNumber % 5 == 0)
        {
            if (waveNumber == 5)
            {
                UnlockAbilityW();
            }
            else if (waveNumber == 10)
            {
                UnlockAbilityE();
            }
            else if (waveNumber == 15)
            {
                UnlockAbilityR();
            }
        }
    }

    /// <summary>
    /// Настройка способности Q (хил)
    /// </summary>
    private void SetupAbilityQ()
    {
        if (ability_Q == null)
            ability_Q = GameObject.Find("ability_Q");
        if (ability_Q == null)
        {
            Debug.LogWarning("[AbilitySystem] ability_Q not found");
            return;
        }

        abilityQ_CanvasGroup = ability_Q.GetComponent<CanvasGroup>();
        if (abilityQ_CanvasGroup == null)
            abilityQ_CanvasGroup = ability_Q.AddComponent<CanvasGroup>();

        abilityQ_Image = ability_Q.GetComponent<Image>();
        if (abilityQ_Image == null)
            abilityQ_Image = ability_Q.GetComponentInChildren<Image>(true);

        abilityQ_Button = ability_Q.GetComponent<Button>();
        if (abilityQ_Button != null)
            abilityQ_Button.onClick.AddListener(TryActivateAbilityQ);

        // Элемент текста для отображения кулдауна
        var timerObj = ability_Q.transform.Find("ability_Q_CooldownTimer");
        if (timerObj != null)
            abilityQ_CooldownTimer = timerObj.GetComponent<TextMeshProUGUI>();
    }

    /// <summary>
    /// Настройка способности W (автоуничтожение горизонтальных линий)
    /// </summary>
    private void SetupAbilityW()
    {
        if (ability_W == null)
            ability_W = GameObject.Find("ability_W");
        if (ability_W == null)
        {
            Debug.LogWarning("[AbilitySystem] ability_W not found");
            return;
        }

        abilityW_CanvasGroup = ability_W.GetComponent<CanvasGroup>();
        if (abilityW_CanvasGroup == null)
            abilityW_CanvasGroup = ability_W.AddComponent<CanvasGroup>();

        abilityW_Image = ability_W.GetComponent<Image>();
        if (abilityW_Image == null)
            abilityW_Image = ability_W.GetComponentInChildren<Image>(true);

        abilityW_Button = ability_W.GetComponent<Button>();
        if (abilityW_Button != null)
            abilityW_Button.onClick.AddListener(TryActivateAbilityW);

        // Элемент текста для отображения кулдауна
        var timerObj = ability_W.transform.Find("ability_W_CooldownTimer");
        if (timerObj != null)
            abilityW_CooldownTimer = timerObj.GetComponent<TextMeshProUGUI>();

        // Изначально заблокирована
        SetAbilityWLocked(true);
    }

    /// <summary>
    /// Настройка способности E (автоуничтожение вертикальных линий)
    /// </summary>
    private void SetupAbilityE()
    {
        if (ability_E == null)
            ability_E = GameObject.Find("ability_E");
        if (ability_E == null)
        {
            Debug.LogWarning("[AbilitySystem] ability_E not found");
            return;
        }

        abilityE_CanvasGroup = ability_E.GetComponent<CanvasGroup>();
        if (abilityE_CanvasGroup == null)
            abilityE_CanvasGroup = ability_E.AddComponent<CanvasGroup>();

        abilityE_Image = ability_E.GetComponent<Image>();
        if (abilityE_Image == null)
            abilityE_Image = ability_E.GetComponentInChildren<Image>(true);

        abilityE_Button = ability_E.GetComponent<Button>();
        if (abilityE_Button != null)
            abilityE_Button.onClick.AddListener(TryActivateAbilityE);

        // Элемент текста для отображения кулдауна
        var timerObj = ability_E.transform.Find("ability_E_CooldownTimer");
        if (timerObj != null)
            abilityE_CooldownTimer = timerObj.GetComponent<TextMeshProUGUI>();

        // Изначально заблокирована
        SetAbilityELocked(true);
    }

    /// <summary>
    /// Настройка способности R (неуязвимость)
    /// </summary>
    private void SetupAbilityR()
    {
        if (ability_R == null)
            ability_R = GameObject.Find("ability_R");
        if (ability_R == null)
        {
            Debug.LogWarning("[AbilitySystem] ability_R not found");
            return;
        }

        abilityR_CanvasGroup = ability_R.GetComponent<CanvasGroup>();
        if (abilityR_CanvasGroup == null)
            abilityR_CanvasGroup = ability_R.AddComponent<CanvasGroup>();

        abilityR_Image = ability_R.GetComponent<Image>();
        if (abilityR_Image == null)
            abilityR_Image = ability_R.GetComponentInChildren<Image>(true);

        abilityR_Button = ability_R.GetComponent<Button>();
        if (abilityR_Button != null)
            abilityR_Button.onClick.AddListener(TryActivateAbilityR);

        // Элемент текста для отображения кулдауна
        var timerObj = ability_R.transform.Find("ability_R_CooldownTimer");
        if (timerObj != null)
            abilityR_CooldownTimer = timerObj.GetComponent<TextMeshProUGUI>();

        // Изначально заблокирована
        SetAbilityRLocked(true);
    }

    /// <summary>
    /// Попытка активировать способность Q (хил)
    /// </summary>
    private void TryActivateAbilityQ()
    {
        if (abilityQ_OnCooldown)
        {
            Debug.Log("[AbilitySystem] Ability Q is on cooldown");
            return;
        }

        if (Player.Instance == null || !Player.Instance.IsAlive)
            return;

        Debug.Log("[AbilitySystem] Ability Q activated!");
        animationAbility.SetActive(true);
        StartCoroutine(ActivateAbilityQ());
    }

    /// <summary>
    /// Активация способности Q (хил, установка кулдауна)
    /// </summary>
    private IEnumerator ActivateAbilityQ()
    {
        
        // Лечение игрока
        int healAmount = Mathf.Max(1, Mathf.RoundToInt(Player.Instance.MaxHealth * 0.5f));
        Player.Instance.Heal(healAmount);

        // Установка кулдауна
        abilityQ_OnCooldown = true;
        abilityQ_CooldownRemaining = abilityQ_Cooldown;
        SetAbilityQCooldown(true);
        yield return new WaitForSeconds(1f);
        animationAbility.SetActive(false);
        yield return new WaitForSeconds(abilityQ_Cooldown);

        abilityQ_OnCooldown = false;
        SetAbilityQCooldown(false);
        Debug.Log("[AbilitySystem] Ability Q cooldown finished");
    }

    /// <summary>
    /// Попытка активировать способность W
    /// </summary>
    private void TryActivateAbilityW()
    {
        if (!abilityW_Unlocked || abilityW_OnCooldown)
        {
            Debug.Log("[AbilitySystem] Ability W is locked or on cooldown");
            return;
        }

        Debug.Log("[AbilitySystem] Ability W activated!");
        animationAbility.SetActive(true);
        StartCoroutine(ActivateAbilityW());
    }

    /// <summary>
    /// Активация способности W (5 сек автоуничтожение горизонтальных линий)
    /// </summary>
    private IEnumerator ActivateAbilityW()
    {
        horizontalLineAutoDestroy_Active = true;
        abilityW_OnCooldown = true;
        abilityW_CooldownRemaining = abilityW_Cooldown + abilityW_Duration;

        // Визуальный эффект - кнопка полупрозрачная
        SetAbilityWCooldown(true);

        // Сразу удаляем все текущие горизонтальные линии
        if (WaveManager.Instance != null)
            WaveManager.Instance.AutoDestroyHorizontalLines();
        yield return new WaitForSeconds(1f);
        animationAbility.SetActive(false);
        yield return new WaitForSeconds(abilityW_Duration);

        horizontalLineAutoDestroy_Active = false;
        Debug.Log("[AbilitySystem] Ability W effect ended");

        yield return new WaitForSeconds(abilityW_Cooldown);

        abilityW_OnCooldown = false;
        SetAbilityWCooldown(false);
        Debug.Log("[AbilitySystem] Ability W cooldown finished");
    }

    /// <summary>
    /// Попытка активировать способность E
    /// </summary>
    private void TryActivateAbilityE()
    {
        if (!abilityE_Unlocked || abilityE_OnCooldown)
        {
            Debug.Log("[AbilitySystem] Ability E is locked or on cooldown");
            return;
        }

        Debug.Log("[AbilitySystem] Ability E activated!");
        animationAbility.SetActive(true);   
        StartCoroutine(ActivateAbilityE());
    }

    /// <summary>
    /// Активация способности E (5 сек автоуничтожение вертикальных линий)
    /// </summary>
    private IEnumerator ActivateAbilityE()
    {
        verticalLineAutoDestroy_Active = true;
        abilityE_OnCooldown = true;
        abilityE_CooldownRemaining = abilityE_Cooldown + abilityE_Duration;

        // Визуальный эффект - кнопка полупрозрачная
        SetAbilityECooldown(true);

        // Сразу удаляем все текущие вертикальные линии
        if (WaveManager.Instance != null)
            WaveManager.Instance.AutoDestroyVerticalLines();

        yield return new WaitForSeconds(1f);
        animationAbility.SetActive(false);

        yield return new WaitForSeconds(abilityE_Duration);

        verticalLineAutoDestroy_Active = false;
        Debug.Log("[AbilitySystem] Ability E effect ended");

        yield return new WaitForSeconds(abilityE_Cooldown);

        abilityE_OnCooldown = false;
        SetAbilityECooldown(false);
        Debug.Log("[AbilitySystem] Ability E cooldown finished");
    }

    /// <summary>
    /// Попытка активировать способность R
    /// </summary>
    private void TryActivateAbilityR()
    {
        if (!abilityR_Unlocked || abilityR_OnCooldown)
        {
            Debug.Log("[AbilitySystem] Ability R is locked or on cooldown");
            return;
        }

        Debug.Log("[AbilitySystem] Ability R activated!");
        animationAbility.SetActive(true);   
        StartCoroutine(ActivateAbilityR());
    }

    /// <summary>
    /// Активация способности R (5 сек неуязвимость)
    /// </summary>
    private IEnumerator ActivateAbilityR()
    {
        invincibility_Active = true;
        abilityR_OnCooldown = true;
        abilityR_CooldownRemaining = abilityR_Cooldown + abilityR_Duration;

        // Визуальный эффект - кнопка полупрозрачная
        animationAbility.SetActive(true);
        SetAbilityRCooldown(true);

        yield return new WaitForSeconds(1f);
        animationAbility.SetActive(false);
        yield return new WaitForSeconds(abilityR_Duration);

        invincibility_Active = false;
        Debug.Log("[AbilitySystem] Ability R effect ended");



        yield return new WaitForSeconds(abilityR_Cooldown);

        abilityR_OnCooldown = false;
        SetAbilityRCooldown(false);
        
        Debug.Log("[AbilitySystem] Ability R cooldown finished");
    }

    /// <summary>
    /// Установить статус кулдауна способности Q
    /// </summary>
    private void SetAbilityQCooldown(bool onCooldown)
    {
        if (ability_Q == null) return;
        if (abilityQ_CanvasGroup != null)
            abilityQ_CanvasGroup.alpha = onCooldown ? abilityQ_DisabledAlpha : 1f;
        if (abilityQ_Button != null)
            abilityQ_Button.interactable = !onCooldown;
        if (abilityQ_CooldownTimer != null)
            abilityQ_CooldownTimer.gameObject.SetActive(onCooldown);
    }

    /// <summary>
    /// Разблокировать способность W
    /// </summary>
    private void UnlockAbilityW()
    {
        if (abilityW_Unlocked) return;
        abilityW_Unlocked = true;
        SetAbilityWLocked(false);
        Debug.Log("[AbilitySystem] Ability W unlocked! (Wave 5)");
    }

    /// <summary>
    /// Установить статус блокировки/разблокировки способности W
    /// </summary>
    private void SetAbilityWLocked(bool locked)
    {
        if (ability_W == null) return;
        if (abilityW_CanvasGroup != null)
            abilityW_CanvasGroup.alpha = locked ? disabledAlpha : 1f;
        if (abilityW_Button != null)
            abilityW_Button.interactable = !locked;
    }

    /// <summary>
    /// Установить статус кулдауна способности W
    /// </summary>
    private void SetAbilityWCooldown(bool onCooldown)
    {
        if (ability_W == null) return;
        if (abilityW_CanvasGroup != null)
            abilityW_CanvasGroup.alpha = onCooldown ? disabledAlpha : 1f;
        if (abilityW_Button != null)
            abilityW_Button.interactable = !onCooldown;
        if (abilityW_CooldownTimer != null)
            abilityW_CooldownTimer.gameObject.SetActive(onCooldown);
    }

    /// <summary>
    /// Разблокировать способность E
    /// </summary>
    private void UnlockAbilityE()
    {
        if (abilityE_Unlocked) return;
        abilityE_Unlocked = true;
        SetAbilityELocked(false);
        Debug.Log("[AbilitySystem] Ability E unlocked! (Wave 10)");
    }

    /// <summary>
    /// Установить статус блокировки/разблокировки способности E
    /// </summary>
    private void SetAbilityELocked(bool locked)
    {
        if (ability_E == null) return;
        if (abilityE_CanvasGroup != null)
            abilityE_CanvasGroup.alpha = locked ? disabledAlpha : 1f;
        if (abilityE_Button != null)
            abilityE_Button.interactable = !locked;
    }

    /// <summary>
    /// Установить статус кулдауна способности E
    /// </summary>
    private void SetAbilityECooldown(bool onCooldown)
    {
        if (ability_E == null) return;
        if (abilityE_CanvasGroup != null)
            abilityE_CanvasGroup.alpha = onCooldown ? disabledAlpha : 1f;
        if (abilityE_Button != null)
            abilityE_Button.interactable = !onCooldown;
        if (abilityE_CooldownTimer != null)
            abilityE_CooldownTimer.gameObject.SetActive(onCooldown);
    }

    /// <summary>
    /// Разблокировать способность R
    /// </summary>
    private void UnlockAbilityR()
    {
        if (abilityR_Unlocked) return;
        abilityR_Unlocked = true;
        SetAbilityRLocked(false);
        Debug.Log("[AbilitySystem] Ability R unlocked! (Wave 15)");
    }

    /// <summary>
    /// Установить статус блокировки/разблокировки способности R
    /// </summary>
    private void SetAbilityRLocked(bool locked)
    {
        if (ability_R == null) return;
        if (abilityR_CanvasGroup != null)
            abilityR_CanvasGroup.alpha = locked ? disabledAlpha : 1f;
        if (abilityR_Button != null)
            abilityR_Button.interactable = !locked;
    }

    /// <summary>
    /// Установить статус кулдауна способности R
    /// </summary>
    private void SetAbilityRCooldown(bool onCooldown)
    {
        if (ability_R == null) return;
        if (abilityR_CanvasGroup != null)
            abilityR_CanvasGroup.alpha = onCooldown ? disabledAlpha : 1f;
        if (abilityR_Button != null)
            abilityR_Button.interactable = !onCooldown;
        if (abilityR_CooldownTimer != null)
            abilityR_CooldownTimer.gameObject.SetActive(onCooldown);
    }

    /// <summary>
    /// Сбросить все способности (при новой игре)
    /// </summary>
    public void ResetAbilities()
    {
        abilityQ_OnCooldown = false;
        SetAbilityQCooldown(false);

        abilityW_Unlocked = false;
        abilityW_OnCooldown = false;
        horizontalLineAutoDestroy_Active = false;
        SetAbilityWLocked(true);

        abilityE_Unlocked = false;
        abilityE_OnCooldown = false;
        verticalLineAutoDestroy_Active = false;
        SetAbilityELocked(true);

        abilityR_Unlocked = false;
        abilityR_OnCooldown = false;
        invincibility_Active = false;
        SetAbilityRLocked(true);

        StopAllCoroutines();
        Debug.Log("[AbilitySystem] All abilities reset");
    }
}
