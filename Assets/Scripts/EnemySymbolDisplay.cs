using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Displays all required symbols above an enemy using sprites
/// Attach this to enemy prefab or as a child object
/// </summary>
public class EnemySymbolDisplay : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0, 1f, 0);
    [SerializeField] private float spacingBetweenSprites = 0.4f; // ← расстояние между спрайтами символов

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

    private Enemy enemy;
    private Transform mainCamera;
    private List<SpriteRenderer> symbolSpriteRenderers = new List<SpriteRenderer>();
    private List<string> lastDisplayedSymbols = new List<string>();

    private void Start()
    {
        enemy = GetComponentInParent<Enemy>();
        mainCamera = Camera.main?.transform;

        UpdateDisplay();
    }

    private void LateUpdate()
    {
        // Follow enemy position and update sprite positions
        if (enemy != null && symbolSpriteRenderers.Count > 0)
        {
            UpdateSpritePositions();
        }
    }

    /// <summary>
    /// Update the displayed symbols — show sprites of all required symbols
    /// </summary>
    public void UpdateDisplay()
    {
        if (enemy == null) return;

        var symbols = enemy.RequiredSymbols;
        
        // Проверяем, изменился ли список символов
        bool symbolsChanged = symbols == null || symbols.Count != lastDisplayedSymbols.Count;
        if (!symbolsChanged && symbols != null)
        {
            for (int i = 0; i < symbols.Count; i++)
            {
                if (symbols[i] != lastDisplayedSymbols[i])
                {
                    symbolsChanged = true;
                    break;
                }
            }
        }

        if (!symbolsChanged) return;

        // Сохраняем новый список символов
        lastDisplayedSymbols.Clear();
        if (symbols != null)
            lastDisplayedSymbols.AddRange(symbols);

        // Удаляем старые спрайты
        foreach (var renderer in symbolSpriteRenderers)
        {
            if (renderer != null)
                Destroy(renderer.gameObject);
        }
        symbolSpriteRenderers.Clear();

        // Создаём новые спрайты для каждого символа
        if (symbols != null && symbols.Count > 0)
        {
            for (int i = 0; i < symbols.Count; i++)
            {
                CreateSymbolSpriteRenderer(symbols[i], i);
            }
        }
    }

    /// <summary>
    /// Create SpriteRenderer for one symbol at given index
    /// </summary>
    private void CreateSymbolSpriteRenderer(string symbol, int index)
    {
        GameObject spriteObj = new GameObject($"SymbolSprite_{index}");
        spriteObj.transform.SetParent(transform);
        
        // Вычисляем позицию с учетом количества символов (центрируем относительно врага)
        int totalSymbols = lastDisplayedSymbols.Count;
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
    /// Update positions of all symbol sprites to follow the enemy
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
                    enemy.transform.position + offset + new Vector3(posX, 0, 0);
            }
        }
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
    /// Set symbol externally
    /// </summary>
    public void SetSymbol(string symbol)
    {
        // Удаляем старые спрайты
        foreach (var renderer in symbolSpriteRenderers)
        {
            if (renderer != null)
                Destroy(renderer.gameObject);
        }
        symbolSpriteRenderers.Clear();
        lastDisplayedSymbols.Clear();

        // Создаём один новый спрайт
        lastDisplayedSymbols.Add(symbol);
        CreateSymbolSpriteRenderer(symbol, 0);
    }

    /// <summary>
    /// Set symbols externally (displays all symbols)
    /// </summary>
    public void SetSymbols(List<string> symbols)
    {
        if (symbols == null || symbols.Count == 0)
        {
            foreach (var renderer in symbolSpriteRenderers)
            {
                if (renderer != null)
                    Destroy(renderer.gameObject);
            }
            symbolSpriteRenderers.Clear();
            lastDisplayedSymbols.Clear();
            return;
        }

        // Удаляем старые спрайты
        foreach (var renderer in symbolSpriteRenderers)
        {
            if (renderer != null)
                Destroy(renderer.gameObject);
        }
        symbolSpriteRenderers.Clear();

        // Создаём новые спрайты для всех символов
        lastDisplayedSymbols.Clear();
        lastDisplayedSymbols.AddRange(symbols);

        for (int i = 0; i < symbols.Count; i++)
        {
            CreateSymbolSpriteRenderer(symbols[i], i);
        }
    }
}
