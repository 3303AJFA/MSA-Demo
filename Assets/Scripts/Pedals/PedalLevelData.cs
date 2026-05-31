using UnityEngine;

[System.Serializable]
public class PedalLevelData
{
    [Tooltip("Mk номер (I=1, II=2 ... V=5). Только для отображения.")]
    [Range(1, 5)] public int mark = 1;

    [Tooltip("Стоимость в % Voltage")]
    public float voltageCost = 20f;

    [Tooltip("Основное численное значение. Смысл задаётся педалью (урон, размер щита, % дебаффа)")]
    public float value = 10f;

    [Tooltip("Длительность эффекта (для bleed/stun/buff). 0 если не нужно.")]
    public float duration = 0f;

    [Tooltip("Кулдаун струны в долях такта. 0 = взять из CardData.cooldownBeats")]
    public float cooldownBeats = 0f;
}
