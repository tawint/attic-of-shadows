using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GestureField : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    private static string GetPath(Transform t)
    {
        if (t == null) return "";
        return (t.parent != null ? GetPath(t.parent) + "/" : "") + t.name;
    }
    private List<Vector2> points = new List<Vector2>();
    private bool isDrawing = false;
    private LineRenderer lineRenderer;
    [SerializeField] private Material lineMaterial;

    public static string LastDrawnSymbol { get; private set; } = "Unknown";

    void Awake()
    {
        Debug.Log($"[GestureField] Awake: {gameObject.name}, path={GetPath(transform)}");
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        if (lineMaterial != null)
        {
            lineRenderer.material = lineMaterial;
        }
        else
        {
            Debug.LogWarning("Line material not assigned! Line may be invisible.");
            lineRenderer.material = new Material(Shader.Find("Unlit/Color"));
        }

        // Улучшенный внешний вид линии 
        lineRenderer.startColor = new Color(0.475f, 0.314f, 0.455f, 1f);  // Ярко-голубо-зелёный
        lineRenderer.endColor = new Color(0.475f, 0.314f, 0.455f, 1f);
        lineRenderer.startWidth = 0.18f;  // Увеличенная толщина
        lineRenderer.endWidth = 0.18f;
        lineRenderer.useWorldSpace = false;
        lineRenderer.positionCount = 0;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isDrawing = true;
        points.Clear();
        lineRenderer.positionCount = 0;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)transform, eventData.position, eventData.pressEventCamera, out Vector2 localPos))
        {
            points.Add(localPos);
            UpdateLine();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDrawing) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)transform, eventData.position, eventData.pressEventCamera, out Vector2 localPos))
        {
            points.Add(localPos);
            UpdateLine();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDrawing = false;

        Debug.Log($"[GestureField] OnPointerUp: points={points.Count}, GestureManager={GestureManager.Instance != null}");

        if (points.Count < 4 || GestureManager.Instance == null)
        {
            if (points.Count < 4) Debug.Log("[GestureField] Too few points, gesture ignored (need >= 4)");
            ClearLine();
            return;
        }

        var result = GestureManager.Instance.Recognizer.Recognize(points);
        string finalName = result.Match?.Name ?? "Unknown";
        float score = result.Score;
        Debug.Log($"[GestureField] Recognizer result: raw={result.Match?.Name ?? "null"}, score={score:F2}");

        // === POST-FILTRACIYA DLYA LINIY ===
        if (result.Match != null && (finalName == "VerticalLine" || finalName == "HorizontalLine"))
        {
            finalName = CorrectLineOrientation(finalName);
            Debug.Log($"[GestureField] Line corrected to: {finalName}");
        }

        // === POST-FILTRACIYA DLYA UGLOV (^, V, <, >) ===
        if (result.Match != null && (finalName == "^" || finalName == "V" || finalName == "<" || finalName == ">"))
        {
            finalName = DetermineAngleOrientation();
            Debug.Log($"[GestureField] Angle corrected to: {finalName}");
        }

        if (finalName != "Unknown")
        {
            LastDrawnSymbol = finalName;
            Debug.Log($"[GestureField] Final symbol: {finalName} (score: {score:F2}), attacking...");
            AttackEnemiesWithSymbol(finalName);
            if (Player.Instance != null)
            {
                Player.Instance.PlayNextSpellAnimation();
                Player.Instance.ShowSymbol(finalName);
            }
        }
        else
        {
            Debug.Log("[GestureField] Unknown gesture - not attacking");
        }

        Invoke(nameof(ClearLine), 0.3f);
    }

    /// <summary>
    /// Attack enemies that require the drawn symbol
    /// Also auto-destroys lines if ability is active
    /// </summary>
    private void AttackEnemiesWithSymbol(string symbol)
    {
        if (WaveManager.Instance == null)
        {
            Debug.LogWarning("[GestureField] WaveManager.Instance is NULL! Cannot attack.");
            return;
        }

        int enemiesDefeated = WaveManager.Instance.DefeatEnemiesWithSymbol(symbol);

        if (enemiesDefeated > 0)
        {
            Debug.Log($"[GestureField] Defeated {enemiesDefeated} enemies with symbol: {symbol}");
        }
        else
        {
            Debug.Log($"[GestureField] No enemy defeated. Symbol drawn: {symbol} (check if any enemy has this symbol and is not invincible)");
        }

        // Проверяем активные способности для автоуничтожения линий
        if (AbilitySystem.Instance != null)
        {
            if (AbilitySystem.Instance.IsHorizontalLineAutoDestroyActive && symbol == "HorizontalLine")
            {
                Debug.Log("[GestureField] Auto-destroying HorizontalLine (Ability W active)");
                WaveManager.Instance.AutoDestroyHorizontalLines();
            }
            if (AbilitySystem.Instance.IsVerticalLineAutoDestroyActive && symbol == "VerticalLine")
            {
                Debug.Log("[GestureField] Auto-destroying VerticalLine (Ability E active)");
                WaveManager.Instance.AutoDestroyVerticalLines();
            }
        }
    }

    /// <summary>
    /// Opredelyaet orientaciyu ugla (^, V, <, >) po realnym Tochkam
    /// </summary>
    private string DetermineAngleOrientation()
    {
        if (points.Count < 3) return "^";

        // Nahodim bounding box
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        foreach (var p in points)
        {
            minX = Mathf.Min(minX, p.x);
            maxX = Mathf.Max(maxX, p.x);
            minY = Mathf.Min(minY, p.y);
            maxY = Mathf.Max(maxY, p.y);
        }

        float width = maxX - minX;
        float height = maxY - minY;

        // Nahodim tochku peregiba (vershinu ugla) - tochka naibolee udalennaya ot linii start-end
        Vector2 startPoint = points[0];
        Vector2 endPoint = points[points.Count - 1];
        
        float maxDist = 0;
        int apexIndex = points.Count / 2;
        
        for (int i = 1; i < points.Count - 1; i++)
        {
            float dist = DistanceToLine(points[i], startPoint, endPoint);
            if (dist > maxDist)
            {
                maxDist = dist;
                apexIndex = i;
            }
        }
        
        Vector2 apex = points[apexIndex];
        
        // Centr figury
        float centerX = (minX + maxX) / 2;
        float centerY = (minY + maxY) / 2;

        // Opredelyaem gde nahoditsya vershina otnositelno centra
        float apexRelX = apex.x - centerX;
        float apexRelY = apex.y - centerY;

        // Normalizuem otnositelno razmera figury
        float normX = width > 0.01f ? apexRelX / width : 0;
        float normY = height > 0.01f ? apexRelY / height : 0;

        // Opredelyaem orientaciyu po napravleniyu vershiny
        if (Mathf.Abs(normY) > Mathf.Abs(normX))
        {
            // Vershina sverhu ili snizu -> ^ ili V
            if (normY > 0)
                return "^";  // vershina sverhu
            else
                return "V";  // vershina snizu
        }
        else
        {
            // Vershina sleva ili sprava -> < ili >
            if (normX < 0)
                return "<";  // vershina sleva
            else
                return ">";  // vershina sprava
        }
    }

    /// <summary>
    /// Rasstoyanie ot tochki do linii
    /// </summary>
    private float DistanceToLine(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
    {
        Vector2 line = lineEnd - lineStart;
        float len = line.magnitude;
        if (len < 0.0001f) return (point - lineStart).magnitude;
        
        Vector2 normalized = line / len;
        Vector2 toPoint = point - lineStart;
        float projection = Vector2.Dot(toPoint, normalized);
        Vector2 closest = lineStart + normalized * Mathf.Clamp(projection, 0, len);
        return (point - closest).magnitude;
    }

    /// <summary>
    /// Korrektiruet orientaciyu linii tolko pri ochen yavnom sootnoshenii (chto by ne menyat iz-za paneli).
    /// </summary>
    private string CorrectLineOrientation(string currentName)
    {
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        foreach (var p in points)
        {
            minX = Mathf.Min(minX, p.x);
            maxX = Mathf.Max(maxX, p.x);
            minY = Mathf.Min(minY, p.y);
            maxY = Mathf.Max(maxY, p.y);
        }

        float width = maxX - minX;
        float height = maxY - minY;

        // Tolko esli forma ochen yavno vertikalnaya ili gorizontalnaya (2.5x)
        if (width < 0.001f || height < 0.001f)
            return currentName;

        if (height > width * 2.5f)
            return "VerticalLine";
        if (width > height * 2.5f)
            return "HorizontalLine";

        return currentName;
    }

    private void UpdateLine()
    {
        lineRenderer.positionCount = points.Count;
        for (int i = 0; i < points.Count; i++)
        {
            // Z = -1 chtoby liniya bila nad fonom paneli
            lineRenderer.SetPosition(i, new Vector3(points[i].x, points[i].y, -1));
        }
    }

    private void ClearLine()
    {
        lineRenderer.positionCount = 0;
        points.Clear();
    }
}
