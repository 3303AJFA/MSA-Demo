using UnityEngine;
using UnityEngine.InputSystem;
using Yarn.Unity;

/// <summary>
/// ВРЕМЕННАЯ заглушка запуска диалога — клавиша F запускает заданный узел Yarn-проекта.
/// Уйдёт в систему 2 (F + контур-подсветка NPC). НЕ переносить логику триггера в Yarn —
/// архитектурная граница «диалог запускается СНАРУЖИ» сохраняется (по тех-заметке).
/// </summary>
public class DialogueTestTrigger : MonoBehaviour
{
    [Tooltip("Yarn DialogueRunner. Если null — берётся первый в сцене.")]
    public DialogueRunner runner;

    [Tooltip("Имя стартового узла в YarnProject (например 'Start').")]
    public string startNode = "Start";

    [Tooltip("Клавиша запуска тестового диалога.")]
    public Key triggerKey = Key.F;

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        if (!kb[triggerKey].wasPressedThisFrame) return;

        if (runner == null) runner = FindFirstObjectByType<DialogueRunner>();
        if (runner == null)
        {
            Debug.LogWarning("[DialogueTestTrigger] DialogueRunner не найден в сцене.");
            return;
        }
        if (runner.IsDialogueRunning) return;
        if (string.IsNullOrEmpty(startNode))
        {
            Debug.LogWarning("[DialogueTestTrigger] startNode пуст.");
            return;
        }

        runner.StartDialogue(startNode).Forget();
    }
}
