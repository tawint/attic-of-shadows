using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class TypewriterWithConfirm : MonoBehaviour
{
    public TMP_Text textComponent;
    private string fullText = "О нет! Они прорвались! Святой свет, защити меня. Я не сдамся, пока мой крест горит!";
    public float typingSpeed = 0.1f;

    private bool isTyping = false;
    private bool isWaitingForConfirm = false; // Ждём подтверждение после печати

    void Start()
    {
        if (textComponent == null)
        {
            Debug.LogError("TMP_Text не назначен!");
            return;
        }


        textComponent.text = fullText;
        textComponent.maxVisibleCharacters = 0;
        StartCoroutine(TypeText());
    }

    void Update()
    {
        if (isTyping && Input.anyKeyDown)
        {
            // Пропускаем печать
            StopAllCoroutines();
            textComponent.maxVisibleCharacters = fullText.Length;
            isTyping = false;
            StartWaitingForConfirm();
        }
        else if (isWaitingForConfirm && Input.anyKeyDown)
        {
            // Подтверждение: переходим на следующую сцену
            SceneManager.LoadScene(2);
        }
    }

    private IEnumerator TypeText()
    {
        isTyping = true;

        for (int i = 0; i < fullText.Length; i++)
        {
            // Пропускаем TMP-теги
            if (fullText[i] == '<')
            {
                int closeIndex = fullText.IndexOf('>', i);
                if (closeIndex != -1)
                {
                    i = closeIndex;
                    textComponent.maxVisibleCharacters = i + 1;
                    continue;
                }
            }

            textComponent.maxVisibleCharacters++;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        StartWaitingForConfirm();
    }

    private void StartWaitingForConfirm()
    {
        isWaitingForConfirm = true;
    }
}