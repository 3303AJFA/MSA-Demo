using UnityEngine;
using Yarn.Unity;

/// <summary>
/// Точка вызова именованных сюжетных событий из .yarn в C#-подписчиков. Заменяет старый
/// DialogueRunner.OnChoiceEventFired/RaiseChoiceEvent — паттерн сохранён, входная точка теперь
/// Yarn-команда вместо DialogueEffect.eventID.
///
/// Использование в .yarn:
///   &lt;&lt;trigger_event "got_quest_intro"&gt;&gt;
/// или просто:
///   &lt;&lt;trigger_event got_quest_intro&gt;&gt;
///
/// Внешние подписчики (квестовая система, ачивки, открытие струн) подписываются на static
/// MSAYarnCommands.OnEventFired и реагируют на eventID.
///
/// [YarnCommand]-метод static — Yarn регистрирует команды по имени автоматически при загрузке
/// проекта, никаких ручных AddCommandHandler не нужно.
/// </summary>
public static class MSAYarnCommands
{
    public static event System.Action<string> OnEventFired;

    [YarnCommand("trigger_event")]
    public static void TriggerEvent(string eventID)
    {
        if (string.IsNullOrEmpty(eventID))
        {
            Debug.LogWarning("[MSAYarnCommands] trigger_event с пустым ID — игнор.");
            return;
        }
        OnEventFired?.Invoke(eventID);
    }
}
