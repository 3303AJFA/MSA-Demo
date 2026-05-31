using UnityEngine;

public abstract class PedalEffect : ScriptableObject
{
    [Header("Pedal Info")]
    public string pedalName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Levels (Mk I-V)")]
    public PedalLevelData[] levels = new PedalLevelData[5];

    [Tooltip("Активный уровень (индекс 0..4). Меняется через прогресс игрока, не вручную.")]
    [HideInInspector] public int currentLevel = 0;

    public PedalLevelData GetLevel()
    {
        if (levels == null || levels.Length == 0) return null;
        int idx = Mathf.Clamp(currentLevel, 0, levels.Length - 1);
        return levels[idx];
    }

    public float GetVoltageCost()
    {
        var lvl = GetLevel();
        return lvl != null ? lvl.voltageCost : 0f;
    }

    public float GetCooldownBeats()
    {
        var lvl = GetLevel();
        return lvl != null ? lvl.cooldownBeats : 0f;
    }

    public abstract void Execute(CombatContext ctx);
}
