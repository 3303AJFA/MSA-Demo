using UnityEngine;

[System.Serializable]
public class StreakProfile
{
    [Tooltip("Сколько добавлять к стрику за PERFECT ноту")]
    public float perfectGain = 12f;

    [Tooltip("Сколько добавлять за GOOD ноту")]
    public float goodGain = 5f;

    [Tooltip("Сколько вычитать за OFF-нажатие")]
    public float offLoss = 15f;

    [Tooltip("Сколько вычитать когда игрок получил урон")]
    public float damageLoss = 30f;

    [Tooltip("Сколько вычитать когда риф зевнул (SLEEPY HEAD — авто-отпуск по comboDecayBeats)")]
    public float sleepyLoss = 18f;
}
