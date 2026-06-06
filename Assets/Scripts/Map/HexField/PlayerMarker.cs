using UnityEngine;
using DG.Tweening;

/// <summary>
/// Метка игрока на поле. Срез 1 — простой GameObject с любым визуалом (примитив/иконка),
/// перемещается между узлами через DOTween. В будущих срезах сюда же ляжет 3D-Vivus
/// (мечта про модель на узле), но это пост-релизный задел.
/// </summary>
public class PlayerMarker : MonoBehaviour
{
    [Tooltip("Длительность твина перемещения между узлами. 0 = снап без анимации.")]
    public float moveDuration = 0.3f;

    [Tooltip("Смещение метки относительно центра узла (например, +Y чтобы стоять над поверхностью гекса).")]
    public Vector3 nodeOffset = new Vector3(0f, 0.5f, 0f);

    public void SnapTo(Vector3 nodePosition)
    {
        transform.DOKill();
        transform.position = nodePosition + nodeOffset;
    }

    public void MoveTo(Vector3 nodePosition)
    {
        transform.DOKill();
        Vector3 target = nodePosition + nodeOffset;
        if (moveDuration <= 0f)
        {
            transform.position = target;
            return;
        }
        transform.DOMove(target, moveDuration).SetEase(Ease.OutQuad);
    }
}
