using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using Yarn.Unity;
using BoolDictionary = System.Collections.Generic.Dictionary<string, bool>;
using FloatDictionary = System.Collections.Generic.Dictionary<string, float>;
using StringDictionary = System.Collections.Generic.Dictionary<string, string>;

/// <summary>
/// Мост между переменными Yarn ($flag) и нашими булевыми флагами в GameState.dialogueFlags.
/// MSA по тех-заметке CLAUDE.local — только bool-флаги, никаких skill-check'ов / статов /
/// числовых переменных. Float и string маршруты — warning'ом, чтобы было видно если в .yarn
/// кто-то завёл числовую переменную.
///
/// Имена переменных Yarn хранятся с префиксом '$' ($asked_about_mother). В GameState.dialogueFlags
/// храним без префикса — StripDollar() при чтении/записи.
///
/// Component crashes nothing: GameState — DontDestroyOnLoad-синглтон, MSAVariableStorage просто
/// делегирует. Никакого in-memory кэша.
/// </summary>
public class MSAVariableStorage : VariableStorageBehaviour
{
    public override bool TryGetValue<T>(string variableName, [NotNullWhen(true)] out T result)
    {
        result = default;
        if (string.IsNullOrEmpty(variableName)) return false;

        var gs = GameState.Instance;
        if (gs == null)
        {
            Debug.LogWarning($"[MSAVariableStorage] GameState.Instance == null при чтении {variableName}");
            return false;
        }

        // Yarn'овская VM часто запрашивает значение через T = System.IConvertible (общий интерфейс,
        // bool его реализует). Поэтому проверяем не "T это bool", а "в T можно положить bool".
        // Это покрывает T = bool, T = IConvertible, T = object.
        if (typeof(T).IsAssignableFrom(typeof(bool)))
        {
            bool value = gs.GetFlag(StripDollar(variableName));
            result = (T)(object)value;
            return true;
        }

        Debug.LogWarning($"[MSAVariableStorage] TryGetValue<{typeof(T).Name}>({variableName}) — " +
                         "MSA поддерживает только bool-флаги. Возвращаю default.");
        return false;
    }

    public override void SetValue(string variableName, bool boolValue)
    {
        if (string.IsNullOrEmpty(variableName)) return;
        var gs = GameState.Instance;
        if (gs == null)
        {
            Debug.LogWarning($"[MSAVariableStorage] GameState.Instance == null при записи {variableName}");
            return;
        }
        gs.SetFlag(StripDollar(variableName), boolValue);
    }

    public override void SetValue(string variableName, float floatValue)
    {
        Debug.LogWarning($"[MSAVariableStorage] SetValue<float>({variableName}={floatValue}) — " +
                         "MSA поддерживает только bool-флаги, операция игнорируется. " +
                         "Если в .yarn нужна числовая переменная — расширить MSAVariableStorage.");
    }

    public override void SetValue(string variableName, string stringValue)
    {
        Debug.LogWarning($"[MSAVariableStorage] SetValue<string>({variableName}=\"{stringValue}\") — " +
                         "MSA поддерживает только bool-флаги, операция игнорируется.");
    }

    public override void Clear()
    {
        if (GameState.Instance != null) GameState.Instance.ClearAllFlags();
    }

    public override bool Contains(string variableName)
    {
        if (string.IsNullOrEmpty(variableName)) return false;
        var gs = GameState.Instance;
        if (gs == null) return false;
        return gs.dialogueFlags.Contains(StripDollar(variableName));
    }

    public override (FloatDictionary FloatVariables, StringDictionary StringVariables, BoolDictionary BoolVariables) GetAllVariables()
    {
        var floats = new FloatDictionary();
        var strings = new StringDictionary();
        var bools = new BoolDictionary();
        if (GameState.Instance != null)
        {
            foreach (var flag in GameState.Instance.dialogueFlags)
                bools["$" + flag] = true;
        }
        return (floats, strings, bools);
    }

    public override void SetAllVariables(FloatDictionary floats, StringDictionary strings, BoolDictionary bools, bool clear = true)
    {
        var gs = GameState.Instance;
        if (gs == null) return;
        if (clear) gs.ClearAllFlags();
        if (bools != null)
            foreach (var kv in bools)
                gs.SetFlag(StripDollar(kv.Key), kv.Value);
    }

    private static string StripDollar(string variableName)
    {
        if (string.IsNullOrEmpty(variableName)) return variableName;
        return variableName[0] == '$' ? variableName.Substring(1) : variableName;
    }
}
