using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using DG.Tweening;
using Yarn.Unity;

/// <summary>
/// DE-стиль правый лог реплик — теперь как DialoguePresenter Yarn'а. Заменяет старый
/// DialogueLogUI: вся визуальная логика сохранена (накопительный лог, говорящие цветом из
/// DialogueSpeaker SO, кнопки выбора в рейле, DOTween-фейды, фикс canvas-wake), источник
/// строк/опций — Yarn вместо нашего runner'а.
///
/// КРИТИЧНО: rootCanvasGroup.alpha НИКОГДА не опускается до 0 в рантайме. При alpha=0 Unity
/// пропускает Canvas из drawcall'ов и layout-passes до input event — реплики становились
/// невидимыми, пока пользователь не кликнет. Появление/скрытие лога анимируется фейдом
/// Background-Image и per-line CanvasGroup'ов.
///
/// Маппинг говорящего: Yarn выдаёт CharacterName из конвенции "Name: text" в .yarn.
/// SpeakerByName-словарь сопоставляет имя со SO для цвета. Если SO не нашли — рисуем
/// белым с raw-именем.
/// </summary>
public class MSADialogueView : DialoguePresenterBase
{
    [Header("References")]
    [Tooltip("Канвас-группа корня лога. Используется ТОЛЬКО для toggle interactable/blocksRaycasts. " +
             "Alpha НЕ трогаем — alpha=0 глушит рендер канваса до input event (баг wake on click).")]
    public CanvasGroup rootCanvasGroup;

    [Tooltip("Image фона лога. Его alpha анимируется при появлении/скрытии вместо корневой CanvasGroup. " +
             "Если null — ищется ребёнок 'Background' под rootCanvasGroup.")]
    public Image backgroundImage;

    [Tooltip("Контейнер строк. Внутри — VerticalLayoutGroup (Control Width=true, Lower Center).")]
    public RectTransform linesContainer;

    [Tooltip("Контейнер кнопок выбора. Внизу лога.")]
    public RectTransform choicesContainer;

    [Header("Prefabs")]
    [Tooltip("Префаб реплики — CanvasGroup + дочерние TMP 'Speaker' + 'Body'.")]
    public GameObject linePrefab;

    [Tooltip("Префаб кнопки выбора — Button + дочерний TMP.")]
    public GameObject choiceButtonPrefab;

    [Header("Speakers")]
    [Tooltip("Реестр SO говорящих. Имя в .yarn ('NPC:') ищется по displayName в этом списке для цвета. " +
             "Не нашли — белый цвет, рисуется raw-имя.")]
    public List<DialogueSpeaker> knownSpeakers = new List<DialogueSpeaker>();

    [Header("Style")]
    [Range(0f, 1f)] public float pastLineAlpha = 0.4f;
    public float lineFadeDuration = 0.25f;
    public Color choiceColor = new Color(0.79f, 0.64f, 0.29f, 1f);
    public float panelFadeDuration = 0.3f;

    private readonly List<CanvasGroup> spawnedLines = new List<CanvasGroup>();
    private readonly List<GameObject> spawnedChoices = new List<GameObject>();
    private Dictionary<string, DialogueSpeaker> speakerByName;
    private float backgroundTargetAlpha = 1f;

    // Установлено true когда игрок жмёт Space/Enter/ЛКМ на показанной реплике —
    // RunLineAsync крутится в await-цикле, пока не увидит этот флаг.
    private bool advanceRequested;

    // DialogueOptionID выбранной кнопки. Null = ещё не выбрано.
    // RunOptionsAsync крутится пока null.
    private int? pendingChoiceID;

