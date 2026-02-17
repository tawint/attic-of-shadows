using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Пуля стреляющего монстра. Свои HP (линии бьют, макс 3). Пока пуля жива — стреляющего монстра не убить.
/// </summary>
public class EnemyBullet : MonoBehaviour
{
    [Tooltip("Максимум 2 HP, линии снимают по 1")]
    [SerializeField] private int maxHp = 2;
    [SerializeField] private float speed = 4f;
    [SerializeField] private float damageToPlayer = 1f;
    
    [Header("Symbol display")]
    [SerializeField] private Vector3 symbolOffset = new Vector3(0, 0.5f, 0);
    [SerializeField] private float spacingBetweenSprites = 0.3f; // ← расстояние между спрайтами символов

    [Header("Symbol Sprites")]
    [SerializeField] private Sprite[] symbolSprites = new Sprite[9];
    // symbolSprites[0] - Symbols_0 (звезда Star)
    // symbolSprites[1] - Symbols_1 (горизонтальная линия HorizontalLine)
    // symbolSprites[2] - Symbols_2 (вертикальная линия VerticalLine)
    // symbolSprites[3] - Symbols_3 (угол вверх ^)
    // symbolSprites[4] - Symbols_4 (угол вниз V)
    // symbolSprites[5] - Symbols_5 (Кружок Circle)
    // symbolSprites[6] - Symbols_6 (угол влево <)
    // symbolSprites[7] - Symbols_7 (угол вправо >)
    // symbolSprites[8] - Symbols_8 (спираль Spiral)

    [Header("Symbol Sprite Scale")]
    [SerializeField] private Vector3 symbolSpriteScale = new Vector3(0.6f, 0.6f, 1f); // ← УМЕНЬШИТЕ ЗДЕСЬ для меньшего размера спрайтов

    private int currentHp;
    private List<string> requiredSymbols = new List<string>();
    private Transform target;
    private Enemy owner;
    private Rigidbody2D rb;
    private List<SpriteRenderer> symbolSpriteRenderers = new List<SpriteRenderer>();

    public event System.Action OnDestroyed;

    public static readonly string[] LineSymbols = { "VerticalLine", "HorizontalLine" };
    public List<string> RequiredSymbols => requiredSymbols;

    public void Initialize(Transform playerTarget, Enemy shooter)
    {
        target = playerTarget;
        owner = shooter;
        currentHp = maxHp;
        requiredSymbols.Clear();
        requiredSymbols.Add("VerticalLine");
        requiredSymbols.Add("HorizontalLine");
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;
        SetBulletLayer();
        UpdateSymbolDisplay();
        Debug.Log($"[EnemyBullet] Initialize: HP={maxHp}, symbols=[| -]");
    }

    /// <summary> Layer "Bullet" — v Unity nuzhno otklyuchit koliziyu s layer "Enemy" v Physics 2D. </summary>
    private void SetBulletLayer()
    {
        int layer = LayerMask.NameToLayer("Bullet");
        if (layer >= 0)
            gameObject.layer = layer;
    }

    private void Update()
    {
        if (target == null) return;
        Vector2 dir = (target.position - transform.position).normalized;
        if (rb != null)
            rb.velocity = dir * speed;
        else
            transform.position += (Vector3)(dir * speed * Time.deltaTime);
        
        UpdateSpritePositions();
    }

    /// <summary>
    /// Update positions of all symbol sprites to follow the bullet
    /// </summary>
    private void UpdateSpritePositions()
    {
        if (symbolSpriteRenderers.Count == 0) return;

        int totalSymbols = symbolSpriteRenderers.Count;
        float totalWidth = (totalSymbols - 1) * spacingBetweenSprites;
        float startX = -totalWidth * 0.5f;

        for (int i = 0; i < symbolSpriteRenderers.Count; i++)
        {
            if (symbolSpriteRenderers[i] != null)
            {
                float posX = startX + i * spacingBetweenSprites;
                symbolSpriteRenderers[i].transform.position = 
                    transform.position + symbolOffset + new Vector3(posX, 0, 0);
            }
        }
    }

