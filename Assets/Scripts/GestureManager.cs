using UnityEngine;

public class GestureManager : MonoBehaviour
{
    public static GestureManager Instance { get; private set; }
    public DollarRecognizer Recognizer { get; private set; }

    void Awake()
    {
        Debug.Log($"[GestureManager] Awake: {gameObject.name}");
        if (Instance == null)
        {
            Instance = this;
            Recognizer = new DollarRecognizer();
            Recognizer.InitializeDefaultGestures();
            Debug.Log("[GestureManager] Recognizer initialized");
        }
        else
        {
            Destroy(gameObject);
        }
    }
}