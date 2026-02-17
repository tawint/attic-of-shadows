using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using TMPro;

public class Enemy : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private Transform target;
    private NavMeshAgent agent;
    private float baseSpeed = 2f;

    [Header("Type & Visual")]
    [SerializeField] private EnemyType enemyType = EnemyType.Fast;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite fastSprite;
    [SerializeField] private Sprite tankySprite;
    [SerializeField] private Sprite shootingSprite;
    [SerializeField] private Sprite bossSprite;

    [Header("Symbol")]
    [SerializeField] private List<string> requiredSymbols = new List<string> { "VerticalLine" };
    [SerializeField] private TextMeshPro symbolText;

    [Header("Stats")]
    [SerializeField] private int scoreValue = 100;
    [SerializeField] private float damageToPlayer = 1f;

    [Header("Shooting (only for Shooting type)")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpawnInterval = 2f;

    [Header("Death")]
    [Tooltip("Длительность анимации смерти перед уничтожением объекта")]
    [SerializeField] private float deathAnimationDuration = 0.5f;

    private bool isInvincible = false;
    private bool isDead = false;
    private EnemyBullet currentBullet;

    public EnemyType Type => enemyType;
    public string RequiredSymbol => requiredSymbols.Count > 0 ? requiredSymbols[0] : "";
    public List<string> RequiredSymbols => requiredSymbols;
    public int ScoreValue => scoreValue;
    public bool CanBeDamaged => !isInvincible;

    private void Start()
    {
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0)
            gameObject.layer = enemyLayer;

        // Коллайдер как триггер — мобы не сталкиваются и не блокируют друг друга, урон по игроку проходит
        var col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;

        Debug.Log($"[Enemy] Start: type={enemyType}, name={gameObject.name}");
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.updateRotation = false;
            agent.updateUpAxis = false;
        }
        else Debug.LogWarning($"[Enemy] {gameObject.name} has no NavMeshAgent!");

        if (target == null)
        {
            var player = FindObjectOfType<Player>();
            if (player != null)
                target = player.transform;
        }
        if (target == null) Debug.LogWarning($"[Enemy] {gameObject.name} has no target (player)!");

        UpdateSymbolDisplay();
        ApplyTypeSprite();
    }

    private void Update()
    {
        if (isDead) return;

        if (agent != null && agent.isOnNavMesh && target != null)
        {
            agent.SetDestination(target.position);
        }

        if (target != null)
        {
            float dist = Vector2.Distance(transform.position, target.position);
            if (dist < 0.5f)
            {
                AttackPlayer();
            }
        }
    }

    private void OnDestroy()
    {
        CancelInvoke();
        if (currentBullet != null)
        {
            currentBullet.OnDestroyed -= OnBulletDestroyed;
        }
    }

    /// <summary>
    /// Есть ли у врага этот символ (и можно ли его снять — не неуязвим).
    /// </summary>
    public bool CanBeDefeatedBy(string drawnSymbol)
    {
        if (isInvincible) return false;
        foreach (string s in requiredSymbols)
        {
            if (s == drawnSymbol) return true;
        }
        return false;
    }

    /// <summary>
    /// Снять символ только если он совпадает с ПЕРВЫМ в списке (порядок рисования: сначала первый, потом второй и т.д.).
    /// Символ пропадает над головой. Когда список пуст — враг умирает.
    /// </summary>
    public bool TryRemoveSymbol(string drawnSymbol)
    {
        if (isInvincible) return false;
        if (requiredSymbols.Count == 0) return false;
        if (requiredSymbols[0] != drawnSymbol) return false; // не тот символ или не в том порядке

        requiredSymbols.RemoveAt(0);

        UpdateSymbolDisplay();
        GetComponentInChildren<EnemySymbolDisplay>()?.UpdateDisplay();

        if (requiredSymbols.Count == 0)
        {
            OnDefeated();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Inicializaciya s parametrami
    /// </summary>
    public void Initialize(EnemyType type, List<string> symbols, float speedMultiplier, int score, Transform playerTarget)
    {
        enemyType = type;
        requiredSymbols = new List<string>(symbols);
        scoreValue = score;
        target = playerTarget;

        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        float speed = baseSpeed * speedMultiplier;
        if (type == EnemyType.Boss)
            speed *= 0.4f; // босс медленный
        if (agent != null)
            agent.speed = speed;

        Debug.Log($"[Enemy] Initialize: type={type}, symbols=[{string.Join(",", symbols)}], speed={speed}, score={score}");

        ApplyTypeSprite();
        UpdateSymbolDisplay();
        GetComponentInChildren<EnemySymbolDisplay>()?.UpdateDisplay();

        if (enemyType == EnemyType.Shooting && bulletPrefab != null)
        {
            ShootBullet();
            InvokeRepeating(nameof(ShootBullet), bulletSpawnInterval, bulletSpawnInterval);
            Debug.Log($"[Enemy] Shooting enemy: bullet prefab set, first shot now, then every {bulletSpawnInterval}s");
        }
        else if (enemyType == EnemyType.Shooting && bulletPrefab == null)
            Debug.LogWarning("[Enemy] Shooting enemy but Bullet Prefab is NULL! Assign in Inspector.");
    }

    public void SetInvincible(bool invincible)
    {
        isInvincible = invincible;
    }

    public void RegisterBullet(EnemyBullet bullet)
    {
        if (currentBullet != null)
        {
            currentBullet.OnDestroyed -= OnBulletDestroyed;
        }
        currentBullet = bullet;
        if (currentBullet != null)
        {
            currentBullet.OnDestroyed += OnBulletDestroyed;
            SetInvincible(true);
        }
    }

    private void OnBulletDestroyed()
    {
        currentBullet = null;
        SetInvincible(false);
    }

    private void ShootBullet()
    {
        if (bulletPrefab == null || currentBullet != null) return;

        GameObject bulletObj = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        EnemyBullet bullet = bulletObj.GetComponent<EnemyBullet>();
        if (bullet != null)
        {
            bullet.Initialize(target, this);
            RegisterBullet(bullet);
        }
    }

    private void ApplyTypeSprite()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogWarning($"[Enemy] {gameObject.name} has no SpriteRenderer! Sprites won't show.");
            return;
        }
        switch (enemyType)
        {
            case EnemyType.Fast: if (fastSprite != null) spriteRenderer.sprite = fastSprite; break;
            case EnemyType.Tanky: if (tankySprite != null) spriteRenderer.sprite = tankySprite; break;
            case EnemyType.Shooting: if (shootingSprite != null) spriteRenderer.sprite = shootingSprite; break;
            case EnemyType.Boss: if (bossSprite != null) spriteRenderer.sprite = bossSprite; break;
        }
        if (spriteRenderer.sprite == null)
            Debug.LogWarning($"[Enemy] ApplyTypeSprite: type={enemyType} but sprite is NULL! Assign sprite in Inspector for this type.");
    }

    private void UpdateSymbolDisplay()
    {
        if (symbolText != null && requiredSymbols.Count > 0)
        {
            symbolText.text = string.Join(" ", requiredSymbols);
        }
    }

    public void OnDefeated()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log($"[Enemy] OnDefeated: type={enemyType}, name={gameObject.name}, score={scoreValue}");
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.AddScore(scoreValue);

        if (WaveManager.Instance != null)
            WaveManager.Instance.OnEnemyDefeated(this);

        var anim = GetComponent<Animator>();
        if (anim != null)
            anim.SetBool("IsDead", true);

        if (agent != null) agent.enabled = false;
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        CancelInvoke();
        if (currentBullet != null)
        {
            currentBullet.OnDestroyed -= OnBulletDestroyed;
            currentBullet = null;
        }

        StartCoroutine(DestroyAfterDeathAnimation());
    }

    private System.Collections.IEnumerator DestroyAfterDeathAnimation()
    {
        yield return new WaitForSeconds(deathAnimationDuration);
        Destroy(gameObject);
    }

    private void AttackPlayer()
    {
        if (WaveManager.Instance != null)
            WaveManager.Instance.OnEnemyReachedPlayer(this);

        var player = target?.GetComponent<Player>();
        if (player != null)
            player.TakeDamage(damageToPlayer);

        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            AttackPlayer();
    }
}
