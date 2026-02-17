using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("Prefabs by Type")]
    [Tooltip("If type prefabs are empty, this one is used for ALL types")]
    [SerializeField] private GameObject defaultEnemyPrefab;
    [SerializeField] private GameObject fastPrefab;
    [SerializeField] private GameObject tankyPrefab;
    [SerializeField] private GameObject shootingPrefab;
    [SerializeField] private GameObject bossPrefab;

    [Header("Spawn Settings")]
    [Tooltip("Точки спавна (дыры) — мобы появляются из этих позиций. Перетащи сюда пустые объекты в сцене.")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private Transform playerTarget;
    [Tooltip("Позиции игрока перед боссом (PlayerPlace_1, PlayerPlace_2, PlayerPlace_3). Игрок телепортируется на случайную перед каждой босс-волной.")]
    [SerializeField] private Transform[] playerPlacePoints;

    [Header("Wave Settings")]
    [Tooltip("С каждой волной врагов больше: старт + (волна-1) × прирост")]
    [SerializeField] private int startingEnemiesPerWave = 3;
    [SerializeField] private int enemiesIncreasePerWave = 2;
    [SerializeField] private float baseSpawnDelay = 2f;
    [SerializeField] private float minSpawnDelay = 0.5f;
    [SerializeField] private float delayDecreasePerWave = 0.15f;

    [Header("Макс. символов над врагом (нарисовать любой — убить)")]
    [Tooltip("Быстрые: линии, макс символов над одним врагом")]
    [SerializeField] private int maxSymbolsPerFastEnemy = 10;
    [Tooltip("Жирные: галочки и линии, макс символов над одним врагом")]
    [SerializeField] private int maxSymbolsPerTankyEnemy = 7;
    [Tooltip("Стреляющий: линии; макс символов над врагом (у пули свои 3 HP)")]
    [SerializeField] private int maxSymbolsPerShootingEnemy = 2;

    [Header("Прогрессия символов (растёт после каждой босс-волны)")]
    [Tooltip("Текущее кол-во символов на тип; увеличивается после каждой волны с боссом")]
    private int currentSymbolsPerFastEnemy = 1;
    private int currentSymbolsPerTankyEnemy = 1;
    private int currentSymbolsPerShootingEnemy = 1;

    [Header("Enemy Settings")]
    [SerializeField] private float baseEnemySpeed = 1f;
    [SerializeField] private float speedIncreasePerWave = 0.1f;
    [SerializeField] private float maxSpeedMultiplier = 3f;
    [SerializeField] private int baseScorePerEnemy = 100;
    [SerializeField] private int scoreIncreasePerWave = 25;

    [Header("Boss Wave")]
    [Tooltip("Босс: звездочка + много других символов, медленный, один на волну")]
    [SerializeField] private int bossWaveInterval = 5;
    [SerializeField] private int bossScoreBonus = 500;

    [Header("Wave Timing")]
    [SerializeField] private float delayBetweenWaves = 3f;
    [SerializeField] private float firstWaveDelay = 2f;

    private int currentWave = 0;
    private int enemiesRemaining = 0;
    private int enemiesToSpawn = 0;
    private bool isSpawning = false;
    private bool isWaitingForNextWave = false;
    private List<Enemy> activeEnemies = new List<Enemy>();

    public int CurrentWave => currentWave;
    public int EnemiesRemaining => enemiesRemaining;
    public bool IsWaveInProgress => isSpawning || enemiesRemaining > 0;

    public System.Action<int> OnWaveStarted;
    public System.Action<int> OnWaveCompleted;
    public System.Action OnGameOver;

    // Быстрые: только вертикальная и горизонтальная линии
    private static readonly string[] FastSymbols = { "VerticalLine", "HorizontalLine", "Circle" };
    // Жирные: галочки и линии
    private static readonly string[] TankySymbols = { "^", "V", "<", ">", "VerticalLine", "HorizontalLine" };
    // Стреляющий: линии (пулю бьют линии, макс 2 HP у пули)
    private static readonly string[] ShootingSymbols = { "VerticalLine", "HorizontalLine" };
    // Босс: звездочка и много других символов
    private static readonly string[] BossSymbols = { "Star", "Circle", "^", "V", "<", ">", "Spiral" };

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void Start()
    {
        Debug.Log("[WaveManager] Start");
        if (playerTarget == null)
        {
            var player = FindObjectOfType<Player>();
            if (player != null) playerTarget = player.transform;
        }
        if (playerTarget == null) Debug.LogWarning("[WaveManager] Player target is NULL!");
        Debug.Log($"[WaveManager] Prefabs: Default={defaultEnemyPrefab != null}, Fast={fastPrefab != null}, Tanky={tankyPrefab != null}, Shooting={shootingPrefab != null}, Boss={bossPrefab != null}");
        StartCoroutine(StartFirstWave());
    }

    private void Update()
    {
        if (activeEnemies.Count > 0)
        {
            int removed = activeEnemies.RemoveAll(e => e == null);
            if (removed > 0)
            {
                enemiesRemaining = Mathf.Max(0, enemiesRemaining - removed);
                CheckWaveCompletion();
            }
        }
    }

    private IEnumerator StartFirstWave()
    {
        yield return new WaitForSeconds(firstWaveDelay);
        StartNextWave();
    }

    public void StartNextWave()
    {
        currentWave++;
        bool isBossWave = (currentWave % bossWaveInterval == 0);

        Debug.Log($"[WaveManager] StartNextWave: wave={currentWave}, isBossWave={isBossWave}");

        if (isBossWave)
        {
            MovePlayerToRandomPlace();
            enemiesToSpawn = 1;
            enemiesRemaining = 1;
            Debug.Log($"[WaveManager] Wave {currentWave} - BOSS! Boss prefab={bossPrefab != null}");
            OnWaveStarted?.Invoke(currentWave);
            StartCoroutine(SpawnBossWave());
        }
        else
        {
            enemiesToSpawn = startingEnemiesPerWave + (currentWave - 1) * enemiesIncreasePerWave;
            enemiesRemaining = enemiesToSpawn;
            Debug.Log($"[WaveManager] Wave {currentWave} started! Enemies to spawn: {enemiesToSpawn}");
            OnWaveStarted?.Invoke(currentWave);
            StartCoroutine(SpawnWaveEnemies());
        }
    }

    /// <summary>
    /// Перед боссом перемещает игрока на случайную точку из PlayerPlace_1, PlayerPlace_2, PlayerPlace_3.
    /// Использует NavMesh для плавного движения вместо телепорта.
    /// </summary>
    private void MovePlayerToRandomPlace()
    {
        Transform[] places = playerPlacePoints;
        if (places == null || places.Length == 0)
        {
            var p1 = GameObject.Find("PlayerPlace_1");
            var p2 = GameObject.Find("PlayerPlace_2");
            var p3 = GameObject.Find("PlayerPlace_3");
            var list = new List<Transform>();
            if (p1 != null) list.Add(p1.transform);
            if (p2 != null) list.Add(p2.transform);
            if (p3 != null) list.Add(p3.transform);
            if (list.Count == 0) return;
            places = list.ToArray();
        }

        Transform chosen = places[Random.Range(0, places.Length)];
        if (chosen == null || playerTarget == null) return;

        Vector3 pos = chosen.position;
        var agent = playerTarget.GetComponent<NavMeshAgent>();
        var animator = playerTarget.GetComponent<Animator>();

        if (agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(pos);
            // Включаем анимацию ходьбы
            if (animator != null)
                animator.SetBool("IsWalking", true);
            StartCoroutine(WaitForPlayerReachedDestination(agent, animator));
        }
        else
        {
            // Fallback: просто телепортируем
            playerTarget.position = pos;
        }

        Debug.Log($"[WaveManager] Player moving to {chosen.name} before boss wave");
    }

    private System.Collections.IEnumerator WaitForPlayerReachedDestination(NavMeshAgent agent, Animator animator)
    {
        // Ждём пока игрок не приблизится к цели или не достигнет её
        float timeout = 10f;  // максимум 10 секунд ожидания
        float elapsed = 0f;
        
        while (elapsed < timeout && agent.remainingDistance > 0.5f && agent.hasPath)
        {
            elapsed += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }
        
        // Отключаем анимацию ходьбы
        if (animator != null)
            animator.SetBool("IsWalking", false);
        Debug.Log("[WaveManager] Player reached destination");
    }

    private IEnumerator SpawnBossWave()
    {
        isSpawning = true;
        SpawnEnemy(EnemyType.Boss, 0.4f, baseScorePerEnemy * currentWave + bossScoreBonus);
        yield return new WaitForSeconds(0.5f);
        isSpawning = false;
        CheckWaveCompletion();
    }

    private IEnumerator SpawnWaveEnemies()
    {
        isSpawning = true;
        float spawnDelay = Mathf.Max(minSpawnDelay, baseSpawnDelay - (currentWave - 1) * delayDecreasePerWave);
        float speedMultiplier = Mathf.Min(maxSpeedMultiplier, baseEnemySpeed + (currentWave - 1) * speedIncreasePerWave);
        int enemyScore = baseScorePerEnemy + (currentWave - 1) * scoreIncreasePerWave;

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            EnemyType type = ChooseEnemyType();
            SpawnEnemy(type, speedMultiplier, enemyScore);
            yield return new WaitForSeconds(spawnDelay);
        }
        isSpawning = false;
    }

    private EnemyType ChooseEnemyType()
    {
        var options = new List<EnemyType>();
        if (fastPrefab != null) options.Add(EnemyType.Fast);
        if (tankyPrefab != null) options.Add(EnemyType.Tanky);
        // Стреляющие мобы спавнятся только после волны 10
        if (shootingPrefab != null && currentWave >= 10) options.Add(EnemyType.Shooting);
        if (options.Count == 0) return EnemyType.Fast;
        return options[Random.Range(0, options.Count)];
    }

    private void SpawnEnemy(EnemyType type, float speedMultiplier, int score)
    {
        GameObject prefab = GetPrefabForType(type);
        if (prefab == null)
        {
            Debug.LogWarning($"[WaveManager] SpawnEnemy: NO PREFAB for type={type}! Assign in Inspector.");
            return;
        }

        Vector3 spawnPosition = GetSpawnPositionFromHoles();
        Debug.Log($"[WaveManager] SpawnEnemy: type={type}, pos={spawnPosition}");

        GameObject enemyObj = Instantiate(prefab, spawnPosition, Quaternion.identity);
        NavMeshAgent agent = enemyObj.GetComponent<NavMeshAgent>();
        if (agent != null) agent.Warp(spawnPosition);
        else Debug.LogWarning($"[WaveManager] Spawned {type} has no NavMeshAgent!");

        Enemy enemy = enemyObj.GetComponent<Enemy>();
        if (enemy == null)
        {
            Debug.LogWarning($"[WaveManager] Spawned prefab for {type} has no Enemy script!");
            return;
        }
        {
            List<string> symbolPool = GetSymbolsForType(type);
            List<string> assigned = new List<string>();
            if (type == EnemyType.Boss)
            {
                assigned.Add("Star");
                for (int i = 0; i < symbolPool.Count && assigned.Count < 4; i++)
                {
                    if (symbolPool[i] != "Star" && Random.value > 0.5f)
                        assigned.Add(symbolPool[i]);
                }
                if (assigned.Count == 1) assigned.Add(symbolPool[Random.Range(0, symbolPool.Count)]);
            }
            else
            {
                int maxForType = GetMaxSymbolsForType(type);
                int currentCap = GetCurrentSymbolCountForType(type);
                int count = Mathf.Clamp(currentCap, 1, Mathf.Min(maxForType, symbolPool.Count));
                var shuffled = new List<string>(symbolPool);
                for (int i = shuffled.Count - 1; i > 0; i--)
                {
                    int j = Random.Range(0, i + 1);
                    var t = shuffled[i]; shuffled[i] = shuffled[j]; shuffled[j] = t;
                }
                for (int i = 0; i < count && i < shuffled.Count; i++)
                    assigned.Add(shuffled[i]);
            }
            enemy.Initialize(type, assigned, speedMultiplier, score, playerTarget);
            activeEnemies.Add(enemy);
            Debug.Log($"[WaveManager] Enemy spawned: type={type}, symbols=[{string.Join(",", assigned)}], activeCount={activeEnemies.Count}");
        }
    }

    private GameObject GetPrefabForType(EnemyType type)
    {
        GameObject prefab = null;
        switch (type)
        {
            case EnemyType.Fast: prefab = fastPrefab; break;
            case EnemyType.Tanky: prefab = tankyPrefab; break;
            case EnemyType.Shooting: prefab = shootingPrefab; break;
            case EnemyType.Boss: prefab = bossPrefab; break;
            default: prefab = fastPrefab; break;
        }
        if (prefab == null)
            prefab = defaultEnemyPrefab;
        return prefab;
    }

    private int GetMaxSymbolsForType(EnemyType type)
    {
        switch (type)
        {
            case EnemyType.Fast: return maxSymbolsPerFastEnemy;
            case EnemyType.Tanky: return maxSymbolsPerTankyEnemy;
            case EnemyType.Shooting: return maxSymbolsPerShootingEnemy;
            default: return 2;
        }
    }

    /// <summary>
    /// Текущее кол-во символов для типа (прогрессия до max после каждой босс-волны).
    /// </summary>
    private int GetCurrentSymbolCountForType(EnemyType type)
    {
        switch (type)
        {
            case EnemyType.Fast: return currentSymbolsPerFastEnemy;
            case EnemyType.Tanky: return currentSymbolsPerTankyEnemy;
            case EnemyType.Shooting: return currentSymbolsPerShootingEnemy;
            default: return 1;
        }
    }

    private void IncreaseSymbolCountsAfterBossWave()
    {
        currentSymbolsPerFastEnemy = Mathf.Min(currentSymbolsPerFastEnemy + 1, maxSymbolsPerFastEnemy);
        currentSymbolsPerTankyEnemy = Mathf.Min(currentSymbolsPerTankyEnemy + 1, maxSymbolsPerTankyEnemy);
        currentSymbolsPerShootingEnemy = Mathf.Min(currentSymbolsPerShootingEnemy + 1, maxSymbolsPerShootingEnemy);
        Debug.Log($"[WaveManager] After boss wave: symbols per type Fast={currentSymbolsPerFastEnemy}, Tanky={currentSymbolsPerTankyEnemy}, Shooting={currentSymbolsPerShootingEnemy}");
    }

    private List<string> GetSymbolsForType(EnemyType type)
    {
        switch (type)
        {
            case EnemyType.Fast: return new List<string>(FastSymbols);
            case EnemyType.Tanky: return new List<string>(TankySymbols);
            case EnemyType.Shooting: return new List<string>(ShootingSymbols);
            case EnemyType.Boss: return new List<string>(BossSymbols);
            default: return new List<string>(FastSymbols);
        }
    }

    /// <summary>
    /// Спавн из точек-дыр (spawnPoints). Если точек нет — запасной вариант у игрока.
    /// </summary>
    private Vector3 GetSpawnPositionFromHoles()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform hole = spawnPoints[Random.Range(0, spawnPoints.Length)];
            if (hole != null)
            {
                Vector3 pos = hole.position;
                NavMeshHit hit;
                if (NavMesh.SamplePosition(pos, out hit, 2f, NavMesh.AllAreas))
                    return hit.position;
                return pos;
            }
        }
        if (playerTarget != null)
        {
            Vector3 fallback = playerTarget.position + Vector3.right * 4f + Vector3.down * 2f;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(fallback, out hit, 5f, NavMesh.AllAreas))
                return hit.position;
            return fallback;
        }
        return Vector3.zero;
    }

    public void OnEnemyDefeated(Enemy enemy)
    {
        if (activeEnemies.Contains(enemy)) { activeEnemies.Remove(enemy); enemiesRemaining--; }
        activeEnemies.RemoveAll(e => e == null);
        CheckWaveCompletion();
    }

    public void OnEnemyReachedPlayer(Enemy enemy)
    {
        if (activeEnemies.Contains(enemy)) { activeEnemies.Remove(enemy); enemiesRemaining--; }
        activeEnemies.RemoveAll(e => e == null);
        CheckWaveCompletion();
    }

    private void CheckWaveCompletion()
    {
        bool allEnemiesDead = enemiesRemaining <= 0 && activeEnemies.Count == 0;
        if (allEnemiesDead && !isWaitingForNextWave)
        {
            if (isSpawning) { StopAllCoroutines(); isSpawning = false; }
            bool wasBossWave = (currentWave % bossWaveInterval == 0);
            if (wasBossWave)
                IncreaseSymbolCountsAfterBossWave();
            OnWaveCompleted?.Invoke(currentWave);
            isWaitingForNextWave = true;
            StartCoroutine(StartNextWaveDelayed());
        }
    }

    private IEnumerator StartNextWaveDelayed()
    {
        yield return new WaitForSeconds(delayBetweenWaves);
        isWaitingForNextWave = false;
        StartNextWave();
    }

    /// <summary>
    /// Defeat all enemies (and damage bullets) for drawn symbol. Monsters can have multiple symbols - match if drawn symbol is in their list.
    /// </summary>
    public int DefeatEnemiesWithSymbol(string symbol)
    {
        Debug.Log($"[WaveManager] DefeatEnemiesWithSymbol: symbol={symbol}, activeEnemies={activeEnemies.Count}");
        bool isLine = (symbol == "VerticalLine" || symbol == "HorizontalLine");

        if (isLine)
        {
            var bullets = FindObjectsByType<EnemyBullet>(FindObjectsSortMode.None);
            Debug.Log($"[WaveManager] Line symbol: damaging {bullets.Length} bullets");
            foreach (var bullet in bullets)
            {
                if (bullet != null) bullet.TakeDamageFromSymbol(symbol);
            }
        }

        int count = 0;
        var copy = new List<Enemy>(activeEnemies);
        foreach (var enemy in copy)
        {
            if (enemy == null || !enemy.CanBeDefeatedBy(symbol) || !enemy.CanBeDamaged) continue;
            if (enemy.TryRemoveSymbol(symbol))
                count++;
        }
        return count;
    }

    /// <summary>
    /// Auto-destroy horizontal lines (Ability W effect)
    /// </summary>
    public void AutoDestroyHorizontalLines()
    {
        Debug.Log($"[WaveManager] Auto-destroying all HorizontalLines");
        DefeatEnemiesWithSymbol("HorizontalLine");
    }

    /// <summary>
    /// Auto-destroy vertical lines (Ability E effect)
    /// </summary>
    public void AutoDestroyVerticalLines()
    {
        Debug.Log($"[WaveManager] Auto-destroying all VerticalLines");
        DefeatEnemiesWithSymbol("VerticalLine");
    }
}
