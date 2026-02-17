using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Логи при старте сцены: камера, Canvas, крупные UI — чтобы искать причину перекрытия экрана.
/// Повесь на любой GameObject в сцене (например, на пустой объект BootLog).
/// </summary>
[DefaultExecutionOrder(-1000)]
public class BootLog : MonoBehaviour
{
    private void Awake()
    {
        Debug.Log("========== [BootLog] Scene start ==========");

        var cam = Camera.main;
        if (cam != null)
        {
            Debug.Log($"[BootLog] Camera.main: clearFlags={cam.clearFlags}, depth={cam.depth}, cullingMask={cam.cullingMask}, orthographicSize={cam.orthographicSize}, backgroundColor={cam.backgroundColor}");
        }
        else
            Debug.LogWarning("[BootLog] Camera.main is NULL!");

        var canvases = FindObjectsOfType<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            var c = canvases[i];
            var rt = c.GetComponent<RectTransform>();
            string size = rt != null ? $", rect={rt.rect.width}x{rt.rect.height}, sizeDelta={rt.sizeDelta}" : "";
            Debug.Log($"[BootLog] Canvas[{i}] name={c.gameObject.name}, path={GetPath(c.transform)}, renderMode={c.renderMode}, sortOrder={c.sortingOrder}{size}");

            // Canvas как дочерний объект камеры ломает отображение сцены — отвязываем в корень
            if (cam != null && IsUnderCamera(c.transform, cam.transform))
            {
                Debug.LogWarning($"[BootLog] Canvas '{c.gameObject.name}' is under Main Camera — reparenting to root so scene displays correctly.");
                c.transform.SetParent(null);
                if (c.renderMode == RenderMode.ScreenSpaceCamera && c.worldCamera == null)
                    c.worldCamera = cam;
                Debug.Log($"[BootLog] Canvas reparented. New path={GetPath(c.transform)}");
            }

            // GestureArea рисуется поверх HP, если идёт позже в иерархии. Ставим её первой — тогда она сзади.
            var gestureArea = c.transform.Find("GestureArea");
            if (gestureArea != null)
            {
                gestureArea.SetAsFirstSibling();
                Debug.Log("[BootLog] GestureArea set as first sibling so HP/UI draw on top.");
            }

            var images = c.GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                var irt = img.rectTransform;
                float w = irt.rect.width, h = irt.rect.height;
                if (w > 200 || h > 200)
                    Debug.Log($"[BootLog]   -> large Image: {img.gameObject.name}, {w}x{h}, sprite={img.sprite?.name ?? "null"}");
            }
        }

        var bigRenderers = FindObjectsOfType<SpriteRenderer>(true);
        foreach (var sr in bigRenderers)
        {
            if (sr.bounds.size.x > 10f || sr.bounds.size.y > 10f)
                Debug.Log($"[BootLog] Large SpriteRenderer: {sr.gameObject.name}, path={GetPath(sr.transform)}, size={sr.bounds.size}, sprite={sr.sprite?.name ?? "null"}");
        }

        // Враги не сталкиваются друг с другом — все могут дойти до игрока и нанести урон
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0)
        {
            Physics2D.IgnoreLayerCollision(enemyLayer, enemyLayer, true);
            Debug.Log("[BootLog] Enemy-Enemy collision ignored (layer 'Enemy')");
        }

        Debug.Log("========== [BootLog] end ==========");
    }

    private static bool IsUnderCamera(Transform t, Transform cameraTransform)
    {
        while (t != null)
        {
            if (t == cameraTransform) return true;
            t = t.parent;
        }
        return false;
    }

    private static string GetPath(Transform t)
    {
        if (t == null) return "";
        return (t.parent != null ? GetPath(t.parent) + "/" : "") + t.name;
    }
}
