using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioSystem : MonoBehaviour
{
    public static AudioSystem Instance;

    [Header("String Notes")]
    public AudioClip noteE_Low;
    public AudioClip noteA;
    public AudioClip noteD;
    public AudioClip noteG;
    public AudioClip noteB;
    public AudioClip noteE_High;

    [Header("Pick Sound")]
    public AudioClip pickSound;

    [Header("Settings")]
    [Range(0f, 0.02f)]
    public float chordHumanizeOffset = 0.01f; // живость аккорда

    private List<AudioSource> audioSources = new List<AudioSource>();

    void Awake()
    {
        Instance = this;

        // Создаём пул AudioSource (по одному на струну)
        for (int i = 0; i < 6; i++)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            audioSources.Add(source);
        }
    }

    // Вызывается при нажатии клавиши
    public void PlayPick()
    {
        if (pickSound == null) return;
        var source = gameObject.AddComponent<AudioSource>();
        source.PlayOneShot(pickSound, 0.3f);
        Destroy(source, 1f);
    }

    /// <summary>
    /// Одиночная нота струны. Индексы 0..5 соответствуют клавишам Q/W/E/A/S/D:
    /// 0=E_Low (Q), 1=A (W), 2=D (E), 3=G (A), 4=B (S), 5=E_High (D).
    /// Без боевой обработки — просто звук. Используется гитарным режимом в MapScene.
    /// </summary>
    public void PlayNote(int stringIndex)
    {
        AudioClip clip = stringIndex switch
        {
            0 => noteE_Low,
            1 => noteA,
            2 => noteD,
            3 => noteG,
            4 => noteB,
            5 => noteE_High,
            _ => null
        };
        if (clip == null) return;
        audioSources[stringIndex % audioSources.Count].PlayOneShot(clip);
    }

    // Вызывается при срабатывании комбо
    public void PlayCombo(List<CardData> cards)
    {
        StartCoroutine(PlayChord(cards));
    }

    private IEnumerator PlayChord(List<CardData> cards)
    {
        for (int i = 0; i < cards.Count; i++)
        {
            AudioClip clip = GetClipForCard(cards[i]);
            if (clip != null)
                audioSources[i % audioSources.Count].PlayOneShot(clip);

            // Humanize - каждая нота чуть позже предыдущей
            if (cards.Count > 1)
                yield return new WaitForSeconds(
                    Random.Range(0f, chordHumanizeOffset)
                );
        }
    }

    private AudioClip GetClipForCard(CardData card)
    {
        return card.cardName switch
        {
            "E↓" => noteE_Low,
            "A"  => noteA,
            "D"  => noteD,
            "G"  => noteG,
            "B"  => noteB,
            "E↑" => noteE_High,
            _    => null
        };
    }
}