using UnityEngine;

/// <summary>
/// Говорящий в диалоге. Статическая design-данная: имя для UI + цвет для различения в логе.
/// Один SO-ассет на персонажа, переиспользуется в любом DialogueLineNode.
/// </summary>
[CreateAssetMenu(fileName = "Speaker_New", menuName = "MSA/Dialogue/Speaker")]
public class DialogueSpeaker : ScriptableObject
{
    [Tooltip("Имя как показывается в UI лога.")]
    public string displayName = "???";

    [Tooltip("Цвет имени говорящего в логе — для разведения по цветам, как в DE.")]
    public Color color = Color.white;
}