    private void UpdateSymbolDisplay()
    {
        // Удаляем старые спрайты
        foreach (var renderer in symbolSpriteRenderers)
        {
            if (renderer != null)
                Destroy(renderer.gameObject);
        }
        symbolSpriteRenderers.Clear();

        if (requiredSymbols == null || requiredSymbols.Count == 0)
            return;

        // Создаём новые спрайты для каждого символа
        for (int i = 0; i < requiredSymbols.Count; i++)
        {
            CreateSymbolSpriteRenderer(requiredSymbols[i], i);
        }
    }

    /// <summary>
    /// Create SpriteRenderer for one symbol at given index
    /// </summary>
    private void CreateSymbolSpriteRenderer(string symbol, int index)
    {
        GameObject spriteObj = new GameObject($"BulletSymbol_{index}");
        spriteObj.transform.SetParent(transform);
        
        // Вычисляем позицию с учетом количества символов (центрируем)
        int totalSymbols = requiredSymbols.Count;
        float totalWidth = (totalSymbols - 1) * spacingBetweenSprites;
        float startX = -totalWidth * 0.5f;
        float posX = startX + index * spacingBetweenSprites;
        
        spriteObj.transform.localPosition = new Vector3(posX, 0, 0);

        SpriteRenderer spriteRenderer = spriteObj.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = 100;
        spriteObj.transform.localScale = symbolSpriteScale;

        // Устанавливаем спрайт
        Sprite sprite = GetSymbolSprite(symbol);
        spriteRenderer.sprite = sprite;
        spriteRenderer.color = Color.white;

        symbolSpriteRenderers.Add(spriteRenderer);
    }

    /// <summary>
    /// Get sprite for symbol
    /// </summary>
    private Sprite GetSymbolSprite(string symbol)
    {
        int spriteIndex = GetSymbolSpriteIndex(symbol);
        if (spriteIndex >= 0 && spriteIndex < symbolSprites.Length)
            return symbolSprites[spriteIndex];
        return null;
    }

    /// <summary>
    /// Get sprite index (0-8) for symbol
    /// </summary>
    private int GetSymbolSpriteIndex(string symbol)
    {
        switch (symbol)
        {
            case "Star": return 0;          // Symbols_0 - звезда
            case "HorizontalLine": return 1; // Symbols_1 - горизонтальная линия
            case "VerticalLine": return 2;   // Symbols_2 - вертикальная линия
            case "^": return 3;              // Symbols_3 - угол вверх
            case "V": return 4;              // Symbols_4 - угол вниз
            case "Circle": return 5;         // Symbols_5 - кружок
            case "<": return 6;              // Symbols_6 - угол влево
            case ">": return 7;              // Symbols_7 - угол вправо
            case "Spiral": return 8;         // Symbols_8 - спираль
            default: return -1;
        }
    }

    /// <summary>
    /// Uron ot linii (VerticalLine/HorizontalLine). Lyubaya liniya snimaet 1 HP.
    /// S spiska simvolov snímается pervyj, esli sovpadaet — dlya otobrazheniya.
    /// </summary>
    public bool TakeDamageFromSymbol(string symbol)
    {
        bool isLine = (symbol == "VerticalLine" || symbol == "HorizontalLine");
        if (!isLine) return false;

        if (requiredSymbols != null && requiredSymbols.Count > 0 && requiredSymbols[0] == symbol)
        {
            requiredSymbols.RemoveAt(0);
            UpdateSymbolDisplay();
        }

        currentHp--;
        Debug.Log($"[EnemyBullet] TakeDamageFromSymbol: symbol={symbol}, HP -> {currentHp}");
        if (currentHp <= 0)
        {
            DestroyBullet();
            return true;
        }
        return true;
    }

    private void DestroyBullet()
    {
        Debug.Log("[EnemyBullet] DestroyBullet: bullet destroyed, shooter can now be damaged");
        OnDestroyed?.Invoke();
        Destroy(gameObject);
    }

    /// <summary>
    /// Урон игроку при попадании — такой же как от врагов (TakeDamage).
    /// Нужно: у префаба Bullet — Collider2D (Is Trigger = true), Rigidbody2D; у игрока — тег Player и Collider2D.
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            var player = other.GetComponent<Player>();
            if (player != null)
                player.TakeDamage(damageToPlayer);
            DestroyBullet();
        }
    }
}