    void Awake()
    {
        speakerByName = new Dictionary<string, DialogueSpeaker>();
        for (int i = 0; i < knownSpeakers.Count; i++)
        {
            var sp = knownSpeakers[i];
            if (sp != null && !string.IsNullOrEmpty(sp.displayName))
                speakerByName[sp.displayName] = sp;
        }

        if (backgroundImage == null && rootCanvasGroup != null)
        {
            var bg = rootCanvasGroup.transform.Find("Background");
            if (bg != null) backgroundImage = bg.GetComponent<Image>();
        }

        if (backgroundImage != null)
        {
            backgroundTargetAlpha = backgroundImage.color.a;
            SetBackgroundAlpha(0f);
        }

        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha = 1f;
            rootCanvasGroup.interactable = false;
            rootCanvasGroup.blocksRaycasts = false;
        }
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // Хоткеи выбора 1..9 — пока кнопки видны и выбор ещё не сделан.
        if (spawnedChoices.Count > 0 && pendingChoiceID == null)
        {
            for (int i = 0; i < spawnedChoices.Count && i < 9; i++)
            {
                Key digit = (Key)((int)Key.Digit1 + i);
                if (kb[digit].wasPressedThisFrame)
                {
                    SelectChoiceByIndex(i);
                    return;
                }
            }
        }
        // Продвижение реплики только когда кнопок выбора НЕТ.
        else if (spawnedChoices.Count == 0)
        {
            bool pressed = kb.spaceKey.wasPressedThisFrame
                || kb.enterKey.wasPressedThisFrame
                || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
            if (pressed) advanceRequested = true;
        }
    }

    public override async YarnTask OnDialogueStartedAsync()
    {
        ClearAll();
        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.interactable = true;
            rootCanvasGroup.blocksRaycasts = true;
        }
        if (backgroundImage != null)
        {
            backgroundImage.DOKill();
            backgroundImage.DOFade(backgroundTargetAlpha, panelFadeDuration);
        }
        await YarnTask.Yield();
    }

    public override async YarnTask OnDialogueCompleteAsync()
    {
        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.interactable = false;
            rootCanvasGroup.blocksRaycasts = false;
        }

        // Гасим реплики синхронно с фоном — не трогая rootCanvasGroup (вернуло бы баг wake-on-click).
        for (int i = 0; i < spawnedLines.Count; i++)
        {
            var cg = spawnedLines[i];
            if (cg == null) continue;
            cg.DOKill();
            cg.DOFade(0f, panelFadeDuration);
        }
        if (backgroundImage != null)
        {
            backgroundImage.DOKill();
            backgroundImage.DOFade(0f, panelFadeDuration);
        }

        await YarnTask.Delay(System.TimeSpan.FromSeconds(panelFadeDuration));
        ClearAll();
    }

    public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        SpawnLine(line);
        advanceRequested = false;

        while (!advanceRequested && !token.IsNextContentRequested)
            await YarnTask.Yield();

        advanceRequested = false;
    }

    public override async YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] options, LineCancellationToken token)
    {
        pendingChoiceID = null;
        SpawnChoices(options);

        while (pendingChoiceID == null && !token.IsNextContentRequested)
            await YarnTask.Yield();

        int? finalID = pendingChoiceID;
        pendingChoiceID = null;
        ClearChoices();

        if (finalID == null) return null;
        for (int i = 0; i < options.Length; i++)
            if (options[i].DialogueOptionID == finalID.Value)
                return options[i];
        return null;
    }

    void SpawnLine(LocalizedLine line)
    {
        // Дим предыдущие реплики.
        for (int i = 0; i < spawnedLines.Count; i++)
        {
            var cg = spawnedLines[i];
            if (cg == null) continue;
            cg.DOKill();
            cg.DOFade(pastLineAlpha, lineFadeDuration);
        }

        if (linePrefab == null || linesContainer == null) return;

        var go = Instantiate(linePrefab, linesContainer);
        var cgNew = go.GetComponent<CanvasGroup>();
        if (cgNew == null) cgNew = go.AddComponent<CanvasGroup>();
        cgNew.alpha = 0f;
        cgNew.DOFade(1f, lineFadeDuration);

        string charName = line.CharacterName;
        string body = line.TextWithoutCharacterName.Text;

        DialogueSpeaker speaker = null;
        if (!string.IsNullOrEmpty(charName) && speakerByName.TryGetValue(charName, out var found))
            speaker = found;

        var speakerLabel = FindChildTMP(go, "Speaker");
        var bodyLabel = FindChildTMP(go, "Body");

        if (speakerLabel != null)
        {
            speakerLabel.text = speaker != null ? speaker.displayName : (charName ?? "???");
            speakerLabel.color = speaker != null ? speaker.color : Color.white;
        }
        if (bodyLabel != null) bodyLabel.text = body;

        spawnedLines.Add(cgNew);

        var lineRect = go.GetComponent<RectTransform>();
        if (lineRect != null) LayoutRebuilder.ForceRebuildLayoutImmediate(lineRect);
        LayoutRebuilder.ForceRebuildLayoutImmediate(linesContainer);
    }

    void SpawnChoices(DialogueOption[] options)
    {
        ClearChoices();
        if (choiceButtonPrefab == null || choicesContainer == null) return;

        int visibleIndex = 0;
        for (int i = 0; i < options.Length; i++)
        {
            var opt = options[i];
            // Yarn передаёт unavailable-варианты тоже (например с невыполненным <<if>>) —
            // мы их прячем полностью, как делал старый runner.
            if (!opt.IsAvailable) continue;

            var go = Instantiate(choiceButtonPrefab, choicesContainer);
            spawnedChoices.Add(go);

            int capturedID = opt.DialogueOptionID;
            var label = go.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = $"{visibleIndex + 1}. {opt.Line.TextWithoutCharacterName.Text}";
                label.color = choiceColor;
            }

            var btn = go.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => { pendingChoiceID = capturedID; });
            }

            visibleIndex++;
        }

        for (int i = 0; i < spawnedChoices.Count; i++)
        {
            var rt = spawnedChoices[i] != null ? spawnedChoices[i].GetComponent<RectTransform>() : null;
            if (rt != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(choicesContainer);
    }

    void SelectChoiceByIndex(int hotkeyIndex)
    {
        if (hotkeyIndex < 0 || hotkeyIndex >= spawnedChoices.Count) return;
        var go = spawnedChoices[hotkeyIndex];
        if (go == null) return;
        var btn = go.GetComponent<Button>();
        if (btn != null) btn.onClick.Invoke();
    }

    void ClearChoices()
    {
        for (int i = 0; i < spawnedChoices.Count; i++)
            if (spawnedChoices[i] != null) Destroy(spawnedChoices[i]);
        spawnedChoices.Clear();
    }

    void ClearAll()
    {
        ClearChoices();
        for (int i = 0; i < spawnedLines.Count; i++)
            if (spawnedLines[i] != null && spawnedLines[i].gameObject != null)
                Destroy(spawnedLines[i].gameObject);
        spawnedLines.Clear();
    }

    void SetBackgroundAlpha(float a)
    {
        if (backgroundImage == null) return;
        var c = backgroundImage.color;
        c.a = a;
        backgroundImage.color = c;
    }

    TextMeshProUGUI FindChildTMP(GameObject root, string childName)
    {
        var tr = root.transform.Find(childName);
        if (tr != null) return tr.GetComponent<TextMeshProUGUI>();
        var all = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < all.Length; i++)
            if (all[i].name == childName) return all[i];
        return null;
    }
}
